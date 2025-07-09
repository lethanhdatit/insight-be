using System;
using System.Collections.Generic;
using System.Text.Json;

public class TransactionMetaData
{
    public TopUpPackageSnap TopUpPackageSnap { get; set; }

    public List<TransactionHictory> TransactionHictories { get; set; } = [];

    public List<TransactionEvent> Events { get; set; } = [];
}

public class TransactionEvent
{
    public string ModelNamespace { get; set; }

    public string Data { get; set; }
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

public static class TransactionEventExtensions
{
    public static dynamic DeserializeTransactionEvent(this TransactionEvent transactionEvent)
    {
        if (transactionEvent.ModelNamespace.IsMissing())
            throw new ArgumentNullException(nameof(transactionEvent.ModelNamespace));

        var type = Type.GetType(transactionEvent.ModelNamespace);
        if (type == null)
            throw new InvalidOperationException($"Cannot find type '{transactionEvent.ModelNamespace}'");

        return JsonSerializer.Deserialize(transactionEvent.Data, type);
    }

    public static TransactionEvent SerializeTransactionEvent(this object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return new TransactionEvent
        {
            ModelNamespace = obj.GetType().AssemblyQualifiedName,
            Data = JsonSerializer.Serialize(obj)
        };
    }
}