using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class BocMenhBusiness(ILogger<BocMenhBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IGeminiAIService geminiAIService
    , ITransactionBusiness transactionBusiness
    , IPhongThuyNhanSinhService phongThuyNhanSinhService) : BaseHttpBusiness<BocMenhBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IBocMenhBusiness
{
    private readonly IGeminiAIService _geminiAIService = geminiAIService;
    private readonly IPhongThuyNhanSinhService _phongThuyNhanSinhService = phongThuyNhanSinhService;
    private readonly ITransactionBusiness _transactionBusiness = transactionBusiness;

    public async Task<BaseResponse<dynamic>> GetTuTruBatTuAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var currentUserId = Current.UserId;

        var existed = await context.TheologyRecords.AsNoTracking()
                                                   .Include(i => i.ServicePrice)
                                                   .FirstOrDefaultAsync(f => f.Id == id 
                                                                          && f.UserId == currentUserId
                                                                          && f.Kind == (short)TheologyKind.TuTruBatTu);

        if (existed == null)
            throw new BusinessException("TuTruBatTuNotFound", "TuTruBatTu not found");

        if (existed.Result.IsMissing()
            && existed.Status != (short)TheologyStatus.Failed)
            FuncTaskHelper.FireAndForget(() => ExplainTuTruBatTuAsync(existed.Id));

        var serviceFates = existed.ServicePriceSnap.IsPresent() ?
                           JsonSerializer.Deserialize<ServicePriceSnap>(existed.ServicePriceSnap).FinalFates :
                           existed.ServicePrice.GetFinalFates();

        var res = await GetTuTruBatTuWithPaymentStatus(existed.Id, context);

        return new(new
        {
            Id = id,
            Status = (TheologyStatus)existed.Status,
            ServicePrice = serviceFates,
            Input = existed.Input.IsPresent() ? JsonSerializer.Deserialize<TuTruBatTuRequest>(existed.Input) : null,
            PreData = existed.PreData.IsPresent() ? JsonSerializer.Deserialize<LunarInfo>(existed.PreData) : null,
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

            var (systemPrompt, userPrompt, combinedPrompts, laSoBatTu) = await GenerateBatTuTuTruPromptAsync(request);

            var key = request.InitUniqueKey(kind, systemPrompt, userPrompt, combinedPrompts);

            var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.UniqueKey == key);            

            if (existed == null)
            {
                existed = new TheologyRecord
                {
                    UserId = userId.Value,
                    UniqueKey = key,
                    Kind = (byte)kind,
                    ServicePriceSnap = JsonSerializer.Serialize(new ServicePriceSnap(servicePrice)),
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

                if (servicePrice.GetFinalFates() <= 0)
                {
                    await _transactionBusiness.PayTheologyRecordAsync(existed.Id);
                }
            }

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

            if (existed.FirstAnalysisTs == null)
            {
                existed.FirstAnalysisTs = DateTime.UtcNow;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();
            }

            if (existed.Status == (byte)TheologyStatus.Created
                || (existed.Status == (byte)TheologyStatus.Analyzing
                     && (existed.LastAnalysisTs == null || existed.LastAnalysisTs.Value < DateTime.UtcNow.AddSeconds(-(GeminiAIService.HttpClientTimeOut/3)))))
            {
                existed.Status = (byte)TheologyStatus.Analyzing;
                existed.LastAnalysisTs = DateTime.UtcNow;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();

                var res = (await _geminiAIService.SendChatAsync(existed.CombinedPrompts)).NormalizeAiResponseString();

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

                existed.SuccessedAnalysisTs = DateTime.UtcNow;
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
                    existed.FailedAnalysisTs = DateTime.UtcNow;
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
   
    public async Task<BaseResponse<dynamic>> ExplainTuTruBatTuTestAsync(TuTruBatTuRequest request)
    {
        request.Standardize();

        var (systemPrompt, userPrompt, combinedPrompts, laSoBatTu) = await GenerateBatTuTuTruPromptAsync(request);

        var res = (await _geminiAIService.SendChatAsync(combinedPrompts)).NormalizeAiResponseString();

        // Deserialize chuỗi JSON thành đối tượng C#
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Giúp linh hoạt hơn với kiểu chữ hoa/thường của key trong JSON
        };
        var analysisResult = JsonSerializer.Deserialize<TuTruAnalysisResult>(res, options);

        return new(analysisResult);
    }

    private async Task<(string systemPrompt, string userPrompt, string combinedPrompts, LunarInfo laSoBatTu)> GenerateBatTuTuTruPromptAsync(TuTruBatTuRequest request)
    {
        var laSoBatTu = VietnameseCalendar.GetLunarCalendarDetails(request.BirthDateTime).LunarDetails;

        var tutru = laSoBatTu.TuTru;

        string userInfo = $@"
- Họ và tên: {request.Name}
- Giới tính: {request.Gender.GetDescription()}
- Tuổi hiện tại (Dương lịch): {(DateTime.UtcNow.Year - request.BirthDateTime.Year)}
- Năm hiện tại (Dương lịch): {DateTime.UtcNow.Year}
- Ngày giờ sinh Dương lịch: {request.BirthDateTime:dd/MM/yyyy HH:mm} ({request.BirthDateTime.DayOfWeek})
- Ngày giờ sinh Âm lịch: {laSoBatTu.LunarDate.Date:dd/MM/yyyy HH:mm} ({laSoBatTu.LunarDate.Date.DayOfWeek})
- Tiết khí sinh Âm lịch: {laSoBatTu.SolarTerm}
- Múi giờ/vị trí hiện tại: {CurrentRequest().GetClientTimeZoneId()}
- Tứ trụ:
    + Trụ Giờ: {tutru.Hour.ShortDisplay}
       - Can: {tutru.Hour.Can.Display}. Thập thần: [[empty]]
       - Chi: {tutru.Hour.Chi.Display}.
       - Nạp âm: {tutru.Hour.NapAm.Display}
       - Vòng trường sinh: [[empty]]
       - Thần sát: [[empty]]
       - Thập thần: [[empty]]
    + Trụ Ngày: {tutru.Day.ShortDisplay}
       - Can: {tutru.Day.Can.Display}. Thập thần: [[empty]]
       - Chi: {tutru.Day.Chi.Display}.
       - Nạp âm: {tutru.Day.NapAm.Display}
       - Vòng trường sinh: [[empty]]
       - Thần sát: [[empty]]
       - Thập thần: [[empty]]
    + Trụ Tháng: {tutru.Month.ShortDisplay}
       - Can: {tutru.Month.Can.Display}. Thập thần: [[empty]]
       - Chi: {tutru.Month.Chi.Display}.
       - Nạp âm: {tutru.Month.NapAm.Display}
       - Vòng trường sinh: [[empty]]
       - Thần sát: [[empty]]
       - Thập thần: [[empty]]
    + Trụ Năm: {tutru.Year.ShortDisplay}
       - Can: {tutru.Year.Can.Display}. Thập thần: [[empty]]
       - Chi: {tutru.Year.Chi.Display}.
       - Nạp âm: {tutru.Year.NapAm.Display}
       - Vòng trường sinh: [[empty]]
       - Thần sát: [[empty]]
       - Thập thần: [[empty]]
";

        string fillInfoFrompt = $@"
        Bạn là một chuyên gia Bát Tự bậc thầy.
        trước hết hãy đọc toàn bộ thông tin lá số Bát Tự của người dùng để hiểu và nhớ các thông tin đang có.
        sau đó hãy điền các thông tin còn thiếu vào các trường trong mẫu thông tin lá số này.
        các thông tin còn thiếu sẽ có dạng [[empty]]. hãy điền vào vị trí ""empty"", kết quả là các cụm từ phân cách bởi dấu phẩy, luôn được bao bọc bằng [[]] như bản gốc.
        **Kết quả trả về chỉ bao gồm mẫu thông tin đã cung cấp bao gồm các thông tin đã điền vào, không bao gồm bất kỳ giải thích nào khác.**
        **Kết quả trả về phải đầy đủ, không bỏ qua, lược bớt bất kỳ thông tin nào**
        
        Sau đây là thông tin lá số Bát Tự của người dùng:
        {userInfo}
        ";

        userInfo = (await _geminiAIService.SendChatAsync(fillInfoFrompt)).NormalizeAiResponseString();

        var data = userInfo.ExtractDelimitedLists();

        static void AssignTuTru(dynamic target, int offset, List<List<string>> source)
        {
            target.Can.ThapThan = source.Count > offset ? source[offset] : [];
            target.VongTruongSinh = source.Count > offset + 1 ? source[offset + 1] : [];
            target.ThanSat = source.Count > offset + 2 ? source[offset + 2] : [];
            target.ThapThan = source.Count > offset + 3 ? source[offset + 3] : [];
        }

        AssignTuTru(laSoBatTu.TuTru.Hour, 0, data);
        AssignTuTru(laSoBatTu.TuTru.Day, 4, data);
        AssignTuTru(laSoBatTu.TuTru.Month, 8, data);
        AssignTuTru(laSoBatTu.TuTru.Year, 12, data);

        userInfo = userInfo.RemoveDelimiters("[[", "]]");

        string category = request.Category.GetDescription();

        string userPrompt = $@"
Tôi muốn phân tích lá số Bát Tự theo hướng {category}.
Thông tin lá số như sau:

{userInfo}
";

        string systemPrompt = $@"Bạn là một chuyên gia Bát Tự bậc thầy, chuyên luận giải sâu sắc về {category}. Nhiệm vụ của bạn là trả về một cấu trúc JSON DUY NHẤT, mạch lạc, không chứa bất kỳ ký tự nào khác ngoài JSON (không có ```json hay giải thích).

== ĐỊNH HƯỚNG NỘI DUNG ==
1.  **Cốt lõi trước, chi tiết sau**: Trong mỗi mục, hãy điền ""key_point"" bằng vài câu văn súc tích, đắt giá, nêu bật ý chính. Sau đó, ""detailed_analysis"" sẽ diễn giải sâu hơn, dài hơn, cung cấp bối cảnh và lý luận chi tiết, phải liền mạch và tránh lập ý với ""key_point"". Điều này giúp người đọc nắm bắt nhanh ý chính trước khi đi vào chiều sâu. số lượng câu sẽ được quy định cụ thể trong JSON bên dưới.
2.  **Tập trung vào vấn đề và giải pháp**: Luôn phân tích cả mặt mạnh và yếu, nhưng nhấn mạnh vào các khuyết thiếu, mâu thuẫn để phần cải vận trở nên logic và hữu ích.
3.  **Khuyến khích dùng biểu đồ**: Cung cấp dữ liệu chính xác trong ""element_distribution"" để phía giao diện có thể dựng biểu đồ Ngũ Hành, làm cho kết quả sinh động và đáng tin cậy hơn.
4.  **Cải vận cụ thể**: Các gợi ý cải vận phải gắn chặt với Dụng Thần và Kỵ Thần, các thông tin về lá số đã phân tích. các gợi ý phải cụ thể và có ý nghĩa.
5.  **Giữ nguyên tinh thần luận giải**: Tuân thủ nghiêm ngặt tất cả các mục luận giải (được liệt kê ở JSON bên dưới), không thay đổi thứ tự, không bỏ sót. Văn phong chuyên sâu, uyên bác nhưng dễ hiểu, gần gũi.
6.  **Luôn bám sát thông tin lá số mà người dùng cung cấp, được phép tự do suy luận dựa trên đó**: Sử dụng thông tin từ lá số Bát Tự mà người dùng cung cấp để làm cơ sở cho các phân tích và luận giải. Có thể tự tính thêm các thông tin không có trong lá số dựa trên các thông tin đã có, được phép suy luận tự do từ các thông tin trong lá số ấy.
7.  **Luôn đề cập và xưng hô bằng tên gọi của người dùng và tuổi của họ**: Xưng hô tên gọi dựa vào tuổi, ví dụ anh Đạt, chị Hân, cô Nhàn, chú Đức, ông Năm, bà Lan,... thể hiện sự gần gũi, tôn trọng.
8.  **Luôn chú trọng tính mạch lạc trong câu từ, cách hành văn, tránh lập ý, lập từ quá nhiều** Tuy rằng kết quả trả về ở dạng Json, nhưng vẫn cần đảm bảo tính mạch lạc toàn bộ các câu trong đoạn, các đoạn trong section, section trong cả Json. Phải dễ hiểu trong từng câu từ, tránh việc lập ý lập từ quá nhiều, khiến cho người đọc cảm thấy khó hiểu hoặc rối rắm.
9.  **Luận giải phải dựa trên tính thực tế về độ tuổi, khoản thời gian đang đề cập so, năm hiện tại mà người dùng cung cấp và xu thế xã hội hiện tại**: Các luận giải đưa ra phải mang sức thuyết phục và khiến người dùng cảm giác đúng với bản thân và xu hướng xã hội và độ tuổi của họ lúc được đề cập. lưu ý phải lấy năm hiện tại là năm mà người dùng cung cấp, tránh việc lấy năm mà dữ liệu AI được training cuối cùng có thể gây sai lệch về thời gian.
10. **Quy tắc xác định Đại vận đầu tiên**: dựa vào thông tin người dùng cung cấp và quy tắc dưới đây
    - Xác định chiều đại vận (thuận/nghịch):
      + Nam Dương/Nữ Âm (năm sinh can Giáp, Bính, Mậu, Canh, Nhâm): Đại vận đi thuận (từ tháng sinh → tháng sau).
      + Nam Âm/Nữ Dương (năm sinh can Ất, Đinh, Kỷ, Tân, Quý): Đại vận đi nghịch (từ tháng sinh → tháng trước).
      + Ví dụ: Lê Thành Đạt (Nam, năm Đinh Sửu Âm) → Đại vận nghịch.
    - Tính tuổi bắt đầu đại vận:
      + B1: Xác định tiết khí sinh (vd: 18/09/1997 thuộc tiết Bạch Lộ → tháng 8 âm lịch).
      + B2: Tính ngày từ sinh đến tiết gần nhất (nghịch: lùi về Xử Thử = 26 ngày).
      + B3: Chia 3 ngày = 1 năm → 26 ngày ≈ 8 năm 10 tháng → làm tròn 9 tuổi (2006) bắt đầu đại vận.
11. **Luôn chú trọng JSON syntax để không tạo ra lỗi JsonSerializer.Deserialize trước khi trả về, nếu có hãy fix nó.**

== CẤU TRÚC JSON BẮT BUỘC ==
Hãy điền thông tin vào mẫu JSON dưới đây. **không được phép bỏ qua bất cứ mục nào**

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
      ""explanation"": ""[Giải thích lý do chọn các hành này làm Dụng Thần (ưu tiên hành khuyết). Chúng giúp cân bằng lá số và hỗ trợ {category} như thế nào?][ít nhất 10 câu]""
    }},
    ""unfavorable_gods"": {{
      ""elements"": [""[Hành 1]"", ""[Hành 2, nếu có]""],
      ""detailed_analysis"": ""Ngược lại với Dụng Thần, Kỵ Thần của bạn là hành **[Hành 1]** và **[Hành 2, nếu có]**. Đây là những hành khí gây mất cân bằng trong Tứ trụ, do [Giải thích chi tiết lý do, ví dụ: chúng quá vượng hoặc tương khắc mạnh với Nhật Chủ/Dụng Thần]. Khi các yếu tố Kỵ Thần này bị kích hoạt, bạn có thể gặp phải những rắc rối, cản trở, thậm chí là tai ương. Trong **{{category}}**, điều này có thể biểu hiện dưới dạng [Nêu ví dụ cụ thể về ảnh hưởng tiêu cực, ví dụ: khó khăn trong tài chính, mâu thuẫn trong các mối quan hệ, hoặc gặp phải những quyết định sai lầm dẫn đến thua lỗ]. Việc nhận diện và tìm cách chế hóa Kỵ Thần là vô cùng cần thiết để giảm thiểu rủi ro và bảo vệ vận may của bạn.[ít nhất 15 câu]"",
      ""explanation"": ""[Giải thích lý do các hành này là Kỵ Thần. Chúng gây mất cân bằng và cản trở {category} ra sao?][ít nhất 10 câu]""
    }},    
  }},
  ""ten_year_cycles"": {{
    ""title"": ""Đại Vận - đến những năm 80 tuổi - Chu kỳ 10 năm"",
    ""key_point"": ""[Nhận định chung về các giai đoạn Đại Vận sắp tới: thuận lợi hay khó khăn hơn cho mục tiêu {category}? bố cục nhận định từ lúc sinh ra đến năm 80 tuổi, chu kỳ 10 năm][ít nhất 10 câu]"",
    ""detailed_analysis"": ""[Diễn giải về xu hướng chung của các Đại Vận, giải thích tại sao lại có xu hướng đó. bố cục nhận định từ lúc có đại vận đầu tiên đến đại vận những năm 80 tuổi, chu kỳ 10 năm][ít nhất 20 câu]"",
    ""cycles"": [
      {{ ""age_range"": ""[VD: 9-19 dựa theo **Quy tắc xác định Đại vận đầu tiên** và thông tin người dùng cung cấp]"", ""can_chi"": ""[VD: Canh Dần]"", ""element"": ""[VD: Mộc]"", ""analysis"": ""[Phân tích sâu giai đoạn này: Can Chi của Đại Vận tương tác với Tứ trụ ra sao, ảnh hưởng đến {category} như thế nào (tốt/xấu, cơ hội/thách thức). có phải là giai đoạn bức phá hay không? nếu có thì cần tận dụng gì?] [phân tích dựa trên xu hướng xã hội và độ tuổi của giai đoạn này để tránh đưa ra các luận giải không phù hợp với thực tế]"" }},
      {{ ""age_range"": ""[VD: 19-29 chu kỳ 10 năm]"", ""can_chi"": ""[VD: Tân Mão]"", ""element"": ""[VD: Mộc]"", ""analysis"": ""[Phân tích tương tự cho đại vận tiếp theo.]"" }}
      ... đến những năm 80 tuổi, chu kỳ 10 năm
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
      ...tối đa 5 vật phẩm
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

        return (systemPrompt, userPrompt, combinedPrompts, laSoBatTu);
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
