using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class BocMenhBusiness(ILogger<BocMenhBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , ITransactionBusiness transactionBusiness
    , IOpenAiService openAiService
    , IGeminiAIService geminiAIService
    , IPhongThuyNhanSinhService phongThuyNhanSinhService
    , IOptions<AppSettings> appOptions
    , PainPublisher publisher) : BaseHttpBusiness<BocMenhBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IBocMenhBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly ITransactionBusiness _transactionBusiness = transactionBusiness;
    private readonly IOpenAiService _openAiService = openAiService;
    private readonly IGeminiAIService _geminiAIService = geminiAIService;
    private readonly IPhongThuyNhanSinhService _phongThuyNhanSinhService = phongThuyNhanSinhService;

    public async Task<BaseResponse<dynamic>> GetTuTruBatTuAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var currentUserId = Current.UserId;

        var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.Id == id 
                                                                          && f.UserId == currentUserId
                                                                          && f.Kind == (short)TheologyKind.TuTruBatTu);

        if (existed == null)
            throw new BusinessException("TuTruBatTuNotFound", "TuTruBatTu not found");

        if (existed.Result.IsMissing()
            && existed.Status != (short)TheologyStatus.Failed)
            FuncTaskHelper.FireAndForget(() => ExplainTuTruBatTuAsync(existed.Id));

        var servicePrice = await GetServicePriceByTheologyKind((TheologyKind)existed.Kind, context);
        var res = await GetTuTruBatTuWithPaymentStatus(existed.Id, context);

        return new(new
        {
            Id = id,
            Status = (TheologyStatus)existed.Status,
            ServicePrice = servicePrice?.GetFinalFates(),
            Input = existed.Input.IsPresent() ? JsonSerializer.Deserialize<TuTruBatTuRequest>(existed.Input) : null,
            PreData = existed.PreData.IsPresent() ? JsonSerializer.Deserialize<LaSoBatTuResponse>(existed.PreData) : null,
            Explanation = res.Data,
        });
    }

    public async Task<BaseResponse<dynamic>> InitTuTruBatTuAsync(TuTruBatTuRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            request.Standardize();

            var userId = Current.UserId;
            var kind = TheologyKind.TuTruBatTu;

            if (userId == null || !await context.Users.AnyAsync(a => a.Id == userId))
                throw new BusinessException("Unauthorized", "401 Unauthorized");

            var servicePrice = await GetServicePriceByTheologyKind(kind, context);

            if (servicePrice == null)
                throw new BusinessException("InvalidSystemData", $"Service price not found for kind '{kind}'");

            var lunarBase = VietnameseCalendar.GetLunarDate(request.BirthDateTime.Day, request.BirthDateTime.Month, request.BirthDateTime.Year, request.BirthDateTime.Hour);
            var lunarBirthDateTime = new DateTime(lunarBase.Year, lunarBase.Month, lunarBase.Day, lunarBase.Hour, request.BirthDateTime.Minute, 0, 0);

            var laSoBatTu = (await _phongThuyNhanSinhService.BuildLaSoBatTuAsync(request.Name
                                , request.Gender
                                , request.BirthDateTime
                                , lunarBirthDateTime))
                            .FirstOrDefault();

            string category = request.Category.GetDescription();

            var (systemPrompt, userPrompt, combinedPrompts) = GenerateBatTuTuTruPrompt(request, lunarBirthDateTime, category, laSoBatTu.Tutru);

            var key = request.InitUniqueKey(kind, systemPrompt, userPrompt, combinedPrompts);

            var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.UniqueKey == key);            

            if (existed == null)
            {
                existed = new TheologyRecord
                {
                    UserId = userId.Value,
                    UniqueKey = key,
                    Kind = (byte)kind,
                    Status = (byte)TheologyStatus.Created,
                    Input = JsonSerializer.Serialize(request),
                    PreData = JsonSerializer.Serialize(laSoBatTu),
                    CreatedTs = DateTime.UtcNow,
                    ServicePrice = servicePrice,
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                    CombinedPrompts = combinedPrompts,
                };

                await context.TheologyRecords.AddAsync(existed);
                await context.SaveChangesAsync();
            }

            //if (existed.Result.IsMissing())
            //    FuncTaskHelper.FireAndForget(() => ExplainTuTruBatTuAsync(existed.Id));

            return new(new
            {
                Id = existed.Id
            });
        }
        catch (Exception e)
        {
            throw new BusinessException("UnavailableToTuTruBatTu", "Unavailable to TuTruBatTu.", e);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<bool>> ExplainTuTruBatTuAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.Id == id);

        try
        {
            if (existed == null)
                throw new BusinessException("TuTruBatTuNotFound", "TuTruBatTu not found or unavailabe");

            if (existed.Status == (short)TheologyStatus.Failed)
                throw new BusinessException("FailedTuTruBatTu", "TuTruBatTu failed");

            if (existed.Status == (byte)TheologyStatus.Created
                || (existed.Status == (byte)TheologyStatus.Analyzing
                     && (existed.LastAnalysisTs == null || existed.LastAnalysisTs.Value < DateTime.UtcNow.AddSeconds(-40))))
            {
                existed.Status = (byte)TheologyStatus.Analyzing;
                existed.LastAnalysisTs = DateTime.UtcNow;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();

                var res = await _geminiAIService.SendChatAsync(existed.CombinedPrompts);

                if (res.IsPresent())
                {
                    res = res.Replace("```html", string.Empty);

                    res = res.Replace("```json", string.Empty);

                    if (res.EndsWith("```"))
                    {
                        res = res.TrimEnd('`');
                    }
                }

                var analysisResult = JsonSerializer.Deserialize<TuTruAnalysisResult>(res, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                existed.Result = JsonSerializer.Serialize(new TuTruBatTuDto
                {
                    Original = res,
                    MetaData = analysisResult?.ImprovementSuggestions?.FengShuiItems?.Select(s => new MetadataEntry
                    {
                        TagName = s.Name,
                        Attributes =
                        [
                            new()
                            {
                                Name = "NguHanhBanMenh",
                                Value = s.Elements?.ToList()
                            }
                        ]
                    }).ToList() ?? [],
                });

                existed.Status = (byte)TheologyStatus.Analyzed;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();
            }

            return new(true);
        }
        catch (Exception e)
        {
            if (existed != null)
            {
                existed.FailedCount++;

                if (existed.FailedCount >= 5)
                {
                    existed.Status = (byte)TheologyStatus.Failed;
                }
                else
                {
                    existed.Status = (byte)TheologyStatus.Created;
                }

                existed.Errors ??= [];

                var error = new
                {
                    message = e.Message,
                    type = e.GetType().Name,
                    inner = e.InnerException?.Message,
                    StackTrace = e.StackTrace,
                };
                existed.Errors.Add(JsonSerializer.Serialize(error));

                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();
            }

            if (existed == null || existed.Status == (byte)TheologyStatus.Failed)
                throw new BusinessException("UnavailableToTuTruBatTu", "Unavailable to TuTruBatTu.", e);
            else
                return new(false);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    private static (string systemPrompt, string userPrompt, string combinedPrompts) GenerateBatTuTuTruPrompt(TuTruBatTuRequest request
        , DateTime lunarBirthDateTime
        , string category
        , TuTruInfo tutru)
    {
        string userPrompt = $@"
Tôi muốn phân tích lá số Bát Tự theo hướng {category}.
Thông tin lá số như sau:

- Họ và tên: {request.Name}
- Giới tính: {request.Gender.GetDescription()}
- Tuổi hiện tại: {(DateTime.UtcNow.Year - request.BirthDateTime.Year)}
- Ngày giờ sinh Dương lịch: {request.BirthDateTime:dd/MM/yyyy HH:mm} ({request.BirthDateTime.DayOfWeek})
- Ngày giờ sinh Âm lịch: {lunarBirthDateTime:dd/MM/yyyy HH:mm} ({lunarBirthDateTime.DayOfWeek})
- Tứ trụ:
    + Trụ Giờ: {tutru.ThoiTru.HourCanChi}
       - Can: {tutru.ThoiTru.HourCan} ({tutru.ThoiTru.HourCanNguHanh}). Thập thần: {tutru.ThoiTru.HourCanThapThan.Name}
       - Chi: {tutru.ThoiTru.HourChi} ({tutru.ThoiTru.HourChiNguHanh})
       - Nạp âm: {tutru.ThoiTru.NapAm.Name}
       - Vòng trường sinh: {tutru.ThoiTru.VongTruongSinh.Info.Name}
       - Thần sát: {string.Join(", ", tutru.ThoiTru.ThanSat.Select(s => s.Name).ToList())}
       - Thập thần: {string.Join(", ", tutru.ThoiTru.HourThapThan.Select(s => s.Name).ToList())}
    + Trụ Ngày: {tutru.NhatTru.DayCanChi}
       - Can: {tutru.NhatTru.DayCan} ({tutru.NhatTru.DayCanNguHanh}). Thập thần: {tutru.NhatTru.DayCanThapThan.Name}
       - Chi: {tutru.NhatTru.DayChi} ({tutru.NhatTru.DayChiNguHanh})
       - Nạp âm: {tutru.NhatTru.NapAm.Name}
       - Vòng trường sinh: {tutru.NhatTru.VongTruongSinh.Info.Name}
       - Thần sát: {string.Join(", ", tutru.NhatTru.ThanSat.Select(s => s.Name).ToList())}
       - Thập thần: {string.Join(", ", tutru.NhatTru.DayThapThan.Select(s => s.Name).ToList())}
    + Trụ Tháng: {tutru.NguyetTru.MonthCanChi}
       - Can: {tutru.NguyetTru.MonthCan} ({tutru.NguyetTru.MonthCanNguHanh}). Thập thần: {tutru.NguyetTru.MonthCanThapThan.Name}
       - Chi: {tutru.NguyetTru.MonthChi} ({tutru.NguyetTru.MonthChiNguHanh})
       - Nạp âm: {tutru.NguyetTru.NapAm.Name}
       - Vòng trường sinh: {tutru.NguyetTru.VongTruongSinh.Info.Name}
       - Thần sát: {string.Join(", ", tutru.NguyetTru.ThanSat.Select(s => s.Name).ToList())}
       - Thập thần: {string.Join(", ", tutru.NguyetTru.MonthThapThan.Select(s => s.Name).ToList())}
    + Trụ Năm: {tutru.ThienTru.YearCanChi}
       - Can: {tutru.ThienTru.YearCan} ({tutru.ThienTru.YearCanNguHanh}). Thập thần: {tutru.ThienTru.YearCanThapThan.Name}
       - Chi: {tutru.ThienTru.YearChi} ({tutru.ThienTru.YearChiNguHanh})
       - Nạp âm: {tutru.ThienTru.NapAm.Name}
       - Vòng trường sinh: {tutru.ThienTru.VongTruongSinh.Info.Name}
       - Thần sát: {string.Join(", ", tutru.ThienTru.ThanSat.Select(s => s.Name).ToList())}
       - Thập thần: {string.Join(", ", tutru.ThienTru.YearThapThan.Select(s => s.Name).ToList())}
";

        string systemPrompt = $@"Bạn là một chuyên gia Bát Tự bậc thầy, chuyên luận giải sâu sắc về {category}. Nhiệm vụ của bạn là trả về một cấu trúc JSON DUY NHẤT, mạch lạc, không chứa bất kỳ ký tự nào khác ngoài JSON (không có ```json hay giải thích).

== ĐỊNH HƯỚNG NỘI DUNG ==
1.  **Cốt lõi trước, chi tiết sau**: Trong mỗi mục, hãy điền ""key_point"" bằng một câu văn súc tích, đắt giá, nêu bật ý chính. Sau đó, ""detailed_analysis"" sẽ diễn giải sâu hơn, dài hơn, cung cấp bối cảnh và lý luận chi tiết. Điều này giúp người đọc nắm bắt nhanh ý chính trước khi đi vào chiều sâu.
2.  **Tập trung vào vấn đề và giải pháp**: Luôn phân tích cả mặt mạnh và yếu, nhưng nhấn mạnh vào các khuyết thiếu, mâu thuẫn để phần cải vận trở nên logic và hữu ích.
3.  **Khuyến khích dùng biểu đồ**: Cung cấp dữ liệu chính xác trong ""element_distribution"" để phía giao diện có thể dựng biểu đồ Ngũ Hành, làm cho kết quả sinh động và đáng tin cậy hơn.
4.  **Cải vận cụ thể**: Các gợi ý cải vận phải gắn chặt với Dụng Thần và Kỵ Thần đã phân tích.
5.  **Giữ nguyên tinh thần luận giải**: Tuân thủ nghiêm ngặt 8 mục luận giải, không thay đổi thứ tự, không bỏ sót. Văn phong chuyên sâu, uyên bác nhưng dễ hiểu, gần gũi.
6.  **Luôn bám sát thông tin lá số mà người dùng cung cấp, được phép tự do suy luận dựa trên đó**: Sử dụng thông tin từ lá số Bát Tự mà người dùng cung cấp để làm cơ sở cho các phân tích và luận giải. Không tự ý thêm thông tin không có trong lá số, nhưng được phép suy luận tự do từ lá số ấy.
7.  **Luôn đề cập và xưng hô bằng tên gọi của người dùng và tuổi của họ**: Xưng hô tên gọi dựa vào tuổi, ví dụ anh Đạt, chị Hân, cô Nhàn, chú Đức, ông Năm, bà Lan,... thể hiện sự gần gũi, tôn trọng.
8.  **Luôn chú trọng tính mạch lạc trong câu từ, cách hành văn, tránh lập ý lập từ quá nhiều** Tuy rằng kết quả trả về ở dạng Json, nhưng vẫn cần đảm bảo tính mạch lạc toàn bộ các câu trong đoạn, các đoạn trong section, section trong cả Json. Phải dễ hiểu trong từng câu từ, tránh việc lập ý lập từ quá nhiều, khiến cho người đọc cảm thấy khó hiểu hoặc rối rắm.
9.  **Luận giải phải dựa trên tính thực tế về độ tuổi, khoản thời gian đang đề cập so với xu thế xã hội hiện tại**: Các luận giải đưa ra phải mang sức thuyết phục và khiến người dùng cảm giác đúng với bản thân và xu hướng xã hội và độ tuổi của họ lúc được đề cập.

== CẤU TRÚC JSON BẮT BUỘC ==
Hãy điền thông tin vào mẫu JSON dưới đây.

{{
  ""day_master_analysis"": {{
    ""title"": ""Nhật Chủ – Khí chất tổng quan"",
    ""key_point"": ""[Nêu cốt lõi về tính cách, bản chất của Nhật chủ liên quan đến {category}]. có thể đưa ra một vài nhận định về con người và một vài sự việc trong quá khứ của họ, dự đoán tương lai gần để tăng sự tin tưởng [ít nhất 5 câu]"",
    ""detailed_analysis"": ""[Phân tích chi tiết về Âm Dương, Ngũ Hành của Nhật Can, đặc tính Thiên Can - Địa Chi trụ ngày và ảnh hưởng của chúng đến {category}. Luận giải mạnh - yếu - khuyết - xung - cải vận.][ít nhất 10 câu]""
  }},
  ""five_elements_analysis"": {{
    ""title"": ""Cục diện Ngũ Hành – Vượng suy"",
    ""key_point"": ""[Nhận định tổng quan về tình trạng Ngũ Hành: thân vượng hay nhược, hành nào chủ đạo, hành nào khuyết thiếu. nhấn mạnh sự khuyết thiếu nếu có] [ít nhất 5 câu]"",
    ""detailed_analysis"": ""[Phân tích sâu về sự mất cân bằng, mâu thuẫn, tương khắc trong tứ trụ và tác động của nó tới {category}. Đây là cơ sở để xác định Dụng Thần.] [ít nhất 10 câu]"",
    ""element_distribution"": [
      {{ ""element"": ""Kim"", ""count"": 0, ""strength"": ""[Vượng/Tướng/Hưu/Tù/Tử hoặc Vượng/Nhược]"" }},
      {{ ""element"": ""Mộc"", ""count"": 0, ""strength"": ""[Vượng/Tướng/Hưu/Tù/Tử hoặc Vượng/Nhược]"" }},
      {{ ""element"": ""Thủy"", ""count"": 0, ""strength"": ""[Vượng/Tướng/Hưu/Tù/Tử hoặc Vượng/Nhược]"" }},
      {{ ""element"": ""Hỏa"", ""count"": 0, ""strength"": ""[Vượng/Tướng/Hưu/Tù/Tử hoặc Vượng/Nhược]"" }},
      {{ ""element"": ""Thổ"", ""count"": 0, ""strength"": ""[Vượng/Tướng/Hưu/Tù/Tử hoặc Vượng/Nhược]"" }}
    ]
  }},
  ""ten_gods_analysis"": {{
    ""title"": ""Thập Thần – Bản chất vận hạn chính"",
    ""key_point"": ""[Nhận định các Thập Thần chủ chốt ảnh hưởng đến {category} là gì, tốt hay xấu. nhấn mạnh cái xấu nếu có][ít nhất 5 câu]"",
    ""detailed_analysis"": ""[Phân tích sự hiện diện (hoặc khuyết thiếu) của các Thập Thần quan trọng (Tài, Quan, Ấn, Thực, Tỷ). Luận giải các cách cục, thần sát đặc biệt (nếu có) và ảnh hưởng của chúng đến {category}.][ít nhất 10 câu]""
  }},
  ""useful_and_unfavorable_gods"": {{
    ""title"": ""Dụng Thần – Kỵ Thần"",
    ""key_point"": ""Dụng Thần là [Hành 1] và [Hành 2, nếu có] giúp cân bằng lá số, mang lại may mắn và cơ hội. Kỵ Thần là [Hành 1] và [Hành 2, nếu có] cần được chế hóa để tránh những rắc rối và hao tổn.[ít nhất 5 câu]"",
    ""detailed_analysis"": ""Việc nắm rõ Dụng Thần và Kỵ Thần là kim chỉ nam cho mọi hành động cải vận của bạn. Khi Dụng Thần được tăng cường, mọi việc sẽ trở nên hanh thông, thuận lợi hơn, đặc biệt trong lĩnh vực {{category}}. Ngược lại, khi Kỵ Thần bị kích hoạt, bạn sẽ dễ gặp phải trở ngại, khó khăn, thậm chí là tai ương. Do đó, việc tập trung vào việc bổ trợ Dụng Thần và chế hóa Kỵ Thần là vô cùng quan trọng để cải thiện vận trình của bạn.[ít nhất 10 câu]"",
    ""useful_gods"": {{
      ""elements"": [""[Hành 1]"", ""[Hành 2, nếu có]""],
      ""detailed_analysis"": ""Dựa trên phân tích Tứ trụ, Dụng Thần được xác định là hành **[Hành 1]** và **[Hành 2, nếu có]** bởi vì [Giải thích chi tiết hơn lý do, ví dụ: lá số của bạn đang thiếu hụt nghiêm trọng hành Kim và Thủy, khiến cho năng lượng tổng thể bị mất cân bằng. Kim giúp sinh Thủy, Thủy giúp làm dịu Hỏa vượng, từ đó điều hòa khí trường]. Khi Dụng Thần được bổ trợ, nó sẽ trực tiếp hóa giải những xung khắc, bổ sung những khí thiếu hụt, giúp cho vận trình của bạn trở nên hanh thông, đặc biệt là trong **{{category}}**, nơi bạn sẽ cảm nhận rõ rệt sự thuận lợi, quý nhân phù trợ và các cơ hội đến bất ngờ.[ít nhất 15 câu]"",
      ""explanation"": ""[Giải thích lý do chọn các hành này làm Dụng Thần (ưu tiên hành khuyết). Chúng giúp cân bằng lá số và hỗ trợ {category} như thế nào?][ít nhất 10 câu]"",
    }},
    ""unfavorable_gods"": {{
      ""elements"": [""[Hành 1]"", ""[Hành 2, nếu có]""],
      ""detailed_analysis"": ""Ngược lại với Dụng Thần, Kỵ Thần của bạn là hành **[Hành 1]** và **[Hành 2, nếu có]**. Đây là những hành khí gây mất cân bằng trong Tứ trụ, do [Giải thích chi tiết lý do, ví dụ: chúng quá vượng hoặc tương khắc mạnh với Nhật Chủ/Dụng Thần]. Khi các yếu tố Kỵ Thần này bị kích hoạt, bạn có thể gặp phải những rắc rối, cản trở, thậm chí là tai ương. Trong **{{category}}**, điều này có thể biểu hiện dưới dạng [Nêu ví dụ cụ thể về ảnh hưởng tiêu cực, ví dụ: khó khăn trong tài chính, mâu thuẫn trong các mối quan hệ, hoặc gặp phải những quyết định sai lầm dẫn đến thua lỗ]. Việc nhận diện và tìm cách chế hóa Kỵ Thần là vô cùng cần thiết để giảm thiểu rủi ro và bảo vệ vận may của bạn.[ít nhất 15 câu]"",
      ""explanation"": ""[Giải thích lý do các hành này là Kỵ Thần. Chúng gây mất cân bằng và cản trở {category} ra sao?][ít nhất 10 câu]"",
    }},    
  }},
  ""ten_year_cycles"": {{
    ""title"": ""Đại Vận – Chu kỳ 10 năm - từ lúc sinh ra đến năm 60 tuổi"",
    ""key_point"": ""[Nhận định chung về các giai đoạn Đại Vận sắp tới: thuận lợi hay khó khăn hơn cho mục tiêu {category}? bố cục nhận định từ lúc sinh ra đến năm 60 tuổi, chu kỳ 10 năm][ít nhất 5 câu]"",
    ""detailed_analysis"": ""[Diễn giải về xu hướng chung của các Đại Vận, giải thích tại sao lại có xu hướng đó. bố cục nhận định từ lúc sinh ra đến năm 60 tuổi, chu kỳ 10 năm][ít nhất 10 câu]"",
    ""cycles"": [
      {{ ""age_range"": ""[VD: 0-10 dựa theo tuổi hiện tại mà người dùng cung cấp]"", ""can_chi"": ""[VD: Canh Dần]"", ""element"": ""[VD: Mộc]"", ""analysis"": ""[Phân tích sâu giai đoạn này: Can Chi của Đại Vận tương tác với Tứ trụ ra sao, ảnh hưởng đến {category} như thế nào (tốt/xấu, cơ hội/thách thức). có phải là giai đoạn bức phá hay không? nếu có thì cần tận dụng gì?] [phân tích dựa trên xu hướng xã hội và độ tuổi của giai đoạn này để tránh đưa ra các luận giải không phù hợp với thực tế]"" }},
      {{ ""age_range"": ""[VD: 10-20 dựa theo tuổi hiện tại mà người dùng cung cấp]"", ""can_chi"": ""[VD: Tân Mão]"", ""element"": ""[VD: Mộc]"", ""analysis"": ""[Phân tích tương tự cho đại vận tiếp theo.]"" }}
      ...từ lúc sinh ra (0 tuổi) đến năm 60 tuổi, chu kỳ 10 năm
    ]
  }},
  ""career_guidance"": {{
    ""title"": ""Ngành nghề hoặc mô hình phù hợp"",
    ""key_point"": ""[Gợi ý ngắn gọn các lĩnh vực, ngành nghề hoặc mô hình (làm chủ, làm thuê, đầu tư...) phù hợp nhất.][ít nhất 5 câu]"",
    ""detailed_analysis"": ""[Dựa trên Dụng Thần và đặc tính lá số, phân tích tại sao các ngành nghề, mô hình đó lại phù hợp. Đưa ra các lựa chọn cụ thể để phát huy tối đa tiềm năng trong lĩnh vực {category}.][ít nhất 10 câu]""
  }},
  ""improvement_suggestions"": {{
    ""title"": ""Gợi ý cải vận – Kích hoạt khí vận"",
    ""key_point"": ""[Hành động cốt lõi cần làm để cải vận là gì? (VD: Tăng cường hành Thủy, tập trung vào môi trường năng động...)][ít nhất 8 câu]"",
    ""detailed_analysis"": ""[Dẫn nhập về tầm quan trọng của việc chủ động cải vận. Gợi ý về môi trường sống, thói quen, cách hành xử để bổ sung Dụng Thần và chế ngự Kỵ Thần.][ít nhất 15 câu]"",
    ""feng_shui_items"": [
      {{ ""name"": ""[Tên vật phẩm 1]"", ""elements"": [""[Hành 1]""], ""material"": ""[Chất liệu]"", ""purpose"": ""[Công dụng]"", ""usage_instructions"": ""[Cách dùng cụ thể]"" }},
      {{ ""name"": ""[Tên vật phẩm 2]"", ""elements"": [""[Hành 1]"", ""[Hành 2]""], ""material"": ""[Chất liệu]"", ""purpose"": ""[Công dụng]"", ""usage_instructions"": ""[Cách dùng cụ thể]"" }}
      ...ít nhất 5 vật phẩm và tối đa 10 vật phẩm phong thủy cụ thể
    ]
  }},
  ""conclusion"": {{
    ""title"": ""Lời kết tổng quan"",
    ""key_point"": ""[Tóm tắt 5-10 câu về tiềm năng lớn nhất và thách thức chính trong {category} của người này. định hướng ngành nghề hoặc chủ đề nên đi theo]"",
    ""detailed_analysis"": ""[Tổng kết lại các ưu/nhược điểm từ ít nhất 20 câu. Nhấn mạnh lại vai trò của Dụng Thần, ngũ hành và các hành động cần làm để cải hóa vận mệnh. tóm tắt đại vận 10 năm sắp tới và hành động cần làm, cần chuẩn bị. Khuyến khích mạnh mẽ việc áp dụng các phương pháp cải vận, đặc biệt là sử dụng vật phẩm phong thủy đã gợi ý để đạt được mục tiêu {category}.]""
  }}
}}";
        var combinedPrompts = $@"
{systemPrompt}

Sau đây là yêu cầu và thông tin của người dùng:
{userPrompt}
";

        return (systemPrompt, userPrompt, combinedPrompts);
    }

    public async Task<BaseResponse<dynamic>> ExplainTuTruBatTuTestAsync(TuTruBatTuRequest request)
    {
        request.Standardize();

        var lunarBase = VietnameseCalendar.GetLunarDate(request.BirthDateTime.Day, request.BirthDateTime.Month, request.BirthDateTime.Year, request.BirthDateTime.Hour);
        var lunarBirthDateTime = new DateTime(lunarBase.Year, lunarBase.Month, lunarBase.Day, lunarBase.Hour, request.BirthDateTime.Minute, 0, 0);

        var laSoBatTu = (await _phongThuyNhanSinhService.BuildLaSoBatTuAsync(request.Name
                            , request.Gender
                            , request.BirthDateTime
                            , lunarBirthDateTime))
                        .FirstOrDefault();

        string category = request.Category.GetDescription();

        var tutru = laSoBatTu.Tutru;

        var (systemPrompt, userPrompt, combinedPrompts) = GenerateBatTuTuTruPrompt(request, lunarBirthDateTime, category, laSoBatTu.Tutru);

        var res = await _geminiAIService.SendChatAsync(combinedPrompts);

        if (res.IsPresent())
        {
            res = res.Replace("```html", string.Empty);

            res = res.Replace("```json", string.Empty);

            if (res.EndsWith("```"))
            {
                res = res.TrimEnd('`');
            }
        }

        // Deserialize chuỗi JSON thành đối tượng C#
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Giúp linh hoạt hơn với kiểu chữ hoa/thường của key trong JSON
        };
        var analysisResult = JsonSerializer.Deserialize<TuTruAnalysisResult>(res, options);

        return new(analysisResult);
    }

    private static async Task<BaseResponse<TheologyBaseResult<TuTruAnalysisResult, TuTruAnalysisResult>>> GetTuTruBatTuWithPaymentStatus(Guid theologyRecordId
        , ApplicationDbContext context)
    {
        var result = new TheologyBaseResult<TuTruAnalysisResult, TuTruAnalysisResult>();

        var existed = await context.TheologyRecords.Include(i => i.FatePointTransactions)
                                                   .FirstOrDefaultAsync(f => f.Id == theologyRecordId);

        if (existed?.Result.IsPresent() == true)
        {
            var res = JsonSerializer.Deserialize<TuTruBatTuDto>(existed.Result);

            var ttar = JsonSerializer.Deserialize<TuTruAnalysisResult>(res.Original, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (existed.FatePointTransactions.Count != 0)
            {
                result.PaidResult = ttar;
            }
            else
            {
                ttar?.ToFree();
                result.FreeResult = ttar;
            }
        }

        return new BaseResponse<TheologyBaseResult<TuTruAnalysisResult, TuTruAnalysisResult>>(result);
    }

    private static async Task<ServicePrice> GetServicePriceByTheologyKind(TheologyKind topUpKind, ApplicationDbContext context)
    {
        return await context.ServicePrices.FirstOrDefaultAsync(f => f.ServiceKind == (byte)topUpKind);
    }
}
