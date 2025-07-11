using System.Collections.Generic;

public class PaymentOptions
{
    public const string Path = "Payment";

    public decimal VATaxRate { get; set; }
    public bool VATaxIncluded { get; set; }

    public Dictionary<TransactionProvider, PaymentGateOptions> Gates { get; set; }
    public CurrencyExchangeOptions CurrencyExchangeRates { get; set; } = new();
}

public class PaymentGateOptions
{
    public decimal FeeRate { get; set; }
    public bool BuyerPaysFee { get; set; }

    public PlatformConnectionOptions PlatformConnection { get; set; } = new();
    public GateConnectionOptions GateConnection { get; set; } = new();    
}

public class PlatformConnectionOptions : ConnectionBaseOptions
{    
    public string BankAccount { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string UserBankName { get; set; } = string.Empty;
}

public class GateConnectionOptions : ConnectionBaseOptions
{
    public string NewOrderPath { get; set; } = string.Empty;   
}

public class ConnectionBaseOptions
{
    public string BrandName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenPath { get; set; } = string.Empty;
    public string WebhookId { get; set; }
    public string WebhookPath { get; set; } = string.Empty;
    public bool UseSandbox { get; set; }
}

public class CurrencyExchangeOptions
{
    public string BaseCurrency { get; set; }
    public Dictionary<string, decimal> Rates { get; set; }
}
