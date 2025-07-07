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

        var res = await GetTuTruBatTuWithPaymentStatus(existed.Id, context);

        return new(new
        {
            Id = id,
            Status = (TheologyStatus)existed.Status,
            Input = existed.Input.IsPresent() ? JsonSerializer.Deserialize<TuTruBatTuRequest>(existed.Input) : null,
            PreData = existed.PreData.IsPresent() ? JsonSerializer.Deserialize<LaSoBatTuResponse>(existed.PreData) : null,
            Result = res.Data,
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
            {
                throw new BusinessException("Unauthorized", "401 Unauthorized");
            }

            var servicePrice = await GetServicePriceByTheologyKind(kind, context);

            if (servicePrice == null)
            {
                throw new BusinessException("InvalidSystemData", $"Service price not found for kind '{kind}'");
            }

            var key = request.InitUniqueKey(kind, null, null);

            var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.UniqueKey == key);            

            if (existed != null)
            {
                if (existed.UserId != userId)
                {
                    var cloned = new TheologyRecord
                    {
                        UserId = userId.Value,
                        UniqueKey = existed.UniqueKey,
                        Kind = existed.Kind,
                        Input = existed.Input,
                        Status = existed.Status,
                        PreData = existed.PreData,
                        SystemPrompt = existed.SystemPrompt,
                        UserPrompt = existed.UserPrompt,
                        Result = existed.Result,
                        CreatedTs = DateTime.UtcNow,
                        ServicePrice = servicePrice,
                    };

                    await context.TheologyRecords.AddAsync(cloned);
                    await context.SaveChangesAsync();

                    existed = cloned;
                }
            }
            else
            {
                var lunarBase = VietnameseCalendar.GetLunarDate(request.birthDateTime.Day, request.birthDateTime.Month, request.birthDateTime.Year, request.birthDateTime.Hour);
                var lunarBirthDateTime = new DateTime(lunarBase.Year, lunarBase.Month, lunarBase.Day, lunarBase.Hour, request.birthDateTime.Minute, 0, 0);

                var laSoBatTu = (await _phongThuyNhanSinhService.BuildLaSoBatTuAsync(request.name
                                    , request.gender
                                    , request.birthDateTime
                                    , lunarBirthDateTime))
                                .FirstOrDefault();

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
                };

                await context.TheologyRecords.AddAsync(existed);
                await context.SaveChangesAsync();
            }

            if (existed.Result.IsMissing())
                FuncTaskHelper.FireAndForget(() => ExplainTuTruBatTuAsync(existed.Id, 0));

            return new(new
            {
                Id = existed.Id,
                LaSoBatTu = existed.PreData.IsPresent() ? JsonSerializer.Deserialize<LaSoBatTuResponse>(existed.PreData) : null,
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

    public async Task<BaseResponse<TheologyBaseResult<string, string>>> ExplainTuTruBatTuAsync(Guid id, int retry)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;
        var kind = TheologyKind.TuTruBatTu;

        var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.Id == id
                                                                          && f.UserId == userId);

        try
        {
            if (existed == null)
                throw new BusinessException("TuTruBatTuNotFound", "TuTruBatTu not found");

            if (existed.Status == (byte)TheologyStatus.Analyzing)
            {
                var res = await WaitAndGetTuTruBatTuAsync(existed.Id, context, 60);
                if (res)
                    return await GetTuTruBatTuWithPaymentStatus(existed.Id, context);
                else
                {
                    await Task.Delay(1000);

                    if (retry <= 0)
                        throw new BusinessException("ReachRetryLimitToExplainTuTruBatTu", "Reach retry limit to explain TuTruBatTu");

                    return await ExplainTuTruBatTuAsync(id, --retry);
                }
            }

            if (existed.Result.IsPresent())
            {
                return await GetTuTruBatTuWithPaymentStatus(existed.Id, context);
            }

            existed.Status = (byte)TheologyStatus.Analyzing;
            context.TheologyRecords.Update(existed);
            await context.SaveChangesAsync();

            var request = JsonSerializer.Deserialize<TuTruBatTuRequest>(existed.Input);

            request.Standardize();

            LaSoBatTuResponse laSoBatTu = null;

            var lunarBase = VietnameseCalendar.GetLunarDate(request.birthDateTime.Day, request.birthDateTime.Month, request.birthDateTime.Year, request.birthDateTime.Hour);
            var lunarBirthDateTime = new DateTime(lunarBase.Year, lunarBase.Month, lunarBase.Day, lunarBase.Hour, request.birthDateTime.Minute, 0, 0);

            if (existed.PreData.IsMissing())
            {
                laSoBatTu = (await _phongThuyNhanSinhService.BuildLaSoBatTuAsync(request.name
                                    , request.gender
                                    , request.birthDateTime
                                    , lunarBirthDateTime))
                                .FirstOrDefault();

                if (laSoBatTu != null)
                {
                    existed.PreData = JsonSerializer.Serialize(laSoBatTu);
                    context.TheologyRecords.Update(existed);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                laSoBatTu = JsonSerializer.Deserialize<LaSoBatTuResponse>(existed.PreData);
            }

            if (laSoBatTu == null)
            {
                existed.Status = (byte)TheologyStatus.Created;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();
                throw new BusinessException("FailedToAnalyze", "Failed to analyze TuTruBatTu");
            }

            string category = request.category.GetDescription();

            var tutru = laSoBatTu.Tutru;

            string userPrompt = $@"
Tôi muốn phân tích lá số Bát Tự theo hướng {category}.
Thông tin như sau:

- Họ và tên: {request.name}
- Giới tính: {request.gender.GetDescription()}
- Ngày giờ sinh Dương lịch: {request.birthDateTime:dd/MM/yyyy HH:mm} ({request.birthDateTime.DayOfWeek})
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

            string systemPrompt = $@"Bạn là một chuyên gia Bát Tự chuyên phân tích {category}. Hãy trả lời theo format cố định bên dưới để mọi kết quả luôn nhất quán, dù cùng một thông tin được hỏi nhiều lần. Mục tiêu là đưa ra luận giải mạch lạc, dễ hiểu nhưng đầy đủ chiều sâu huyền học.

== ĐỊNH HƯỚNG TRẢ LỜI ==

0. Nên nhớ rằng kết quả bạn đưa ra sẽ được dùng để hiển thị trên một website dịch vụ xem phong thuỷ chuyên nghiệp, nên hãy quả qua các câu nói dư thừa như lời chào, lời giới thiệu về bản thân bạn, hoặc kiểu nêu ra câu hỏi dẫn từ các hướng dẫn của tôi bên dưới,... những câu nói này trên hệ thống website đó sẽ tự xử lý riêng.
   **Tôi không muốn người dùng khi đọc được kết quả này biết là từ AI sinh ra**
   **Nội dung hiển thị ở dạng mã html, tailwind css**

1. Trả lời theo đúng **thứ tự 8 mục bên dưới**, **không được thay đổi**, **không rút gọn**, **không bỏ mục**.

2. Mỗi phần **phải phân tích đầy đủ mặt mạnh – mặt yếu**, nhưng đặc biệt **tập trung vào khuyết thiếu – hành xấu – mâu thuẫn** để dẫn dắt đến **phần cải vận**.

3. Phân tích **dụng thần** là trọng tâm – ưu tiên theo hành khuyết, nhưng **phải giải thích kỹ nếu không dùng hành khuyết làm dụng thần**, có thể do bị xung khắc hoặc thiên lệch hệ thống.

4. Phần **cải vận phải cụ thể – logic – gắn kết chặt chẽ với phân tích dụng thần và vận khí bên trên**. Tuyệt đối **không trả lời chung chung**.

5. **Gợi ý cải vận bằng vật phẩm phong thủy phải có ít nhất 10 vật phẩm**, chia theo nhiều nhóm, mỗi món phải rõ **hành khí – chất liệu – công dụng – cách dùng**, mỗi món phải đi kèm thẻ **<MetaItem />**. Đây là phần quan trọng và không thể thiếu. 

6. Văn phong chuyên sâu, dễ hiểu, gần gũi.

7. Tất cả các yêu cầu nào có thẻ <Metadata></Metadata> phải giữ lại format thẻ này để phục vụ trích xuất thông tin.

8. **Ngoại trừ phần nội dung bên trong thẻ <Metadata></Metadata> ra thì các nội dung còn lại nên được trang trí bằng
html, tailwind css đơn giản vì tôi sẽ dùng nó nhúng vào website của tôi cũng dùng tailwind css. 
phong cách chủ đạo là huyền bí, huyền học, tone đen, vàng, cam. đặc biệt cần phải tô màu hoặc in đậm (bằng html) các yếu tố như là ngũ hành, .... chọn màu đúng với đặc tính của nó, ví dụ: hành Kim thì màu vàng, ...
**

== FORMAT TRẢ LỜI CỐ ĐỊNH ==

🔹 **Nhật Chủ – Khí chất tổng quan**
Phân tích Nhật Can: Âm Dương, Ngũ Hành, Nạp Âm
Đặc tính Thiên Can – Địa Chi trụ ngày
Áp dụng nguyên tắc phân tích mạnh – yếu – khuyết – xung – cải vận
**Phải đề cập, dẫn chứng liên quan đến {category}**

🔹 **Cục diện Ngũ Hành – Vượng suy**
Tổng số lượng hành – vượng suy từng hành một (Kim – Mộc – Thủy – Hỏa – Thổ)
Nhận định: mệnh thân vượng hay nhược, Thiên lệch ngũ hành, mâu thuẫn tử trụ, tương khắc...
Tập trung khai thác khuyết thiếu – hành xấu – mâu thuẫn để dẫn dắt đến phần cải vận.
Áp dụng nguyên tắc phân tích mạnh – yếu – khuyết – xung – cải vận
**Phải đề cập, dẫn chứng liên quan đến {category}**

🔹 **Thập Thần – Bản chất vận hạn chính**
Phân tích các thần liên quan đến {category}
Có các cách cục hay thần sát đặc biệt không? nếu có thì phân tích chi tiêt vào, nếu không cũng nêu ra là không có.
Có lộ các Thập Thần quan trọng không? (Tài – Quan – Thực – Ấn – Tỷ) nếu có thì phân tích chi tiết vào, nếu không cũng nêu ra là không.
tập trung khai thác khuyết thiếu – hành xấu – mâu thuẫn để dẫn dắt đến phần cải vận.
**Phải đề cập, dẫn chứng liên quan đến {category}**

🔹 **Dụng Thần – Kỵ Thần**
Dụng Thần chính và phụ (nếu có): lý do chọn (phải ưu tiên chọn hành khuyết), giải thích nếu loại trừ hành khuyết. **Ảnh hưởng như thế nào đến {category} của người dùng**
Kỵ Thần cần tránh: lý do chọn, nguyên nhân. **ảnh hưởng như thế nào đến {category} của người dùng**

**Thêm cố định thẻ meta này:**
<Metadata>
Đặt metadata cho Dụng Thần, và Kỵ Thần có dạng như ví dụ sau đây (nhớ bỏ phần ví dụ đi):
  <MetaDungThan NguHanhBanMenh='Kim,Thủy' />
  <MetaKyThan NguHanhBanMenh='Hỏa,Thổ' />
</Metadata>

🔹 **Đại Vận – Chu kỳ vận hạn 10 năm**
Trình bày theo bảng: tuổi, Can Chi, hành khí
Phân tích thuận – nghịch vận từng giai đoạn
phân tích thật sâu vào từng giai đoạn, lý do tại sao, ...
**Phải đề cập, dẫn chứng liên quan đến {category}**
Áp dụng nguyên tắc phân tích mạnh – yếu – khuyết – xung – cải vận

🔹 **Ngành nghề hoặc mô hình phù hợp với mục tiêu {category}**
Gợi ý dựa trên dụng thần, khí chất
Định hướng mô hình: làm thuê – làm chủ – đầu tư – cố vấn – freelancer
Áp dụng nguyên tắc phân tích mạnh – yếu – khuyết – xung – cải vận

🔹 **Gợi ý cải vận – Kích hoạt khí vận theo mục tiêu {category}**
Gợi ý hành trì, môi trường sống tương ứng
Gợi ý bổ sung để tăng cường điểm mạnh, tránh ảnh hưởng đến Kỵ Thần
Dẫn nhập, gợi ý người dùng nên sử dụng thêm các sản phẩn có hành khí, chất liệu, công dụng phù hợp với **Dụng Thần**, **Kỵ Thần**

**Thêm cố định thẻ meta này:**
<Metadata>
   Đặt metadata **GỢI Ý ÍT NHẤT 10 VẬT PHẨM PHONG THỦY**: Ưu tiên vật đeo: vòng tay, cổ, dây chuyền,... . Kèm vật phẩm phụ: linh vật – đá – đồ để bàn – vật phẩm cầu tài, ...
    **Đặt mỗi vật phẩm gợi ý vào thẻ MetaItem có dạng như ví dụ sau đây (nhớ bỏ phần ví dụ đi):
    <MetaItem Ten='Thiềm Thừ' HanhKhi='Thủy,Mộc' ChatLieu='Đá Obsidian' CongDung='Chiêu tài' CachDung='Đặt tại bàn làm việc hướng Bắc' />
</Metadata>

🔹 **Lời kết tổng quan**
Tóm tắt ưu/nhược trong {category}
Nhấn mạnh **hành động cần làm** để cải hóa và phát huy
**Đặc biệt nhấn mạnh vai trò và sự cần thiết của việc sử dụng ít nhất 10 vật phẩm phong thủy cải vận**

===> **TRẢ LỜI BẮT BUỘC THEO ĐÚNG FORMAT TRÊN**. KHÔNG VIẾT CHUNG CHUNG. KHÔNG RÚT GỌN. KHÔNG BỎ MỤC. LUẬN GIẢI RÕ – GỢI Ý CỤ THỂ.

Sau đây là yêu cầu và thông tin của người dùng:
{userPrompt}
";

            var key = request.InitUniqueKey(kind, systemPrompt, null);

            var _existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.UniqueKey == key 
                                                                               && f.Result != null);

            if (_existed != null)
            {
                existed.Result = _existed.Result;
            }
            else
            {
                var res = await _geminiAIService.SendChatAsync(systemPrompt);

                if (res.IsPresent())
                {
                    res = res.Replace("```html", string.Empty);

                    res = res.Replace("```json", string.Empty);

                    if (res.EndsWith("```"))
                    {
                        res = res.TrimEnd('`');
                    }
                }

                var (processedInput, metaData) = MetadataExtractor.Extract(res, true);

                existed.Result = JsonSerializer.Serialize(new TuTruBatTuDto
                {
                    Original = processedInput,
                    MetaData = metaData,
                });
            }

            existed.UniqueKey = key;
            existed.Status = (byte)TheologyStatus.Analyzed;

            context.TheologyRecords.Update(existed);
            await context.SaveChangesAsync();

            return await GetTuTruBatTuWithPaymentStatus(existed.Id, context);
        }
        catch (Exception e)
        {
            if (existed != null)
            {
                existed.Status = (byte)TheologyStatus.Created;
                context.TheologyRecords.Update(existed);
                await context.SaveChangesAsync();
            }
            throw new BusinessException("UnavailableToTuTruBatTu", "Unavailable to TuTruBatTu.", e);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
    
    public async Task<BaseResponse<int>> PaidTheologyRecordAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var userId = Current.UserId;

        var service = await context.TheologyRecords.Include(i => i.FatePointTransactions)
                                                   .Include(i => i.ServicePrice)
                                                   .FirstOrDefaultAsync(f => f.Id == id
                                                                          && f.UserId == userId);

        try
        {
            if (service == null)
                throw new BusinessException("NotFound", "Id not found");

            if (service.FatePointTransactions.Count != 0)
                throw new BusinessException("Paid", "This service paid");

            var fates = await _transactionBusiness.RecalculateUserFates(userId.Value);

            var serviceFates = service.ServicePrice.GetFinalFates();

            if (fates < serviceFates)
                throw new BusinessException("FatesNotEnough", "Fates are not enough to proceed");

            service.FatePointTransactions.Add(new FatePointTransaction
            {
                UserId = userId.Value,
                Fates = -serviceFates,
                CreatedTs = DateTime.UtcNow,
            });

            context.TheologyRecords.Update(service);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            fates = await _transactionBusiness.RecalculateUserFates(userId.Value);
            return new(fates);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
            await context.DisposeAsync();
        }
    }

    private static async Task<BaseResponse<TheologyBaseResult<string, string>>> GetTuTruBatTuWithPaymentStatus(Guid theologyRecordId
        , ApplicationDbContext context)
    {
        var result = new TheologyBaseResult<string, string>();

        var existed = await context.TheologyRecords.Include(i => i.FatePointTransactions)
                                                   .FirstOrDefaultAsync(f => f.Id == theologyRecordId);

        if (existed.FatePointTransactions.Count != 0)
        {
            if (existed.Result.IsPresent())
            {
                var res = JsonSerializer.Deserialize<TuTruBatTuDto>(existed.Result);
                result.PResult = res.Original;
            }
            else
            {
                result.PResult = "Error occurred! Please try again later or contact the admin for assistance";
            }
        }
        else
        {
            result.FResult = string.Empty;
        }

        return new BaseResponse<TheologyBaseResult<string, string>>(result);
    }

    private async Task<bool> WaitAndGetTuTruBatTuAsync(Guid id, ApplicationDbContext context, int seconds)
    {
        while (seconds > 0)
        {
            var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.Id == id
                                                                              && f.Kind == (short)TheologyKind.TuTruBatTu)
                          ?? throw new BusinessException("TuTruBatTuNotFound", "TuTruBatTu not found");

            if (existed.Status != (byte)TheologyStatus.Analyzing)
            {
                return existed.Result?.IsPresent() == true;
            }

            seconds--;
            await Task.Delay(1000);
        }

        return false;
    }

    private static async Task<ServicePrice> GetServicePriceByTheologyKind(TheologyKind topUpKind, ApplicationDbContext context)
    {
        return await context.ServicePrices.FirstOrDefaultAsync(f => f.ServiceKind == (byte)topUpKind);
    }
}
