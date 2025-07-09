using System.Collections.Generic;

public class PaymentGateOptions
{
    public const string Path = "PaymentGate";

    public decimal VATaxRate { get; set; }
    public bool VATaxIncluded { get; set; }
    public VietQROptions VietQR { get; set; } = new();
    public PaypalOptions Paypal { get; set; } = new();
    public CurrencyExchangeOptions CurrencyExchangeRates { get; set; } = new();
}

public class VietQROptions
{
    public PlatformConnectionOptions PlatformConnection { get; set; } = new();
    public GateConnectionOptions GateConnection { get; set; } = new();
    public decimal FeeRate { get; set; }
    public bool BuyerPaysFee { get; set; }
}

public class PaypalOptions
{
    public string ClientId { get; set; }
    public string Secret { get; set; }
    public string BrandName { get; set; }
    public decimal FeeRate { get; set; }
    public bool BuyerPaysFee { get; set; }
    public string WebhookId { get; set; }
    public bool UseSandbox { get; set; }
}

public class PlatformConnectionOptions : ConnectionBaseOptions
{    
    public string TransactionSyncPath { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string UserBankName { get; set; } = string.Empty;
}

public class GateConnectionOptions : ConnectionBaseOptions
{
    public string NewTransactionPath { get; set; } = string.Empty;
}

public class ConnectionBaseOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenPath { get; set; } = string.Empty;
}

public class CurrencyExchangeOptions
{
    public string BaseCurrency { get; set; }

    public Dictionary<string, decimal> Rates { get; set; }
}
