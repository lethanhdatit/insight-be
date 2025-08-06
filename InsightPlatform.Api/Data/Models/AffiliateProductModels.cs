using System.Collections.Generic;

namespace InsightPlatform.Api.Data.Models
{
    /// <summary>
    /// Localized content for affiliate categories
    /// </summary>
    public class AffiliateCategoryLocalizedContent
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Localized content for affiliate products
    /// </summary>
    public class AffiliateProductLocalizedContent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Promotion { get; set; }
        public string Warranty { get; set; }
        public string Shipping { get; set; }
    }

    /// <summary>
    /// Product images structure
    /// </summary>
    public class ProductImages
    {
        public string Thumbnail { get; set; }
        public List<string> Gallery { get; set; }
        public string Video { get; set; }
    }

    /// <summary>
    /// Product attribute (e.g., Color: Red, Size: L)
    /// </summary>
    public class ProductAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; } // text, color, size, etc.
    }

    /// <summary>
    /// Product variant (different SKUs)
    /// </summary>
    public class ProductVariant
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public List<ProductAttribute> Attributes { get; set; }
        public string Image { get; set; }
    }

    /// <summary>
    /// Seller information
    /// </summary>
    public class ProductSeller
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public decimal Rating { get; set; }
        public int TotalProducts { get; set; }
        public int FollowerCount { get; set; }
        public string Location { get; set; }
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Shipping option details
    /// </summary>
    public class ShippingOption
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsFree { get; set; }
        public int EstimatedDays { get; set; }
        public string Provider { get; set; } // GHN, GHTK, ViettelPost, etc.
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Product shipping options
    /// </summary>
    public class ProductShippingOptions
    {
        public List<ShippingOption> Options { get; set; }
        public string DefaultShippingId { get; set; }
        public bool FreeShippingAvailable { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public string ShippingFrom { get; set; }
    }
}
