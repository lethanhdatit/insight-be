using System;

public class TopupPackageDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TopUpPackageKind Kind { get; set; }
    public int? Fates { get; set; }
    public int? FateBonus { get; set; }
    public decimal? FateBonusRate { get; set; } // in percent, e.g., 10 for 10%
    public int? FinalFates { get; set; }
    public decimal? Amount { get; set; }
    public decimal? AmountDiscount { get; set; }
    public decimal? AmountDiscountRate { get; set; } // in percent
    public decimal? FinalAmount { get; set; }
    public bool VATaxIncluded { get; set; }
    public decimal? VATaxRate { get; set; } // in percent
    public DateTime CreatedTs { get; set; }
    public string Currency { get; set; } = "VND";
}