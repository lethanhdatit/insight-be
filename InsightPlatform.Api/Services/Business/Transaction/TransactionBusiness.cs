using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class TransactionBusiness(ILogger<TransactionBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IAccountBusiness accountBusiness
    , IOpenAiService openAiService
    , IGeminiAIService geminiAIService
    , IVietQRService vietQRService
    , IOptions<AppSettings> appOptions
    , IOptions<PaymentGateOptions> paymentSettings
    , PainPublisher publisher) : BaseHttpBusiness<TransactionBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), ITransactionBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly PaymentGateOptions _paymentSettings = paymentSettings.Value;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IOpenAiService _openAiService = openAiService;
    private readonly IGeminiAIService _geminiAIService = geminiAIService;
    private readonly IVietQRService _vietQRService = vietQRService;

    public async Task<BaseResponse<dynamic>> GetTopupsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var topups = (await context.TopUpPackages.ToListAsync())
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        Kind = (TopUpPackageKind)s.Kind,
                        s.Fates,
                        s.FateBonus,
                        s.FateBonusRate,
                        FinalFates = s.GetFinalFates(),
                        s.Amount,
                        s.AmountDiscount,
                        s.AmountDiscountRate,
                        FinalAmount = s.GetFinalAmount(),
                        s.CreatedTs,
                        Rate = s.GetFinalAmount() / s.GetFinalFates()
                    })
                    .OrderBy(o => o.Fates)
                    .ToList();

        return new(topups);
    }

    public async Task<BaseResponse<dynamic>> BuyTopupAsync(BuyTopupRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var userId = Current.UserId;

        try
        {
            var package = await context.TopUpPackages.FirstOrDefaultAsync(f => f.Id == request.TopupPackageId
                                                                            && f.Status == (short)TopupPackageStatus.Actived);

            if (package == null)
            {
                throw new BusinessException("TopupPackageNotFound", "Topup package not found or unavailable");
            }

            var trans = new Transaction
            {
                UserId = userId.Value,
                TopUpPackageId = package.Id,
                Status = (byte)TransactionStatus.New,
                Provider = (byte)request.Provider,
                SubTotal = package.Amount,
                Total = package.GetFinalAmount(),
                CreatedTs = DateTime.UtcNow
            };

            await context.Transactions.AddAsync(trans);
            await context.SaveChangesAsync();

            string ipnUrl = null;

            switch (request.Provider)
            {
                case TransactionProvider.VietQR:
                    var vietQrToken = await _vietQRService.GenerateTokenAsync();

                    var vietQrRequest = new VietQrPaymentRequest
                    {
                        Amount = (long)trans.Total,
                        Content = $"Mua {((TopUpPackageKind)package.Kind).GetDescription()}".RemoveVietnameseDiacritics(),
                        BankAccount = _paymentSettings.VietQR.PlatformConnection.BankAccount,
                        BankCode = _paymentSettings.VietQR.PlatformConnection.BankCode,
                        UserBankName = _paymentSettings.VietQR.PlatformConnection.UserBankName,
                        TransType = "C",
                        OrderId = trans.Id.ToString(),
                        ServiceCode = package.Id.ToString(),
                        QrType = 0,
                        UrlLink = request.CallbackUrl
                    };

                    var vietQr = await _vietQRService.NewPaymentAsync(vietQrToken, vietQrRequest);
                    ipnUrl = vietQr.QrLink;

                    trans.Status = (byte)TransactionStatus.Processing;
                    trans.ProviderTransaction = JsonSerializer.Serialize(new VietQrPaymentMetaData
                    {
                        Request = vietQrRequest,
                        Response = vietQr
                    });

                    context.Transactions.Update(trans);
                    await context.SaveChangesAsync();

                    break;
                case TransactionProvider.Paypal:
                    throw new NotImplementedException();
                default:
                    break;
            }

            await transaction.CommitAsync();

            return new(new
            {
                IpnUrl = ipnUrl
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

    public async Task<BaseResponse<dynamic>> CheckStatusAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        try
        {
            var trans = await context.Transactions.FirstOrDefaultAsync(f => f.Id == id
                                                                         && f.UserId == userId);

            if (trans == null)
            {
                throw new BusinessException("TransactionNotFound", "Transaction not found or unavailable for you");
            }


            return new(new
            {
                Id = id,
                Status = (TransactionStatus)trans.Status,
                ProviderMeta = trans.ProviderTransaction.IsPresent() ? JsonSerializer.Deserialize<VietQrPaymentMetaData>(trans.ProviderTransaction) : null,
                Meta = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : null,
            });
        }
        catch
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<dynamic>> CancelAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        try
        {
            var trans = await context.Transactions.FirstOrDefaultAsync(f => f.Id == id
                                                                         && f.UserId == userId);

            if (trans == null)
            {
                throw new BusinessException("TransactionNotFound", "Transaction not found or unavailable for you");
            }

            if (trans.Status != (byte)TransactionStatus.New
                && trans.Status != (byte)TransactionStatus.Processing)
            {
                throw new BusinessException("NoPermission", "Transaction status is unavailable to canncel");
            }

            trans.Status = (byte)TransactionStatus.Cancelled;

            context.Transactions.Update(trans);
            await context.SaveChangesAsync();

            return new(new
            {
                Id = id,
                Status = (TransactionStatus)trans.Status
            });
        }
        catch
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<Guid>> VietQrCallbackAsync(TransactionCallback request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var trans = await context.Transactions.Include(i => i.FatePointTransactions)
                                                  .Include(i => i.TopUpPackage)
                                                  .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.OrderId));

            if (trans == null)
                throw new BusinessException("TransactionNotFound", $"Transaction not found, id = {request.OrderId}");

            if (trans.Status == (byte)TransactionStatus.Paid
                || trans.Status == (byte)TransactionStatus.Cancelled)
                throw new BusinessException("NoPermission", $"Transaction status is '{(TransactionStatus)trans.Status}', id = {request.OrderId}");

            var metaData = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : new TransactionMetaData();

            metaData.TransactionHictories.Add(new TransactionHictory
            {
                TransactionId = request.TransactionId,
                ReferenceNumber = request.ReferenceNumber,
                OrderId = request.OrderId,
                Amount = request.Amount,
                BankAccount = request.BankAccount,
                Content = request.Content,
                TransactionTime = request.TransactionTime,
            });

            var paidTotal = metaData.TransactionHictories.Sum(s => s.Amount);

            trans.MetaData = JsonSerializer.Serialize(metaData);

            if (paidTotal < trans.Total)
                trans.Status = (byte)TransactionStatus.PartiallyPaid;
            else
            {
                trans.Status = (byte)TransactionStatus.Paid;

                if (!trans.FatePointTransactions.Any())
                {
                    trans.FatePointTransactions.Add(new FatePointTransaction
                    {
                        UserId = trans.UserId,
                        Fates = trans.TopUpPackage.GetFinalFates(),
                        CreatedTs = DateTime.UtcNow,
                    });
                }
            }

            context.Transactions.Update(trans);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            FuncTaskHelper.FireAndForget(() => RecalculateUserFates(trans.UserId));

            return new(trans.Id);
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

    public async Task<BaseResponse<int>> GetUserFates()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        try
        {
            var user = await context.Users.FirstOrDefaultAsync(f => f.Id == userId);

            if (user == null)
            {
                throw new BusinessException("UserNotFound", $"User not found, id = {userId}");
            }

            return new(user.Fates);
        }
        catch
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<int> RecalculateUserFates(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var user = await context.Users.Include(i => i.FatePointTransactions)
                                          .FirstOrDefaultAsync(f => f.Id == userId);

            if (user == null)
            {
                throw new BusinessException("UserNotFound", $"User not found, id = {userId}");
            }

            user.Fates = user.FatePointTransactions.Sum(f => f.Fates);
            context.Users.Update(user);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return user.Fates;
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
