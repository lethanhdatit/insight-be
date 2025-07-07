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
    , IPayPalService payPalService
    , IVietQRService vietQRService
    , ICurrencyService currencyService
    , IOptions<AppSettings> appOptions
    , IOptions<PaymentGateOptions> paymentSettings
    , PainPublisher publisher) : BaseHttpBusiness<TransactionBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), ITransactionBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly PaymentGateOptions _paymentSettings = paymentSettings.Value;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IVietQRService _vietQRService = vietQRService;
    private readonly IPayPalService _payPalService = payPalService;
    private readonly ICurrencyService _currencyService = currencyService;

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

            request.CallbackUrl = request.CallbackUrl?.WithQuery(new
            {
                transId = trans.Id
            });

            string ipnUrl = null;

            switch (request.Provider)
            {
                case TransactionProvider.VietQR:
                    {
                        var vietQrToken = await _vietQRService.GenerateTokenAsync();
                        var content = $"Mua gói '{((TopUpPackageKind)package.Kind).GetDescription()}'";

                        var vietQrRequest = new VietQrPaymentRequest
                        {
                            Amount = (long)trans.Total,
                            Content = content.RemoveVietnameseDiacritics(),
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

                        trans.Note = content;
                        trans.Status = (byte)TransactionStatus.Processing;
                        trans.ProviderTransaction = JsonSerializer.Serialize(new VietQrPaymentMetaData
                        {
                            Request = vietQrRequest,
                            Response = vietQr
                        });

                        context.Transactions.Update(trans);
                        await context.SaveChangesAsync();

                        break;
                    }                   
                case TransactionProvider.Paypal:
                    {
                        var amoutUSD = Math.Round(_currencyService.ConvertFromVND(trans.Total, "USD"), 2);
                        var content = $"Mua gói '{((TopUpPackageKind)package.Kind).GetDescription()}'";

                        ipnUrl = await _payPalService.CreateOrderAsync(
                              amoutUSD
                            , request.CallbackUrl
                            , $"{request.CallbackUrl}&cancel=1"
                            , trans.Id.ToString()
                            , content
                            , trans.Id.ToString());

                        trans.Note = content;
                        trans.Status = (byte)TransactionStatus.Processing;
                        trans.ProviderTransaction = JsonSerializer.Serialize(new PaypalPaymentMetaData
                        {
                            IpnUrl = ipnUrl,
                            CallbackUrl = request.CallbackUrl
                        });

                        context.Transactions.Update(trans);
                        await context.SaveChangesAsync();

                        break;
                    }                    
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
            var trans = await context.Transactions.Include(i => i.TopUpPackage)
                                                  .FirstOrDefaultAsync(f => f.Id == id
                                                                         && f.UserId == userId);

            if (trans == null)
            {
                throw new BusinessException("TransactionNotFound", "Transaction not found or unavailable for you");
            }

            dynamic providerMeta = null;
            var provider = (TransactionProvider)trans.Provider;

            if (trans.ProviderTransaction.IsPresent())
            {
                switch (provider)
                {
                    case TransactionProvider.VietQR:
                        providerMeta = JsonSerializer.Deserialize<VietQrPaymentMetaData>(trans.ProviderTransaction);
                        break;
                    case TransactionProvider.Paypal:
                        providerMeta = JsonSerializer.Deserialize<PaypalPaymentMetaData>(trans.ProviderTransaction);                        
                        break;
                    default:
                        break;
                }
            }

            var meta = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : null;

            return new(new
            {
                Id = id,
                Status = (TransactionStatus)trans.Status,
                Total = provider == TransactionProvider.Paypal ? Math.Round(_currencyService.ConvertFromVND(trans.Total, "USD"), 2) : trans.Total,
                SubTotal = provider == TransactionProvider.Paypal ? Math.Round(_currencyService.ConvertFromVND(trans.SubTotal, "USD"), 2) : trans.SubTotal,
                Paid = meta?.TransactionHictories.Sum(s => s.Amount) ?? 0,
                Provider = provider,
                Currency = provider == TransactionProvider.Paypal ? "USD" : "VND",
                Content = trans.Note,
                Fates = trans.TopUpPackage.GetFinalFates(),
                ProviderMeta = providerMeta,
                Meta = meta,
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

    public async Task<BaseResponse<Guid>> HandleVietQrCallbackAsync(TransactionCallback data)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var trans = await context.Transactions.Include(i => i.FatePointTransactions)
                                                  .Include(i => i.TopUpPackage)
                                                  .FirstOrDefaultAsync(x => x.Id == Guid.Parse(data.OrderId));

            if (trans == null)
                throw new BusinessException("TransactionNotFound", $"Transaction not found, id = {data.OrderId}");

            if (trans.Status == (byte)TransactionStatus.Paid
                || trans.Status == (byte)TransactionStatus.Cancelled)
                throw new BusinessException("NoPermission", $"Transaction status is '{(TransactionStatus)trans.Status}', id = {data.OrderId}");

            var metaData = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : new TransactionMetaData();

            metaData.TransactionHictories.Add(new TransactionHictory
            {
                TransactionId = data.TransactionId,
                ReferenceNumber = data.ReferenceNumber,
                OrderId = data.OrderId,
                Amount = data.Amount,
                BankAccount = data.BankAccount,
                Content = data.Content,
                TransactionTime = data.TransactionTime,
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

    public async Task<BaseResponse<Guid>> HandlePaypalCallbackAsync(PayPalWebhookEvent data)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var eventType = data.EventType;
            var order = data.Resource?.PurchaseUnits?.FirstOrDefault();
            var orderId = Guid.TryParse(order?.ReferenceId ?? order?.CustomId ?? order?.InvoiceId, out var oId) ? oId : (Guid?)null;
            var paypalResourceId = data.Resource?.Id;

            Transaction trans = null;

            if (orderId.HasValue)
            {
                trans = await context.Transactions.Include(i => i.FatePointTransactions)
                                                  .Include(i => i.TopUpPackage)
                                                  .FirstOrDefaultAsync(x => x.Id == orderId.Value);

                if (trans == null)
                    throw new BusinessException("TransactionNotFound", $"Transaction not found, id = {orderId.Value}");

                if (trans.Status == (byte)TransactionStatus.Paid
                    || trans.Status == (byte)TransactionStatus.Cancelled)
                    throw new BusinessException("NoPermission", $"Transaction status is '{(TransactionStatus)trans.Status}', id = {orderId.Value}");

            }

            switch (eventType)
            {
                case "CHECKOUT.ORDER.APPROVED":
                    {
                        var amount = order.Amount;

                        if (amount == null || amount.Value.IsMissing())
                            throw new BusinessException("InvalidPaypalHookRequest", $"amount is empty");

                        if (paypalResourceId.IsMissing())
                            throw new BusinessException("InvalidPaypalHookRequest", $"paypalResourceId is empty");

                        await _payPalService.CaptureOrderAsync(paypalResourceId);

                        var hookAmountUSD = decimal.Parse(amount.Value);
                        var transAmoutUSD = Math.Round(_currencyService.ConvertFromVND(trans.Total, "USD"), 2);

                        var metaData = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : new TransactionMetaData();

                        metaData.TransactionHictories.Add(new TransactionHictory
                        {
                            TransactionId = data.Resource.Id,
                            ReferenceNumber = data.Id,
                            OrderId = orderId?.ToString(),
                            Amount = hookAmountUSD,
                            Content = order.Description,
                            TransactionTime = data.CreateTime.ToUnixTimestamp(),
                        });

                        var paidTotalUSD = metaData.TransactionHictories.Sum(s => s.Amount);

                        trans.MetaData = JsonSerializer.Serialize(metaData);

                        if (paidTotalUSD < transAmoutUSD)
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
                        break;
                    }
                case "CHECKOUT.ORDER.DENIED":
                case "CHECKOUT.ORDER.DECLINED":
                    {
                        if (!orderId.HasValue)
                            throw new BusinessException("InvalidPaypalHookRequest", $"orderId is empty");

                        await CancelAsync(orderId.Value);
                        break;
                    }
            }

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
            var user = await context.Users.FirstOrDefaultAsync(f => f.Id == userId)
                    ?? throw new BusinessException("UserNotFound", $"User not found, id = {userId}");

            var fatesSum = await context.FatePointTransactions
                                .Where(t => t.UserId == user.Id)
                                .SumAsync(f => f.Fates);

            user.Fates = fatesSum;
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
