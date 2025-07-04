using System.Text.Json.Serialization;

public class TransactionCallback
{
    [JsonPropertyName("transactionid")]
    public string TransactionId { get; set; }

    [JsonPropertyName("transactiontime")]
    public long TransactionTime { get; set; }

    [JsonPropertyName("referencenumber")]
    public string ReferenceNumber { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("bankaccount")]
    public string BankAccount { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }

    [JsonPropertyName("sign")]
    public string Sign { get; set; }

    [JsonPropertyName("terminalCode")]
    public string TerminalCode { get; set; }

    [JsonPropertyName("urlLink")]
    public string UrlLink { get; set; }

    [JsonPropertyName("serviceCode")]
    public string ServiceCode { get; set; }

    [JsonPropertyName("subTerminalCode")]
    public string SubTerminalCode { get; set; }
}

public class SuccessResponse
{
    public bool Error { get; set; }
    public string ErrorReason { get; set; }
    public string ToastMessage { get; set; }
    public TransactionResponseObject Object { get; set; }
}

public class ErrorResponse
{
    public bool Error { get; set; }
    public string ErrorReason { get; set; }
    public string ToastMessage { get; set; }
    public object Object { get; set; }
}

public class TransactionResponseObject
{
    [JsonPropertyName("reftransactionid")]
    public string RefTransactionId { get; set; }
}
