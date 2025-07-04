using System.Collections.Generic;
using System.Text.Json.Serialization;

public class VietQrPaymentRequest
{
    [JsonPropertyName("bankCode")]
    public string BankCode { get; set; }

    [JsonPropertyName("bankAccount")]
    public string BankAccount { get; set; }

    [JsonPropertyName("userBankName")]
    public string UserBankName { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("qrType")]
    public int QrType { get; set; }

    [JsonPropertyName("amount")]
    public long? Amount { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }

    [JsonPropertyName("transType")]
    public string TransType { get; set; }

    [JsonPropertyName("terminalCode")]
    public string TerminalCode { get; set; }

    [JsonPropertyName("serviceCode")]
    public string ServiceCode { get; set; }

    [JsonPropertyName("subTerminalCode")]
    public string SubTerminalCode { get; set; }

    [JsonPropertyName("sign")]
    public string Sign { get; set; }
    
    [JsonPropertyName("note")]
    public string Note { get; set; } 
    
    [JsonPropertyName("urlLink")]
    public string UrlLink { get; set; }
}

public class VietQrPaymentResponse
{
    [JsonPropertyName("bankCode")]
    public string BankCode { get; set; }

    [JsonPropertyName("bankAccount")]
    public string BankAccount { get; set; }

    [JsonPropertyName("userBankName")]
    public string UserBankName { get; set; }

    [JsonPropertyName("amount")]
    public string Amount { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("terminalCode")]
    public string TerminalCode { get; set; }

    [JsonPropertyName("subTerminalCode")]
    public string SubTerminalCode { get; set; }

    [JsonPropertyName("serviceCode")]
    public string ServiceCode { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }


    [JsonPropertyName("bankName")]
    public string BankName { get; set; }

    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; }

    [JsonPropertyName("imgId")]
    public string ImgId { get; set; }

    [JsonPropertyName("existing")]
    public long Existing { get; set; }

    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; }

    [JsonPropertyName("transactionRefId")]
    public string TransactionRefId { get; set; }

    [JsonPropertyName("qrLink")]
    public string QrLink { get; set; }

    [JsonPropertyName("additionalData")]
    public List<dynamic> AdditionalData { get; set; }

    [JsonPropertyName("vaAccount")]
    public string VaAccount { get; set; }
}

public class VietQrPaymentMetaData
{
    public VietQrPaymentRequest Request { get; set; }

    public VietQrPaymentResponse Response { get; set; }
}