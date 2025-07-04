using System.Collections.Generic;

public class TransactionMetaData
{
    public List<TransactionHictory> TransactionHictories { get; set; } = [];
}

public class TransactionHictory
{
    public string TransactionId { get; set; }

    public long TransactionTime { get; set; }

    public string ReferenceNumber { get; set; }

    public decimal Amount { get; set; }

    public string Content { get; set; }

    public string BankAccount { get; set; }

    public string OrderId { get; set; }
}