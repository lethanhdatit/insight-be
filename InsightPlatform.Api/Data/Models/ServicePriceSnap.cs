using System;

public class ServicePriceSnap
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte ServiceKind { get; set; }
    public int Fates { get; set; }
    public int? FatesDiscount { get; set; }
    public decimal? FatesDiscountRate { get; set; }
    public int FinalFates { get; set; }

    public ServicePriceSnap() { }

    public ServicePriceSnap(ServicePrice entity)
    {
       Id = entity.Id;
       Name = entity.Name;
       Description = entity.Description;
       ServiceKind = entity.ServiceKind;
       Fates = entity.Fates;
       FatesDiscount = entity.FatesDiscount;
       FatesDiscountRate = entity.FatesDiscountRate;
       FinalFates = entity.GetFinalFates();
    }
}