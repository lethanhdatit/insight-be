using System;

public class TransactionCheckoutDto
{
    public Guid? Id { get; set; }
    public TransactionStatus? Status { get; set; }
    public TransactionProvider? Provider { get; set; }
    public string Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? Total { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountTotal { get; set; }
    public decimal? FinalTotal { get; set; }
    public decimal? Paid { get; set; }
    public bool BuyerPaysFee { get; set; }
    public decimal? FeeRate { get; set; } // in percent
    public decimal? FeeTotal { get; set; }
    public bool VATaxIncluded { get; set; }
    public decimal? VATaxRate { get; set; } // in percent
    public decimal? VATaxTotal { get; set; }
    public string Note { get; set; }
    public string PackageName { get; set; }
    public int? Fates { get; set; }
    public int? FinalFates { get; set; }
    public int? FateBonus { get; set; }
    public decimal? FateBonusRate { get; set; }
    public dynamic ProviderMeta { get; set; }
    public TransactionMetaData Meta { get; set; }
}