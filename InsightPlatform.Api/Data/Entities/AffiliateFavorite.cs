using System;
using System.ComponentModel.DataAnnotations.Schema;

public class AffiliateFavorite : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    
    // Navigation properties
    public User User { get; set; }
    public AffiliateProduct Product { get; set; }
}
