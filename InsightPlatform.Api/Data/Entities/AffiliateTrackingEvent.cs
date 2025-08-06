using System;
using System.ComponentModel.DataAnnotations.Schema;

public enum AffiliateTrackingAction : byte
{
    View = 1,
    Click = 2,
    Share = 3,
    AddToFavorite = 4,
    RemoveFromFavorite = 5,
    Search = 6,
    Filter = 7
}

public class AffiliateTrackingEvent : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public Guid? UserId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public AffiliateTrackingAction Action { get; set; }
    
    public string SessionId { get; set; }
    public string UserAgent { get; set; }
    public string ClientIP { get; set; }
    public string Referrer { get; set; }
    
    // Additional data stored as JSONB (search terms, filter values, etc.)
    public string MetaData { get; set; }
    
    // Navigation properties
    public User User { get; set; }
    public AffiliateProduct Product { get; set; }
    public AffiliateCategory Category { get; set; }
}
