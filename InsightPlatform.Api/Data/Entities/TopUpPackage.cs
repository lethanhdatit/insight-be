using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class TopUpPackage : Trackable
{
    public Guid Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }    

    public string Name { get; set; }
    public string Description { get; set; }
    public byte Kind { get; set; }
    public byte Status { get; set; }

    public decimal Amount { get; set; }
    public decimal? AmountDiscount { get; set; }
    public decimal? AmountDiscountRate { get; set; }

    public int Fates { get; set; }
    public int? FateBonus { get; set; }
    public decimal? FateBonusRate { get; set; }

    public ICollection<Transaction> Transactions { get; set; }

    public int GetFinalFates()
    {
        var finalFates = Fates;

        if (FateBonus != null)
            finalFates += FateBonus.Value;

        if (FateBonusRate != null)
            finalFates = (int)Math.Ceiling(finalFates + (Fates * FateBonusRate.Value));

        return Math.Max(finalFates, 0);
    }

    public decimal GetAmountAfterDiscount()
    {
        var finalAmount = Amount;

        if (AmountDiscount != null)
            finalAmount -= AmountDiscount.Value;

        if (AmountDiscountRate != null)
            finalAmount = Math.Ceiling(finalAmount - (Amount * AmountDiscountRate.Value));

        return Math.Max(finalAmount, 0);
    }
}
