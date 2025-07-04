public class PaymentGateOptions
{
    public const string Path = "PaymentGate";

    public VietQROptions VietQR { get; set; } = new();
}

public class VietQROptions
{
    public PlatformConnectionOptions PlatformConnection { get; set; } = new();
    public GateConnectionOptions GateConnection { get; set; } = new();
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
