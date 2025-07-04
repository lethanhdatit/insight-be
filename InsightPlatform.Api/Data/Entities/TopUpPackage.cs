using System;
using System.Collections.Generic;

public class TopUpPackage
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte Kind { get; set; }
    public byte Status { get; set; }

    public decimal Amount { get; set; }
    public decimal? AmountDiscount { get; set; }
    public double? AmountDiscountRate { get; set; }

    public int Fates { get; set; }
    public int? FateBonus { get; set; }
    public double? FateBonusRate { get; set; }

    public DateTime CreatedTs { get; set; }

    public ICollection<Transaction> Transactions { get; set; }

    public int GetFinalFates()
    {
        var finalFates = Fates;

        if (FateBonus != null)
            finalFates += FateBonus.Value;

        if (FateBonusRate != null)
            finalFates = (int)Math.Ceiling(finalFates + (Fates * FateBonusRate.Value / 100));

        return Math.Max(finalFates, 0);
    }

    public decimal GetFinalAmount()
    {
        var finalAmount = Amount;

        if (AmountDiscount != null)
            finalAmount -= AmountDiscount.Value;

        if (AmountDiscountRate != null)
            finalAmount = Math.Ceiling(finalAmount - (Amount * (decimal)AmountDiscountRate.Value / 100));

        return Math.Max(finalAmount, 0);
    }
}
