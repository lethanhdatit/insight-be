using System;

public class BuyTopupRequest
{
    public Guid? Id { get; set; }

    public Guid? TopupPackageId { get; set; }

    public TransactionProvider? Provider { get; set; }

    public string CallbackUrl { get; set; }
}