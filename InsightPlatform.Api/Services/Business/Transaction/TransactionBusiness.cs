using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class TransactionBusiness(ILogger<TransactionBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IPayPalService payPalService
    , IVietQRService vietQRService
    , ICurrencyService currencyService
    , IOptions<PaymentOptions> paymentSettings) : BaseHttpBusiness<TransactionBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), ITransactionBusiness
{
    private readonly PaymentOptions _paymentSettings = paymentSettings.Value;
    private readonly IVietQRService _vietQRService = vietQRService;
    private readonly IPayPalService _payPalService = payPalService;
    private readonly ICurrencyService _currencyService = currencyService;

    public async Task<BaseResponse<dynamic>> GetTopupsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var topups = (await context.TopUpPackages.Where(f => f.Status == (short)TopupPackageStatus.Actived).ToListAsync())
                    .Select(s => new TopupPackageDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Kind = (TopUpPackageKind)s.Kind,
                        Fates = s.Fates,
                        FateBonus = s.FateBonus,
                        FateBonusRate = (s.FateBonusRate ?? 0) * 100,
                        FinalFates = s.GetFinalFates(),
                        Amount = s.Amount,
                        AmountDiscount = s.AmountDiscount,
                        AmountDiscountRate = (s.AmountDiscountRate ?? 0) * 100,
                        FinalAmount = s.GetAmountAfterDiscount(),
                        VATaxIncluded = !_paymentSettings.VATaxIncluded,
                        VATaxRate = _paymentSettings.VATaxRate * 100,
                        CreatedTs = s.CreatedTs,
                    })
                    .OrderBy(o => o.Fates)
                    .ToList();

        return new(topups);
    }

    public async Task<BaseResponse<dynamic>> GetTransactionsAsync(int pageNumber, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        var query = context.Transactions.AsNoTracking()
                                        .Where(f => f.UserId == userId)
                                        .OrderByDescending(o => o.CreatedTs);

        var result = new PaginatedBase<dynamic>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = [],
            TotalRecords = await query.LongCountAsync()
        };
        result.TotalPages = result.PageSize.HasValue ? (long)Math.Ceiling((double)result.TotalRecords / result.PageSize.Value) : 1;

        var list = await query.Skip(((pageNumber - 1) * pageSize))
                              .Take(pageSize)
                              .ToListAsync();

        foreach (var item in list)
        {
            var meta = item.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(item.MetaData) : null;
            string currency = "VND";

            var provider = (TransactionProvider)item.Provider;
            decimal exchangeRate = item.ExchangeRate ?? 1;

            var total = CalculateAmountByGateProvider(provider, item.Total, ref exchangeRate, ref currency);

            var subTotal = CalculateAmountByGateProvider(provider, item.SubTotal, ref exchangeRate, ref currency);

            var feeTotal = CalculateAmountByGateProvider(provider, item.FeeTotal, ref exchangeRate, ref currency);

            var vatTotal = CalculateAmountByGateProvider(provider, item.VATaxTotal, ref exchangeRate, ref currency);

            var finalTotal = CalculateAmountByGateProvider(provider, item.FinalTotal, ref exchangeRate, ref currency);

            var realPaid = meta?.TransactionHictories.Sum(s => s.Amount) ?? 0;
            
            result.Items.Add(new
            {
                Id = item.Id,
                Status = (TransactionStatus)item.Status,
                item.CreatedTs,
                Provider = provider,
                Currency = currency,
                ExchangeRate = exchangeRate,
                Total = total,
                SubTotal = subTotal,
                DiscountTotal = Math.Max(total - subTotal, 0),
                FinalTotal = finalTotal,
                Paid = realPaid,
                BuyerPaysFee = item.BuyerPaysFee,
                FeeRate = item.FeeRate * 100,
                FeeTotal = feeTotal,
                VATaxIncluded = item.VATaxIncluded,
                VATaxRate = item.VATaxRate * 100,
                VATaxTotal = vatTotal,
                Note = item.Note,
                PackageName = meta?.TopUpPackageSnap?.Name,
                Fates = meta?.TopUpPackageSnap?.Fates ?? 0,
                FinalFates = meta?.TopUpPackageSnap?.FinalFates ?? 0,
                FateBonus = meta?.TopUpPackageSnap?.FateBonus ?? 0,
                FateBonusRate = (meta?.TopUpPackageSnap?.FateBonusRate ?? 0) * 100,
            });
        }

        return new(result);
    }
    
    public async Task<BaseResponse<dynamic>> GetServicesAsync(int pageNumber, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        var query = context.TheologyRecords.AsNoTracking()
                                           .Include(i => i.ServicePrice)
                                           .Include(i => i.FatePointTransactions)
                                           .Where(f => f.UserId == userId)
                                           .OrderByDescending(o => o.CreatedTs)
                                           .ThenBy(t => t.Kind);

        var result = new PaginatedBase<dynamic>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = [],
            TotalRecords = await query.LongCountAsync()
        };
        result.TotalPages = result.PageSize.HasValue ? (long)Math.Ceiling((double)result.TotalRecords / result.PageSize.Value) : 1;

        var list = await query.Skip(((pageNumber - 1) * pageSize))
                              .Take(pageSize)
                              .ToListAsync();

        foreach (var item in list)
        {
            dynamic servicePrice = null;

            if (item.ServicePriceSnap.IsPresent())
            {
                var snap = JsonSerializer.Deserialize<ServicePriceSnap>(item.ServicePriceSnap);

                servicePrice = new
                {
                    Fates = snap.Fates,
                    FatesDiscount = snap.FatesDiscount,
                    FatesDiscountRate = (snap.FatesDiscountRate ?? 0) * 100,
                    FinalFates = snap.FinalFates,
                };
            }
            else
            {
                var svPrice = item.ServicePrice;

                servicePrice = new
                {
                    Fates = svPrice.Fates,
                    FatesDiscount = svPrice.FatesDiscount,
                    FatesDiscountRate = (svPrice.FatesDiscountRate ?? 0) * 100,
                    FinalFates = svPrice.GetFinalFates(),
                };
            }

            result.Items.Add(new
            {
                Id = item.Id,
                Status = (TheologyStatus)item.Status,
                IsPaid = item.FatePointTransactions.Count != 0,
                Kind = (TheologyKind)item.Kind,
                item.CreatedTs,
                ServicePrice = servicePrice,
            });
        }

        return new(result);
    }

    public async Task<BaseResponse<dynamic>> BuyTopupAsync(BuyTopupRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var userId = Current.UserId;

        try
        {
            Transaction trans = null;

            if (request.Id != null)
            {
                trans = await context.Transactions.FirstOrDefaultAsync(f => f.Id == request.Id);

                if (trans == null
                    || trans.Status == (short)TransactionStatus.Cancelled
                    || trans.Status == (short)TransactionStatus.Paid)
                {
                    throw new BusinessException("NoPermission", $"Transaction not found or invalid status, '${(TransactionStatus)trans.Status}'");
                }

                switch ((TransactionProvider)trans.Provider)
                {
                    case TransactionProvider.VietQR:
                        {
                            var providerTransaction = trans.ProviderTransaction.IsPresent()
                                                    ? JsonSerializer.Deserialize<VietQrPaymentMetaData>(trans.ProviderTransaction)
                                                    : null;

                            return new(new
                            {
                                IpnUrl = providerTransaction?.Response?.QrLink
                            });
                        }
                    case TransactionProvider.Paypal:
                        {
                            var providerTransaction = trans.ProviderTransaction.IsPresent()
                                                     ? JsonSerializer.Deserialize<PaypalPaymentMetaData>(trans.ProviderTransaction)
                                                     : null;

                            return new(new
                            {
                                IpnUrl = providerTransaction?.IpnUrl
                            });
                        }
                    default:
                        throw new BusinessException("InvalidTransactionProvider", $"Transaction provider is invalid, '${trans.Provider}'");
                }
            }

            if (request.Provider == null
                || request.TopupPackageId == null)
                throw new BusinessException("InvalidInput", $"Invalid input");

            var settings = _paymentSettings.Gates[request.Provider.Value];
            if (settings == null || !settings.IsActive)
            {
                throw new BusinessException("PaymentGateNotAvailable", "Payment gate is not available or not active");
            }

            var package = await context.TopUpPackages.FirstOrDefaultAsync(f => f.Id == request.TopupPackageId
                                                                            && f.Status == (short)TopupPackageStatus.Actived);

            if (package == null)
            {
                throw new BusinessException("TopupPackageNotFound", "Topup package not found or unavailable");
            }

            var provider = request.Provider.Value;

            var amountAfterDiscount = package.GetAmountAfterDiscount();

            trans = new Transaction
            {
                UserId = userId.Value,
                TopUpPackageId = package.Id,
                Status = (byte)TransactionStatus.New,
                Provider = (byte)provider,
                Total = package.Amount,
                SubTotal = amountAfterDiscount,
                FinalTotal = amountAfterDiscount,
                VATaxIncluded = _paymentSettings.VATaxIncluded,
                VATaxRate = _paymentSettings.VATaxRate,
                MetaData = JsonSerializer.Serialize(new TransactionMetaData
                {
                    TopUpPackageSnap = new(package),
                    TransactionHictories = [],
                    Events = []
                })
            };

            await context.Transactions.AddAsync(trans);
            await context.SaveChangesAsync();

            request.CallbackUrl = request.CallbackUrl?.WithQuery(new Dictionary<string, string>
            {
                { "transId", trans.Id.ToString() }
            });

            string ipnUrl = null;            

            switch (provider)
            {
                case TransactionProvider.VietQR:
                    {
                        var vietQrToken = await _vietQRService.GenerateTokenAsync();
                        var content = $"Thanh toán '{((TopUpPackageKind)package.Kind).GetDescription()}'";

                        var total = trans.Total;
                        var subTotal = trans.SubTotal;

                        PaymentUtils.CalculateFeeAndTaxV1(
                          total
                        , subTotal
                        , settings.FeeRate
                        , settings.BuyerPaysFee
                        , trans.VATaxIncluded
                        , trans.VATaxRate
                        , trans.Id.ToString()
                        , content
                        , out decimal feeAmount, out decimal discount, out decimal vatAmount, out decimal finalAmount, out string effectiveDescription);

                        total = PaymentUtils.RoundAmountByGateProvider(provider, total);
                        subTotal = PaymentUtils.RoundAmountByGateProvider(provider, subTotal);
                        vatAmount = PaymentUtils.RoundAmountByGateProvider(provider, vatAmount);
                        feeAmount = PaymentUtils.RoundAmountByGateProvider(provider, feeAmount);
                        discount = PaymentUtils.RoundAmountByGateProvider(provider, discount);
                        finalAmount = total + vatAmount + feeAmount - discount;

                        var vietQrRequest = new VietQrPaymentRequest
                        {
                            Amount = (long)finalAmount,
                            Content = content.RemoveVietnameseDiacritics(),
                            BankAccount = settings.PlatformConnection.BankAccount,
                            BankCode = settings.PlatformConnection.BankCode,
                            UserBankName = settings.PlatformConnection.UserBankName,
                            TransType = "C",
                            OrderId = trans.Id.ToString(),
                            ServiceCode = package.Id.ToString(),
                            QrType = 0,
                            UrlLink = request.CallbackUrl,
                            Note = effectiveDescription
                        };

                        var vietQr = await _vietQRService.NewPaymentAsync(vietQrToken, vietQrRequest);
                        ipnUrl = vietQr.QrLink;

                        trans.BuyerPaysFee = settings.BuyerPaysFee;
                        trans.FeeRate = settings.FeeRate;
                        trans.FeeTotal = feeAmount;
                        trans.VATaxTotal = vatAmount;
                        trans.FinalTotal = finalAmount;

                        trans.Note = effectiveDescription;
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
                        var totalUSD = PaymentUtils.RoundAmountByGateProvider(provider, _currencyService.ConvertFromVND(trans.Total, "USD", out decimal rate));
                        var subTotalUSD = PaymentUtils.RoundAmountByGateProvider(provider, _currencyService.ConvertFromVND(trans.SubTotal, "USD", out rate));

                        var content = $"Thanh toán '{((TopUpPackageKind)package.Kind).GetDescription()}'";

                        (ipnUrl, var feeTotal, var vatTotal, var finalTotal, var note) = await _payPalService.CreateOrderAsync(
                              totalUSD,
                              subTotalUSD
                            , request.CallbackUrl
                            , $"{request.CallbackUrl}&cancel=1"
                            , trans.Id.ToString()
                            , settings.PlatformConnection.BrandName
                            , "vi-VN"
                            , content
                            , trans.Id.ToString()
                            , null
                            , settings.FeeRate
                            , settings.BuyerPaysFee
                            , trans.VATaxIncluded
                            , trans.VATaxRate
                        );

                        trans.BuyerPaysFee = settings.BuyerPaysFee;
                        trans.FeeRate = settings.FeeRate;
                        trans.FeeTotal = _currencyService.ConvertToVND(feeTotal, "USD", out rate);
                        trans.VATaxTotal = _currencyService.ConvertToVND(vatTotal, "USD", out rate);
                        trans.FinalTotal = _currencyService.ConvertToVND(finalTotal, "USD", out rate);
                        trans.ExchangeRate = rate;

                        trans.Note = note;
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
        catch (Exception)
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

    public async Task<BaseResponse<dynamic>> GetMemoCheckoutAsync(Guid topupPackageId, TransactionProvider provider)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userId = Current.UserId;

        try
        {
            var package = await context.TopUpPackages.FirstOrDefaultAsync(f => f.Id == topupPackageId
                                                                            && f.Status == (short)TopupPackageStatus.Actived);

            var settings = _paymentSettings.Gates[provider];

            if (package == null)
            {
                throw new BusinessException("TopupPackageNotFound", "Topup package not found or unavailable");
            }

            var buyerPaysFee = settings.BuyerPaysFee;
            var feeRate = settings.FeeRate;

            var includeVAT = _paymentSettings.VATaxIncluded;
            var VATaxRate = _paymentSettings.VATaxRate;

            decimal rate = 1;
            string currency = "VND";

            var total = package.Amount;
            var subtotal = package.GetAmountAfterDiscount();

            total = CalculateAmountByGateProvider(provider, total, ref rate, ref currency);
            subtotal = CalculateAmountByGateProvider(provider, subtotal, ref rate, ref currency);

            PaymentUtils.CalculateFeeAndTaxV1(
                  total
                , subtotal
                , feeRate
                , buyerPaysFee
                , includeVAT
                , VATaxRate
                , string.Empty
                , string.Empty
                , out decimal feeAmount, out decimal discount, out decimal vatAmount, out decimal finalAmount, out string effectiveDescription);

            total = PaymentUtils.RoundAmountByGateProvider(provider, total);
            subtotal = PaymentUtils.RoundAmountByGateProvider(provider, subtotal);
            discount = PaymentUtils.RoundAmountByGateProvider(provider, discount);
            feeAmount = PaymentUtils.RoundAmountByGateProvider(provider, feeAmount);
            vatAmount = PaymentUtils.RoundAmountByGateProvider(provider, vatAmount);
            finalAmount = total + vatAmount + feeAmount - discount;

            return new(new MemoCheckoutDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Kind = (TopUpPackageKind)package.Kind,
                Fates = package.Fates,
                FateBonus = package.FateBonus,
                FateBonusRate = (package.FateBonusRate ?? 0) * 100,
                FinalFates = package.GetFinalFates(),
                Amount = package.Amount,
                AmountDiscount = package.AmountDiscount,
                AmountDiscountRate = (package.AmountDiscountRate ?? 0) * 100,
                FinalAmount = package.GetAmountAfterDiscount(),
                VATaxIncluded = !_paymentSettings.VATaxIncluded,
                VATaxRate = _paymentSettings.VATaxRate * 100,
                Currency = "VND",
                MemoCheckout = new TransactionCheckoutDto
                {
                    Provider = provider,
                    Currency = currency,
                    ExchangeRate = rate,
                    Total = total,
                    SubTotal = subtotal,
                    DiscountTotal = discount,
                    FinalTotal = finalAmount,
                    BuyerPaysFee = buyerPaysFee,
                    FeeRate = feeRate * 100,
                    FeeTotal = feeAmount,
                    VATaxIncluded = includeVAT,
                    VATaxRate = VATaxRate * 100,
                    VATaxTotal = vatAmount,
                    Note = effectiveDescription,
                    PackageName = package.Name,
                    Fates = package.Fates,
                    FinalFates = package.GetFinalFates(),
                    FateBonus = package.FateBonus ?? 0,
                    FateBonusRate = (package.FateBonusRate ?? 0) * 100,
                }
            });
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<dynamic>> GetDetailAsync(Guid id)
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

            var exchangeRate = trans.ExchangeRate ?? 1;

            var meta = trans.MetaData.IsPresent() ? JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : null;

            string currency = "VND";

            var total = CalculateAmountByGateProvider(provider, trans.Total, ref exchangeRate, ref currency);

            var subTotal = CalculateAmountByGateProvider(provider, trans.SubTotal, ref exchangeRate, ref currency);

            var feeTotal = CalculateAmountByGateProvider(provider, trans.FeeTotal, ref exchangeRate, ref currency);

            var vatTotal = CalculateAmountByGateProvider(provider, trans.VATaxTotal, ref exchangeRate, ref currency);

            var finalTotal = CalculateAmountByGateProvider(provider, trans.FinalTotal, ref exchangeRate, ref currency);

            var realPaid = meta?.TransactionHictories.Sum(s => s.Amount) ?? 0;

            return new(new TransactionCheckoutDto
            {
                Id = id,
                Status = (TransactionStatus)trans.Status,
                Provider = provider,
                Currency = currency,
                ExchangeRate = exchangeRate,
                Total = total,
                SubTotal = subTotal,
                DiscountTotal = Math.Max(total - subTotal, 0),
                FinalTotal = finalTotal,
                Paid = realPaid,
                BuyerPaysFee = trans.BuyerPaysFee,
                FeeRate = trans.FeeRate * 100,
                FeeTotal = feeTotal,
                VATaxIncluded = trans.VATaxIncluded,
                VATaxRate = trans.VATaxRate * 100,
                VATaxTotal = vatTotal,
                Note = trans.Note,
                PackageName = meta?.TopUpPackageSnap?.Name,
                Fates = meta?.TopUpPackageSnap?.Fates ?? 0,
                FinalFates = meta?.TopUpPackageSnap?.FinalFates ?? 0,
                FateBonus = meta?.TopUpPackageSnap?.FateBonus ?? 0,
                FateBonusRate = (meta?.TopUpPackageSnap?.FateBonusRate ?? 0) * 100,
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
            if (data.OrderId.IsMissing())
                throw new BusinessException("InvalidVietQrCallback", "OrderId is missing in the callback data");

            var orderId = Guid.TryParse(data.OrderId, out var id) ? id : (Guid?)null;

            if (orderId == null)
                throw new BusinessException("InvalidVietQrCallback", "OrderId is invalid in the callback data");

            var trans = await context.Transactions.Include(i => i.FatePointTransactions)
                                                  .Include(i => i.TopUpPackage)
                                                  .FirstOrDefaultAsync(x => x.Id == orderId);

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

            if (paidTotal < trans.FinalTotal)
                trans.Status = (byte)TransactionStatus.PartiallyPaid;
            else
            {
                trans.Status = (byte)TransactionStatus.Paid;

                if (!trans.FatePointTransactions.Any())
                {
                    trans.FatePointTransactions.Add(new FatePointTransaction
                    {
                        UserId = trans.UserId,
                        Fates = metaData?.TopUpPackageSnap?.FinalFates ?? trans.TopUpPackage.GetFinalFates(),
                    });
                }
            }

            context.Transactions.Update(trans);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await RecalculateUserFates(trans.UserId);

            return new(trans.Id);
        }
        catch(Exception ex)
        {
            await transaction.RollbackAsync();
            throw new BusinessException("VietQrCallbackFailed", $"payload: {JsonSerializer.Serialize(data)}", ex);
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
            var orderId = Guid.TryParse(data.Resource?.InvoiceId ?? data.Resource?.CustomId ?? order?.ReferenceId ?? order?.CustomId ?? order?.InvoiceId, out var oId) ? oId : (Guid?)null;
            var paypalResourceId = data.Resource?.Id;
            var amount = order?.Amount ?? data.Resource?.Amount;

            Transaction trans = null;

            if (!orderId.HasValue)
            {
                throw new BusinessException("TransactionNotFound", $"Transaction not found, id = ''");
            }

            trans = await context.Transactions.Include(i => i.FatePointTransactions)
                                              .Include(i => i.TopUpPackage)
                                              .FirstOrDefaultAsync(x => x.Id == orderId.Value);

            if (trans == null)
                throw new BusinessException("TransactionNotFound", $"Transaction not found, id = '{orderId.Value}'");

            if (trans.Status == (byte)TransactionStatus.Paid
                || trans.Status == (byte)TransactionStatus.Cancelled)
                throw new BusinessException("NoPermission", $"Transaction status is '{(TransactionStatus)trans.Status}', id = {orderId.Value}");

            var metaData = trans.MetaData.IsPresent() ? 
                           JsonSerializer.Deserialize<TransactionMetaData>(trans.MetaData) : 
                           new TransactionMetaData();

            metaData.Events.Add(data.SerializeTransactionEvent());

            switch (eventType)
            {
                case "CHECKOUT.ORDER.APPROVED":
                    {
                        if (paypalResourceId.IsMissing())
                            throw new BusinessException("InvalidPaypalHookRequest", $"paypalResourceId is empty");

                        await _payPalService.CaptureOrderAsync(paypalResourceId);
                        break;
                    }
                case "PAYMENT.CAPTURE.COMPLETED":
                    {
                        if (amount == null || amount.Value.IsMissing())
                            throw new BusinessException("InvalidPaypalHookRequest", $"amount is empty");

                        var hookAmountUSD = decimal.Parse(amount.Value);
                        var transAmoutUSD = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, _currencyService.ConvertFromVND(trans.FinalTotal, trans.ExchangeRate ?? 1));

                        metaData.TransactionHictories.Add(new TransactionHictory
                        {
                            TransactionId = data.Resource.Id,
                            ReferenceNumber = data.Id,
                            OrderId = orderId.ToString(),
                            Amount = hookAmountUSD,
                            Content = order?.Description ?? data.Summary,
                            TransactionTime = data.CreateTime.ToUnixTimestamp(),
                        });

                        var paidTotalUSD = metaData.TransactionHictories.Sum(s => s.Amount);                        

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
                                    Fates = metaData?.TopUpPackageSnap?.FinalFates ?? trans.TopUpPackage.GetFinalFates()
                                });
                            }
                        }

                        break;
                    }               
                case "CHECKOUT.ORDER.DENIED":
                case "CHECKOUT.ORDER.DECLINED":
                case "PAYMENT.ORDER.CANCELLED":
                case "PAYMENT.CAPTURE.DENIED":
                case "PAYMENT.CAPTURE.DECLINED":
                    {
                        if (!orderId.HasValue)
                            throw new BusinessException("InvalidPaypalHookRequest", $"orderId is empty");

                        await CancelAsync(orderId.Value);
                        break;
                    }
            }

            trans.MetaData = JsonSerializer.Serialize(metaData);
            context.Transactions.Update(trans);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await RecalculateUserFates(trans.UserId);

            return new(trans.Id);
        }
        catch(Exception)
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

    public async Task<BaseResponse<int>> PayTheologyRecordAsync(Guid id)
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

            var userFates = await RecalculateUserFates(userId.Value);

            var serviceFates = service.ServicePriceSnap.IsPresent() ? 
                               JsonSerializer.Deserialize<ServicePriceSnap>(service.ServicePriceSnap).FinalFates : 
                               service.ServicePrice.GetFinalFates();

            if (serviceFates > 0 && userFates < serviceFates)
                throw new BusinessException("FatesNotEnough", "Fates are not enough to proceed");

            service.FatePointTransactions.Add(new FatePointTransaction
            {
                UserId = userId.Value,
                Fates = serviceFates > 0 ? -serviceFates : 0
            });

            context.TheologyRecords.Update(service);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            userFates = await RecalculateUserFates(userId.Value);
            return new(userFates);
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

    private decimal CalculateAmountByGateProvider(TransactionProvider provider, decimal amountVND, ref decimal rate, ref string currency)
    {
        rate = 1;
        currency = "VND";

        switch (provider)
        {
            case TransactionProvider.Paypal:
                currency = "USD";
                return PaymentUtils.RoundAmountByGateProvider(provider, _currencyService.ConvertFromVND(amountVND, currency, out rate));

            case TransactionProvider.VietQR:
                return PaymentUtils.RoundAmountByGateProvider(provider, amountVND);

            default:
                return PaymentUtils.RoundAmountByGateProvider(provider, amountVND);
        }
    }    
}
