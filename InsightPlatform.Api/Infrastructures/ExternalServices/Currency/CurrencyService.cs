using Microsoft.Extensions.Options;
using System;

public interface ICurrencyService
{
    decimal ConvertFromVND(decimal vndAmount, string toCurrency, out decimal rate);

    decimal ConvertFromVND(decimal vndAmount, decimal rate);

    decimal ConvertToVND(decimal amount, string fromCurrency, out decimal rate);

    decimal ConvertToVND(decimal amount, decimal rate);
}

public class CurrencyService(IOptions<PaymentGateOptions> settings) : ICurrencyService
{
    private readonly CurrencyExchangeOptions _options = settings.Value.CurrencyExchangeRates;

    public decimal ConvertFromVND(decimal vndAmount, string toCurrency, out decimal rate)
    {
        if (_options.BaseCurrency != "VND")
            throw new InvalidOperationException("BaseCurrency must be VND");

        if (_options.Rates.TryGetValue(toCurrency.ToUpper(), out rate))
        {
            return ConvertFromVND(vndAmount, rate);
        }

        throw new ArgumentException($"Unsupported currency: {toCurrency}");
    }

    public decimal ConvertFromVND(decimal vndAmount, decimal rate)
    {
        return vndAmount / rate;
    }

    public decimal ConvertToVND(decimal amount, string fromCurrency, out decimal rate)
    {
        if (_options.BaseCurrency != "VND")
            throw new InvalidOperationException("BaseCurrency must be VND");

        if (_options.Rates.TryGetValue(fromCurrency.ToUpper(), out rate))
        {
            return ConvertToVND(amount, rate);
        }

        throw new ArgumentException($"Unsupported currency: {fromCurrency}");
    }

    public decimal ConvertToVND(decimal amount, decimal rate)
    {
        return amount * rate;
    }
}
