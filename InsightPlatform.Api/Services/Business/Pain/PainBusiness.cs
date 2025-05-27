using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

public class PainBusiness(ILogger<PainBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IOptions<AppOptions> appOptions
    , PainPublisher publisher) : BaseHttpBusiness<PainBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IPainBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppOptions _appSettings = appOptions.Value;

    public async Task<BaseResponse<dynamic>> InsertPain(PainDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var ua = Current.UA?.RawUserAgent;

            var entity = new Pain
            {
                Id = Guid.NewGuid(),
                PainDetail = dto.Pain,
                Desire = dto.Desire,
                //DeviceId = ua, //todo
                UserAgent = ua,
                ClientLocale = Current.CurrentCulture?.Name,
                CreatedAt = DateTime.UtcNow
            };

            await context.Pains.AddAsync(entity);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _publisher.PainLabelingAsync(entity.Id);

            return new(new
            {
                TrackLink = _appSettings.FeDomain.WithPath($"track/{entity.Id}")
            });
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
    
    public async Task<BaseResponse<dynamic>> PainLabelingAsync(Guid painId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var entity = await context.Pains.FirstOrDefaultAsync(f => f.Id == painId);

            if (entity != null)
            {

            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new(true);
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
}

public record PainDto(string Pain, string? Desire);