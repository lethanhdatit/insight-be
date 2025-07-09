using System;

public class ServicePrice
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte ServiceKind { get; set; }
    public int Fates { get; set; }
    public int? FatesDiscount { get; set; }
    public decimal? FatesDiscountRate { get; set; }
        
    public int GetFinalFates()
    {
        var finalFates = Fates;

        if (FatesDiscount != null)
            finalFates -= FatesDiscount.Value;

        if (FatesDiscountRate != null)
            finalFates = (int)Math.Ceiling(finalFates - (finalFates * FatesDiscountRate.Value / 100));

        return Math.Max(finalFates, 0);
    }
}
