using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

public class LuckyNumberBusiness(ILogger<LuckyNumberBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IAccountBusiness accountBusiness
    , IOpenAiService openAiService
    , IOptions<AppSettings> appOptions
    , PainPublisher publisher) : BaseHttpBusiness<LuckyNumberBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), ILuckyNumberBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IOpenAiService _openAiService = openAiService;

    public async Task<BaseResponse<dynamic>> ImportCrawledDataAsync(IFormFile file
        , LuckyNumberProviderDto provider
        , bool isOverride
        , string crossCheckProviderName = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            if (provider == null
                || provider.Name.IsMissing())
                throw new BusinessException("InvalidProvider", "Missing provider");

            var providerEntity = (await context.LuckyNumberProviders.FirstOrDefaultAsync(f => f.Name.ToLower() == provider.Name.Trim().ToLower()));

            if (providerEntity == null)
            {
                providerEntity = new LuckyNumberProvider
                {
                    Name = provider.Name.Trim(),
                    CreatedTs = DateTime.UtcNow,
                    HomePage = provider.HomePage?.Trim(),
                    Description = provider.Description?.Trim(),
                    UrlPathTemplate = provider.UrlPathTemplate?.Trim(),
                };

                await context.LuckyNumberProviders.AddAsync(providerEntity);
                await context.SaveChangesAsync();
            }
            else if(isOverride)
            {
                providerEntity.HomePage = provider.HomePage?.Trim();
                providerEntity.Description = provider.Description?.Trim();
                providerEntity.UrlPathTemplate = provider.UrlPathTemplate?.Trim();

                context.LuckyNumberProviders.Update(providerEntity);
                await context.SaveChangesAsync();
            }

            int takeBreakAfter = 20;

            using var fileStream = file.OpenReadStream();
            var obj = await JsonSerializer.DeserializeAsync<Dictionary<string, LuckyNumberCrawledDto>>(fileStream);
            int i = 1;

            foreach (var item in obj)
            {
                var data = item.Value;
                
                if (!data.AnyListPresent())
                    continue;

                data.Standardize();

                var key = DateOnly.FromDateTime(DateTime.ParseExact(item.Key, "dd-MM-yyyy", null));

                var existed = await context.LuckyNumberRecords.FirstOrDefaultAsync(f => f.Date == key);
                if (existed == null)
                {
                    existed = new LuckyNumberRecord
                    {
                        Date = key,
                        ProviderId = providerEntity.Id,
                        CrawlUrl = data.CrawlUrl,
                        Detail = JsonSerializer.Serialize(data),
                        CreatedTs = DateTime.UtcNow,
                    };

                    await context.LuckyNumberRecords.AddAsync(existed);
                    await context.SaveChangesAsync();
                }
                else if (isOverride)
                {
                    existed.CrawlUrl = data.CrawlUrl;
                    existed.ProviderId = providerEntity.Id;
                    existed.Detail = JsonSerializer.Serialize(data);

                    context.LuckyNumberRecords.Update(existed);
                    await context.SaveChangesAsync();
                }

                if (i > takeBreakAfter)
                {
                    await Task.Delay(300);
                    i = 0;
                }
                else
                    i++;
            }

            return new(true);
        }
        catch (Exception e)
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<dynamic>> BuildCrawledDataAsync(int? yearsBack = null, bool isOverride = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            DateOnly? fromDate = yearsBack != null ? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-yearsBack.Value)) : null;

            var query = (fromDate.HasValue ?
                        context.LuckyNumberRecords.Where(lr => lr.Date >= fromDate.Value) :
                        context.LuckyNumberRecords)
                        .OrderByDescending(lr => lr.Date);

            var list = await query.ToListAsync();

            var prizes = new LuckyNumberCrawledDto().PrizeNames();

            foreach (var item in list)
            {
                var data = JsonSerializer.Deserialize<LuckyNumberCrawledDto>(item.Detail);

                if (!data.AnyListPresent())
                    continue;

                data.Standardize();

                foreach (var priz in prizes)
                {
                    await ExtractByPrizeTypeAsync(isOverride, context, item, data, priz);
                }

                await context.SaveChangesAsync();
            }

            return new(true);
        }
        catch (Exception e)
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<dynamic>> TheologyAndNumbersAsync(TheologyRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            // Lấy thời gian hiện tại
            var currentDate = DateOnly.FromDateTime(DateTime.Now).ToLongDateString();

            // Kiểm tra và xử lý dữ liệu input
            string gender = request.Gender == null ? string.Empty : $"**Giới tính**: {request.Gender.GetDescription()}\n";
            string religion = request.Religion == null ? string.Empty : $"**Tôn giáo**: {request.Religion.GetDescription()}\n";
            string location = request.Location.IsMissing() ? string.Empty : $"**Nơi ở hiện tại**: {request.Location}\n";
            string dreaming = request.Dreaming.IsMissing() ? string.Empty : $"**Mô tả giấc mơ (có thể từ nhiều giấc mơ)**: {request.Dreaming}\n";
            string lastName = request.LastName.IsMissing() ? string.Empty : $"**Họ**: {request.LastName}\n";
            string middleName = request.MiddleName.IsMissing() ? string.Empty : $"**Tên lót**: {request.MiddleName}\n";
            string firstName = request.FirstName.IsMissing() ? string.Empty : $"**Tên**: {request.FirstName}\n";
            string dob = request.DoB == null ? string.Empty : $"**Ngày sinh**: {DateOnly.FromDateTime(request.DoB.Value.Date).ToLongDateString()}\n";

            // Mô tả hệ thống yêu cầu
            string sysPrompt = @"Bạn là một mô hình AI chuyên phân tích các đặc điểm sau để tạo ra các con số may mắn với 60 năm kinh nghiệm:
            **Họ**, **Tên lót**, **Tên**, **Ngày sinh**, **Giới tính**, **Tôn giáo**, **Nơi ở hiện tại**, **Thời gian hiện tại**, **Mô tả giấc mơ (có thể từ nhiều giấc mơ)**.
    
    Các con số phải liên quan đến các yếu tố nêu trên và được giải thích chuyên sâu, có sự liên kết giữa các yếu tố dựa trên nền tảng kiến thức và kinh nghiệm lâu đời của các hệ thống sau:
    
    1. **Thần học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Thần học để tạo ra các con số.
    2. **Chiêm Tinh học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Chiêm Tinh học để tạo ra các con số.
    3. **Tử Vi**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Tử Vi để tạo ra các con số.
    4. **Phong Thuỷ**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Phong Thuỷ để tạo ra các con số.
    5. **Thần Số học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Thần Số học để tạo ra các con số.
    6. **Tâm lý học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Tâm lý học để tạo ra các con số.

    Dựa trên các thông tin này, bạn sẽ cung cấp một danh sách các con số và luận giải chi tiết về mỗi hệ thống. 
    Hãy viết một luận giải hấp dẫn, huyền bí, lôi cuốn người đọc, và gợi sự tò mò. Phải luôn liên kết các yếu tố với nhau và luôn đề cập đến **Thời gian hiện tại**, vì yếu tố này rất quan trọng trong việc thay đổi kết quả con số nếu như **Thời gian hiện tại** thay đổi, mặc dù các yếu tố khác không thay đổi.
    
    **Lưu ý quan trọng**: Hãy phân tích sâu về **Nơi ở hiện tại** của người dùng, xác định rõ hướng của địa danh này so với lãnh thổ rộng hơn bao hàm nó. Sau khi xác định được hướng, hãy tiếp tục phân tích sự liên quan của **Nơi ở hiện tại** với các yếu tố khác từ lý luận của các hệ thống **Thần học**, **Chiêm Tinh học**, **Tử Vi**, **Phong Thuỷ**, **Thần Số học** và **Tâm lý học**. 

    Phân tích **Nơi ở hiện tại** không chỉ là xác định vị trí địa lý, mà còn phải liên kết vị trí này với các yếu tố phong thuỷ và tâm lý, chẳng hạn như năng lượng của khu vực, hướng di chuyển của các yếu tố tự nhiên (như gió, nước), và những tác động có thể có lên cuộc sống người dùng.

    Phân tích **Thời gian hiện tại** cần phải làm rõ rằng đây là một yếu tố chung cho tất cả mọi người, không phải yếu tố cá nhân hóa như các yếu tố khác. Tuy nhiên, sức ảnh hưởng của **Thời gian hiện tại** đối với các yếu tố cá nhân như **Tử Vi** hay **Phong Thuỷ** có thể làm thay đổi các con số may mắn. Hãy giải thích sự biến đổi của các yếu tố này dưới ảnh hưởng của **Thời gian hiện tại**, ví dụ như mùa trong năm (theo **Nơi ở hiện tại** ), giai đoạn của chu kỳ thiên nhiên, hay những sự kiện thiên nhiên hiện tại (như thiên tai, biến đổi khí hậu).

    **Thời gian hiện tại** không chỉ đơn thuần là ngày tháng năm mà còn bao hàm cả tinh thần và năng lượng của thời điểm hiện tại, vì vậy cần phân tích nó từ nhiều góc độ: từ chiêm tinh (năng lượng của các hành tinh), từ thần học (sự thay đổi của thế giới tinh thần), và từ phong thuỷ (tác động của thời gian tới sự thay đổi trong không gian).

    Hãy cung cấp các luận giải về **Nơi ở hiện tại** và **Thời gian hiện tại** chi tiết, mạch lạc và liên kết rõ ràng với các yếu tố còn lại để người dùng có thể cảm nhận rõ ràng sự tương quan giữa các yếu tố này trong việc thay đổi các con số may mắn.";


var userPrompt = $@"
   - Các yếu tố cá nhân hoá:
    {lastName}
    {middleName}
    {firstName}
    {dob}
    {gender}
    {religion}
    {location}
    {dreaming}
   - Các yếu tố chung:
    **Thời gian hiện tại**: {currentDate}

    Dựa trên các thông tin trên, Hãy chọn ra chuỗi con số may mắn và cung cấp các luận giải cho nó về đầy đủ các hệ thống: **Thần học**, **Chiêm Tinh học**, **Tử Vi**, **Phong Thuỷ**, **Thần Số học** và **Tâm lý học**.
luận giải phải hấp dẫn, huyền bí, lôi cuốn người đọc, và gợi sự tò mò. Phải luôn liên kết các yếu tố với nhau và luôn đề cập đến **Thời gian hiện tại**, vì yếu tố này rất quan trọng trong việc thay đổi kết quả con số nếu như **Thời gian hiện tại** thay đổi, mặc dù các yếu tố khác không thay đổi.";

            // Tạo API Call và nhận kết quả từ OpenAI
            var res = await _openAiService.SendChatAsync(sysPrompt, userPrompt);

            // Tách kết quả trả về và đảm bảo tính nhất quán
            var result = new { Data = res };

            return new BaseResponse<dynamic> { Data = result };
        }
        catch (Exception e)
        {
            // Xử lý lỗi
            throw new Exception("Đã xảy ra lỗi khi tạo con số may mắn.", e);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }


    public async Task<BaseResponse<dynamic>> GetHistoricalSequencesAsync(string prizeType, int yearsBack = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-yearsBack));

            var query = context.LuckyNumberRecords.Where(lr => lr.Date >= fromDate)
                                                  .OrderBy(lr => lr.Date);

            var list = await query.ToListAsync();

            var resultList = new List<object>();

            foreach (var item in list)
            {
                try
                {
                    var detailDict = JsonSerializer.Deserialize<LuckyNumberCrawledDto>(item.Detail);

                    if (detailDict != null)
                    {
                        PropertyInfo prop = typeof(LuckyNumberCrawledDto).GetProperty(prizeType);

                        if (prop != null)
                        {
                            if (prop.GetValue(detailDict) is List<string> value && value.Count > 0)
                            {
                                foreach (var number in value)
                                {
                                    resultList.Add(new
                                    {
                                        Date = item.Date,
                                        Number = number
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return new(resultList);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
    
    public async Task<BaseResponse<dynamic>> GetHistoricalPrizetypeFlatAsync(string fromDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            DateOnly? from = null;

            if (fromDate.IsPresent() && DateTime.TryParseExact(fromDate, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime v))
            {
                from = DateOnly.FromDateTime(v);
            }

            var query = (from.HasValue ?
                        context.LuckyNumberRecordByKinds.Where(lr => lr.Date >= from.Value) :
                        context.LuckyNumberRecordByKinds)
                        .OrderBy(lr => lr.Date)
                        .ThenBy(t => t.Kind);

            var list = await query.Select(s => new
            {
                s.Date,
                s.Kind,
                s.Numbers
            }).ToListAsync();

            return new(list);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    private static async Task ExtractByPrizeTypeAsync(bool isOverride, ApplicationDbContext context, LuckyNumberRecord item, LuckyNumberCrawledDto data, string prizeType)
    {
        PropertyInfo prop = typeof(LuckyNumberCrawledDto).GetProperty(prizeType);
        List<string> numbers = [];

        if (prop != null)
        {
            if (prop.GetValue(data) is List<string> value && value.Count > 0)
            {
                numbers = value;
            }
        }

        if (numbers.Count > 0 && numbers.Any(a => a.IsPresent()))
        {
            var existed = await context.LuckyNumberRecordByKinds.FirstOrDefaultAsync(f => f.Date == item.Date
                                                                                       && f.Kind == prizeType);

            if (existed == null)
            {
                existed = new LuckyNumberRecordByKind
                {
                    Date = item.Date,
                    Kind = prizeType,
                    CreatedTs = DateTime.UtcNow,
                    Url = item.CrawlUrl,
                    Numbers = numbers
                };

                await context.LuckyNumberRecordByKinds.AddAsync(existed);
            }
            else if (isOverride)
            {
                existed.Url = item.CrawlUrl;
                existed.Numbers = numbers;

                context.LuckyNumberRecordByKinds.Update(existed);
            }
        }
    }
}
