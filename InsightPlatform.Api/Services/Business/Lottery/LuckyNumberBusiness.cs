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
                var key = DateOnly.FromDateTime(DateTime.ParseExact(item.Key, "dd-MM-yyyy", null));

                var existed = await context.LuckyNumberRecords.FirstOrDefaultAsync(f => f.Date == key);
                if (existed == null)
                {
                    existed = new LuckyNumberRecord
                    {
                        Date = key,
                        ProviderId = providerEntity.Id,
                        CrawlUrl = item.Value.CrawlUrl,
                        Detail = JsonSerializer.Serialize(item.Value),
                        CreatedTs = DateTime.UtcNow,
                    };

                    await context.LuckyNumberRecords.AddAsync(existed);
                    await context.SaveChangesAsync();
                }
                else if (isOverride)
                {
                    existed.CrawlUrl = item.Value.CrawlUrl;
                    existed.ProviderId = providerEntity.Id;
                    existed.Detail = JsonSerializer.Serialize(item.Value);

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

            foreach (var item in list)
            {
                var data = JsonSerializer.Deserialize<LuckyNumberCrawledDto>(item.Detail);

                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "ĐB");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G1");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G2");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G3");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G4");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G5");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G6");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G7");
                await ExtractByPrizeTypeAsync(isOverride, context, item, data, "G8");

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
}
