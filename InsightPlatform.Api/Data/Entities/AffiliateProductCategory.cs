using System;
using System.ComponentModel.DataAnnotations.Schema;

public class AffiliateProductCategory : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public Guid ProductId { get; set; }
    public Guid CategoryId { get; set; }
    
    // Navigation properties
    public AffiliateProduct Product { get; set; }
    public AffiliateCategory Category { get; set; }
}
