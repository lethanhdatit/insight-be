using Microsoft.Extensions.Options;
using System;

public interface ICurrencyService
{
    decimal ConvertFromVND(decimal vndAmount, string toCurrency);

    decimal ConvertToVND(decimal amount, string fromCurrency);
}

public class CurrencyService(IOptions<PaymentGateOptions> settings) : ICurrencyService
{
    private readonly CurrencyExchangeOptions _options = settings.Value.CurrencyExchangeRates;

    // Convert from VND to target currency
    public decimal ConvertFromVND(decimal vndAmount, string toCurrency)
    {
        if (_options.BaseCurrency != "VND")
            throw new InvalidOperationException("BaseCurrency must be VND");

        if (_options.Rates.TryGetValue(toCurrency.ToUpper(), out var rate))
        {
            return vndAmount / rate;
        }

        throw new ArgumentException($"Unsupported currency: {toCurrency}");
    }

    public decimal ConvertToVND(decimal amount, string fromCurrency)
    {
        if (_options.BaseCurrency != "VND")
            throw new InvalidOperationException("BaseCurrency must be VND");

        if (_options.Rates.TryGetValue(fromCurrency.ToUpper(), out var rate))
        {
            return amount * rate;
        }

        throw new ArgumentException($"Unsupported currency: {fromCurrency}");
    }
}
