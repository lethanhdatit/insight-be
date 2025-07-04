using System;
using System.Collections.Generic;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TopUpPackageId { get; set; }
    public byte Status { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public byte Provider { get; set; }
    public string ProviderTransaction { get; set; }
    public string MetaData { get; set; }
    public string Note { get; set; }
    public DateTime CreatedTs { get; set; }

    public User User { get; set; }
    public TopUpPackage TopUpPackage { get; set; }
    public ICollection<FatePointTransaction> FatePointTransactions { get; set; }
}
