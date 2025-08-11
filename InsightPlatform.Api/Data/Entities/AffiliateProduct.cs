using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public enum AffiliateProductStatus : byte
{
    Active = 1,
    Inactive = 2,
    SoldOut = 3,
    Deleted = 4
}

public enum AffiliateProvider : byte
{
    Shopee = 1,
    Lazada = 2,
    Tiki = 3,
    Sendo = 4
}

public class AffiliateProduct : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public string ProviderId { get; set; }
    public AffiliateProvider Provider { get; set; }
    public string ProviderUrl { get; set; }
    public AffiliateProductStatus Status { get; set; } = AffiliateProductStatus.Active;
    
    // Pricing
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int Stock { get; set; }
    
    // Rating & Sales
    public decimal? Rating { get; set; }
    public long? RatingCount { get; set; }
    public int TotalSold { get; set; }
    
    // Location & Shipping
    public string SaleLocation { get; set; }
    
    // Localized content stored as JSONB
    // Structure: { "vi": { "name": "...", "description": "...", "promotion": "...", "warranty": "...", "shipping": "..." }, "en": {...} }
    public string LocalizedContent { get; set; }
    
    // Images stored as JSONB array
    // Structure: { "thumbnail": "url", "images": ["url1", "url2", ...] }
    public string Images { get; set; }
    
    // Attributes stored as JSONB array
    // Structure: [{ "name": "Color", "value": "Red" }, { "name": "Size", "value": "M" }]
    public string Attributes { get; set; }
    
    // Labels stored as JSONB array  
    // Structure: ["hot", "popular", "new", "bestseller"]
    public string Labels { get; set; }
    
    // Variants stored as JSONB
    // Structure: [{ "name": "Color", "imageUrl": "...", "values": [{ "valueText": "Red", "imageUrl": "..." }] }]
    public string Variants { get; set; }
    
    // Seller info stored as JSONB
    // Structure: { "name": "...", "imageUrl": "...", "description": "...", "labels": [{ "name": "...", "imageUrl": "..." }] }
    public string SellerInfo { get; set; }
    
    // Shipping options stored as JSONB
    // Structure: { "options": [{ "id": "...", "name": "...", "price": 0, "estimatedDays": 3, "provider": "..." }], "defaultShippingId": "...", "freeShippingAvailable": true, "freeShippingThreshold": 500000 }
    public string ShippingOptions { get; set; }
    
    public DateTime? LastSyncedTs { get; set; }
    
    // Navigation properties
    public ICollection<AffiliateProductCategory> ProductCategories { get; set; }
    public ICollection<AffiliateFavorite> Favorites { get; set; }
    public ICollection<AffiliateTrackingEvent> TrackingEvents { get; set; }
}
