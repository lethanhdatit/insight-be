using System;
using System.Collections.Generic;
using InsightPlatform.Api.Data.Models;

// Request DTOs
public class AffiliateProductFilterRequest
{
    public List<string> Attributes { get; set; }
    public List<string> Labels { get; set; }
    public string Keywords { get; set; }
    public List<Guid> CategoryIds { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }
    public AffiliateProvider? Provider { get; set; }
    public string SortBy { get; set; } = "relevance"; // relevance, price_asc, price_desc, rating_asc, rating_desc
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}

public class AffiliateProductDetailRequest
{
    public List<string> Attributes { get; set; } // Optional: For matching with user preferences
}

public class AddFavoriteRequest
{
    public Guid ProductId { get; set; }
}

public class TrackingEventRequest
{
    public Guid? ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public AffiliateTrackingAction Action { get; set; }
    public string SessionId { get; set; }
    public object MetaData { get; set; }
}

// Response DTOs
public class AffiliateProductListDto
{
    public Guid Id { get; set; }
    public long AutoId { get; set; }
    public AffiliateProvider Provider { get; set; }
    public string ProviderUrl { get; set; }
    public AffiliateProductStatus Status { get; set; }
    public string Slug { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int Stock { get; set; }
    public decimal? Rating { get; set; }
    public int TotalSold { get; set; }
    public string Name { get; set; }
    public string ThumbnailImage { get; set; }
    public List<ProductAttribute> Attributes { get; set; } // Add attributes to list DTO
    public List<string> Labels { get; set; }
    public bool IsFavorite { get; set; }
}

public class AffiliateProductDetailDto
{
    public Guid Id { get; set; }
    public long AutoId { get; set; }
    public AffiliateProvider Provider { get; set; }
    public string ProviderUrl { get; set; }
    public AffiliateProductStatus Status { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int Stock { get; set; }
    public decimal? Rating { get; set; }
    public int TotalSold { get; set; }
    public string SaleLocation { get; set; }
    public string Promotion { get; set; }
    public string Warranty { get; set; }
    public string Shipping { get; set; }
    public ProductImages Images { get; set; }
    public List<ProductAttribute> Attributes { get; set; }
    public List<string> Labels { get; set; }
    public List<ProductVariant> Variants { get; set; }
    public ProductSeller Seller { get; set; }
    public ProductShippingOptions ShippingOptions { get; set; }
    public List<AffiliateCategoryDto> Categories { get; set; }
    public bool IsFavorite { get; set; }
}

public class AffiliateCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? ParentId { get; set; }
    public List<AffiliateCategoryDto> Children { get; set; }
}

public class AffiliateFilterOptionsDto
{
    public List<ProductAttribute> Attributes { get; set; }
    public List<string> Labels { get; set; }
    public List<AffiliateCategoryDto> Categories { get; set; }
}

public class AffiliateFavoriteDto
{
    public Guid Id { get; set; }
    public AffiliateProductListDto Product { get; set; }
    public DateTime FavoritedAt { get; set; }
}

// Supporting DTOs
public class ProductImages
{
    public string Thumbnail { get; set; }
    public List<string> Images { get; set; }
}

public class ProductAttribute
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    public bool IsMatched { get; set; } = false;
}

public class ProductVariant
{
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public List<ProductVariantValue> Values { get; set; }
}

public class ProductVariantValue
{
    public string ValueText { get; set; }
    public string ImageUrl { get; set; }
}

public class ProductSeller
{
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public List<SellerLabel> Labels { get; set; }
}

public class SellerLabel
{
    public string Name { get; set; }
    public string ImageUrl { get; set; }
}
