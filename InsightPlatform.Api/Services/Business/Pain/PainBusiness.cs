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

            var a = Current.CurrentCulture?.Name;

            var entity = new Pain
            {
                Id = Guid.NewGuid(),
                PainDetail = dto.Pain,
                Desire = dto.Desire,
                DeviceId = ua,
                CreatedAt = DateTime.UtcNow
            };

            await context.Pains.AddAsync(entity);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _publisher.SubmitPainAsync(entity.Id, entity.PainDetail, entity.Desire, ua);

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
}

public record PainDto(string Pain, string? Desire);