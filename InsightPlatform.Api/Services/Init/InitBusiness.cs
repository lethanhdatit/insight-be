using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public interface IInitBusiness
{
    Task InitServicePrices();

    Task InitTopUpPackages();
}

public class InitBusiness(ILogger<BocMenhBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor) : BaseHttpBusiness<BocMenhBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IInitBusiness
{
    public async Task InitServicePrices()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if(!await context.ServicePrices.AnyAsync(a => a.ServiceKind == (byte)TheologyKind.Basic))
        {
            await context.ServicePrices.AddAsync(new ServicePrice
            {
                Name = TheologyKind.Basic.GetDescription(),
                Description = TheologyKind.Basic.GetDescription(),
                ServiceKind = (byte)TheologyKind.Basic,
                Fates = 0
            });
        }

        if (!await context.ServicePrices.AnyAsync(a => a.ServiceKind == (byte)TheologyKind.TuTruBatTu))
        {
            await context.ServicePrices.AddAsync(new ServicePrice
            {
                Name = TheologyKind.TuTruBatTu.GetDescription(),
                Description = TheologyKind.TuTruBatTu.GetDescription(),
                ServiceKind = (byte)TheologyKind.TuTruBatTu,
                Fates = 109,
                FatesDiscount = 10,
                FatesDiscountRate = 0.10m,
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task InitTopUpPackages()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!await context.TopUpPackages.AnyAsync(a => a.Kind == (byte)TopUpPackageKind.Package1))
        {
            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package1.GetDescription(),
                Description = TopUpPackageKind.Package1.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package1,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 29000,
                AmountDiscount = 2000,
                AmountDiscountRate = 0.03m,
                Fates = 190,
                FateBonus = 5,
                FateBonusRate = 0.04m,
                CreatedTs = DateTime.UtcNow,
            });

            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package2.GetDescription(),
                Description = TopUpPackageKind.Package2.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package2,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 49000,
                AmountDiscount = 4500,
                AmountDiscountRate = 0.065m,
                Fates = 290, // Số Fates cũng cao hơn
                FateBonus = 8,
                FateBonusRate = 0.06m,
                CreatedTs = DateTime.UtcNow,
            });

            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package3.GetDescription(),
                Description = TopUpPackageKind.Package3.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package3,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 99000,
                AmountDiscount = 7900,
                AmountDiscountRate = 0.085m,
                Fates = 580, // Số Fates tăng mạnh
                FateBonus = 20,
                FateBonusRate = 0.095m,
                CreatedTs = DateTime.UtcNow,
            });

            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package4.GetDescription(),
                Description = TopUpPackageKind.Package4.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package4,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 199000,
                AmountDiscount = 14500,
                AmountDiscountRate = 0.105m,
                Fates = 1160, // Số điểm Fates tăng tiếp
                FateBonus = 45,
                FateBonusRate = 0.115m,
                CreatedTs = DateTime.UtcNow,
            });

            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package5.GetDescription(),
                Description = TopUpPackageKind.Package5.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package5,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 340000, // Mức giá lớn
                AmountDiscount = 32000,
                AmountDiscountRate = 0.125m,
                Fates = 1988, // Số Fates cao
                FateBonus = 80,
                FateBonusRate = 0.135m,
                CreatedTs = DateTime.UtcNow,
            });

            await context.TopUpPackages.AddAsync(new TopUpPackage
            {
                Name = TopUpPackageKind.Package6.GetDescription(),
                Description = TopUpPackageKind.Package6.GetDescription(),
                Kind = (byte)TopUpPackageKind.Package6,
                Status = (byte)TopupPackageStatus.Actived,
                Amount = 580000, // Gói cao cấp nhất
                AmountDiscount = 55000,
                AmountDiscountRate = 0.145m,
                Fates = 3391, // Số điểm Fates cao nhất
                FateBonus = 120,
                FateBonusRate = 0.155m,
                CreatedTs = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }
}