using System;

public class TopUpPackageSnap
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte Kind { get; set; }
    public byte Status { get; set; }

    public decimal Amount { get; set; }
    public decimal AmountAfterDiscount { get; set; }
    public decimal? AmountDiscount { get; set; }
    public decimal? AmountDiscountRate { get; set; }

    public int Fates { get; set; }
    public int FinalFates { get; set; }
    public int? FateBonus { get; set; }
    public decimal? FateBonusRate { get; set; }

    public DateTime CreatedTs { get; set; }
    public DateTime? LastUpdatedTs { get; set; }

    public TopUpPackageSnap() { }

    public TopUpPackageSnap(TopUpPackage entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        Kind = entity.Kind;
        Status = entity.Status;
        Amount = entity.Amount;
        AmountDiscount = entity.AmountDiscount;
        AmountDiscountRate = entity.AmountDiscountRate;
        AmountAfterDiscount = entity.GetAmountAfterDiscount();
        Fates = entity.Fates;
        FinalFates = entity.GetFinalFates();
        FateBonus = entity.FateBonus;
        FateBonusRate = entity.FateBonusRate;
        CreatedTs = entity.CreatedTs;
        LastUpdatedTs = entity.LastUpdatedTs;
    }
}