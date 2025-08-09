using InsightPlatform.Api.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class AffiliateBusiness(ILogger<AffiliateBusiness> logger
        , IDbContextFactory<ApplicationDbContext> contextFactory
        , IHttpContextAccessor contextAccessor) : BaseHttpBusiness<AffiliateBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IAffiliateBusiness
{
    private const string DefaultLanguage = "vi";
    private const int MaxPageSize = 100;
    private const int MinPageSize = 1;
    private readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<BaseResponse<AffiliateFilterOptionsDto>> GetFilterOptionsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var language = GetCurrentLanguage();

        // Get active categories with localized content
        var categories = await context.AffiliateCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync();

        var categoryDtos = categories
            .Where(c => c.ParentId == null)
            .Select(c => MapCategoryToDto(c, categories, language))
            .ToList();

        // Get all active attributes from products
        var activeProducts = await context.AffiliateProducts
            .Where(p => p.Status == AffiliateProductStatus.Active
                     && p.Stock > 0
                     && (p.Attributes != null || p.Labels != null))
            .Select(p => new { p.Attributes, p.Labels })
            .ToListAsync();

        var attributes = new List<ProductAttribute>();
        var labels = new List<string>();

        foreach (var attributeJson in activeProducts.Where(a => a.Attributes.IsPresent() || a.Labels.IsPresent()))
        {
            if (attributeJson.Attributes.IsPresent())
            {
                try
                {
                    var productAttributes = GetLocalizedContent<List<ProductAttribute>>(attributeJson.Attributes, language);
                    if (productAttributes != null)
                        attributes.AddRange(productAttributes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize product attributes: {AttributeJson}", attributeJson.Attributes);
                }
            }

            if (attributeJson.Labels.IsPresent())
            {
                try
                {
                    var productLabels = GetLocalizedContent<List<string>>(attributeJson.Labels, language);
                    if (productLabels != null)
                        labels.AddRange(productLabels);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize product labels: {LabelJson}", attributeJson.Labels);
                }
            }
        }

        var distinctAttributes = attributes
        .GroupBy(a => a.Name)
        .Select(g => new ProductAttribute
        {
            Name = g.Key,
            Value = [.. g.SelectMany(x => x.Value).Distinct().OrderBy(v => v)],
            Type = g.FirstOrDefault()?.Type ?? "undefined",
        })
        .OrderBy(a => a.Name)
        .ToList();

        var distinctLabels = labels.Distinct().OrderBy(l => l).ToList();

        var result = new AffiliateFilterOptionsDto
        {
            Attributes = distinctAttributes,
            Labels = distinctLabels,
            Categories = categoryDtos
        };

        return new BaseResponse<AffiliateFilterOptionsDto>(result);
    }

    public async Task<BaseResponse<PaginatedBase<AffiliateProductListDto>>> GetProductsAsync(AffiliateProductFilterRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var language = GetCurrentLanguage();
        var userId = Current.UserId;

        // Validate pagination
        request.PageSize = Math.Max(MinPageSize, Math.Min(MaxPageSize, request.PageSize));
        request.PageNumber = Math.Max(1, request.PageNumber);

        var query = context.AffiliateProducts
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .Where(p => p.Status == AffiliateProductStatus.Active && p.Stock > 0);

        // Apply filters
        if (request.Provider.HasValue)
        {
            query = query.Where(p => p.Provider == request.Provider.Value);
        }

        if (request.CategoryIds?.Any() == true)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => request.CategoryIds.Contains(pc.CategoryId)));
        }

        if (request.HasDiscount.HasValue)
        {
            if (request.HasDiscount.Value)
            {
                query = query.Where(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.Price);
            }
            else
            {
                query = query.Where(p => !p.DiscountPrice.HasValue || p.DiscountPrice >= p.Price);
            }
        }

        if (request.PriceFrom.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= request.PriceFrom.Value);
        }

        if (request.PriceTo.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= request.PriceTo.Value);
        }

        if (request.Keywords.IsPresent())
        {
            var searchTerm = request.Keywords.ToTsQuery();
            if (searchTerm.IsPresent())
            {
                query = query.Where(p => EF.Functions.ToTsVector("simple", p.LocalizedContent).Matches(EF.Functions.ToTsQuery("simple", searchTerm)));
            }
        }

        // Get initial query without attribute/label filters (will filter in memory)
        var totalCount = await query.CountAsync();
        var allProducts = await query.ToListAsync();

        // Apply attribute and label filters in memory
        var filteredProducts = allProducts.AsEnumerable();

        if (request.Attributes?.Any() == true)
        {
            filteredProducts = filteredProducts.Where(p => ProductMatchesAttributes(p, request.Attributes, language));
        }

        if (request.Labels?.Any() == true)
        {
            filteredProducts = filteredProducts.Where(p => ProductMatchesLabels(p, request.Labels, language));
        }

        // Apply sorting
        filteredProducts = request.SortBy?.ToLower() switch
        {
            "price_asc" => filteredProducts.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price_desc" => filteredProducts.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "rating_asc" => filteredProducts.OrderBy(p => p.Rating ?? 0),
            "rating_desc" => filteredProducts.OrderByDescending(p => p.Rating ?? 0),
            _ => filteredProducts.OrderByDescending(p => p.CreatedTs) // Default: newest first
        };

        var filteredCount = filteredProducts.Count();
        var products = filteredProducts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Get user favorites if logged in
        var favoriteProductIds = new HashSet<Guid>();
        if (userId.HasValue)
        {
            favoriteProductIds = (await context.AffiliateFavorites
                .Where(f => f.UserId == userId.Value && products.Select(p => p.Id).Contains(f.ProductId))
                .Select(f => f.ProductId)
                .ToListAsync()).ToHashSet();
        }

        var productDtos = products.Select(p => MapProductToListDto(p, language, favoriteProductIds.Contains(p.Id), request.Attributes)).ToList();

        var result = new PaginatedBase<AffiliateProductListDto>
        {
            Items = productDtos,
            TotalRecords = filteredCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)filteredCount / request.PageSize)
        };

        return new BaseResponse<PaginatedBase<AffiliateProductListDto>>(result);
    }

    public async Task<BaseResponse<AffiliateProductDetailDto>> GetProductDetailAsync(Guid productId, AffiliateProductDetailRequest request = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var language = GetCurrentLanguage();
        var userId = Current.UserId;

        var product = await context.AffiliateProducts
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            throw new BusinessException("ProductNotFound", "Product not found");
        }

        // Check if user has favorited this product
        var isFavorite = false;
        if (userId.HasValue)
        {
            isFavorite = await context.AffiliateFavorites
                .AnyAsync(f => f.UserId == userId.Value && f.ProductId == productId);
        }

        var productDto = MapProductToDetailDto(product, language, isFavorite, request?.Attributes);

        // Track view event
        if (userId.HasValue || Current.ClientIP.IsPresent())
        {
            await TrackEventInternalAsync(context, new TrackingEventRequest
            {
                ProductId = productId,
                Action = AffiliateTrackingAction.View,
                SessionId = Current.AccessTokenId ?? Current.ClientIP
            });
        }

        return new BaseResponse<AffiliateProductDetailDto>(productDto);
    }

    public async Task<BaseResponse<bool>> AddToFavoriteAsync(AddFavoriteRequest request)
    {
        if (!Current.IsAuthenticated)
        {
            throw new BusinessException("Unauthorized", "User must be logged in to add favorites");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var userId = Current.UserId!.Value;

            // Check if product exists and is active
            var productExists = await context.AffiliateProducts
                .AnyAsync(p => p.Id == request.ProductId && p.Status == AffiliateProductStatus.Active);

            if (!productExists)
            {
                throw new BusinessException("ProductNotFound", "Product not found or inactive");
            }

            // Check if already favorited
            var existingFavorite = await context.AffiliateFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == request.ProductId);

            if (existingFavorite != null)
            {
                return new BaseResponse<bool>(true, "Already in favorites");
            }

            // Add to favorites
            var favorite = new AffiliateFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = request.ProductId
            };

            context.AffiliateFavorites.Add(favorite);
            await context.SaveChangesAsync();

            // Track event
            await TrackEventInternalAsync(context, new TrackingEventRequest
            {
                ProductId = request.ProductId,
                Action = AffiliateTrackingAction.AddToFavorite,
                SessionId = Current.AccessTokenId
            });

            await transaction.CommitAsync();
            return new BaseResponse<bool>(true, "Added to favorites successfully");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BaseResponse<bool>> RemoveFromFavoriteAsync(Guid productId)
    {
        if (!Current.IsAuthenticated)
        {
            throw new BusinessException("Unauthorized", "User must be logged in to remove favorites");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var userId = Current.UserId!.Value;

            var favorite = await context.AffiliateFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null)
            {
                return new BaseResponse<bool>(true, "Not in favorites");
            }

            context.AffiliateFavorites.Remove(favorite);
            await context.SaveChangesAsync();

            // Track event
            await TrackEventInternalAsync(context, new TrackingEventRequest
            {
                ProductId = productId,
                Action = AffiliateTrackingAction.RemoveFromFavorite,
                SessionId = Current.AccessTokenId
            });

            await transaction.CommitAsync();
            return new BaseResponse<bool>(true, "Removed from favorites successfully");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BaseResponse<PaginatedBase<AffiliateFavoriteDto>>> GetMyFavoritesAsync(int pageSize = 20, int pageNumber = 1)
    {
        if (!Current.IsAuthenticated)
        {
            throw new BusinessException("Unauthorized", "User must be logged in to view favorites");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var language = GetCurrentLanguage();
        var userId = Current.UserId!.Value;

        // Validate pagination
        pageSize = Math.Max(MinPageSize, Math.Min(MaxPageSize, pageSize));
        pageNumber = Math.Max(1, pageNumber);

        var query = context.AffiliateFavorites
            .Include(f => f.Product)
            .Where(f => f.UserId == userId && f.Product.Status == AffiliateProductStatus.Active)
            .OrderByDescending(f => f.CreatedTs);

        var totalCount = await query.CountAsync();
        var favorites = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var favoriteDtos = favorites.Select(f => new AffiliateFavoriteDto
        {
            Id = f.Id,
            Product = MapProductToListDto(f.Product, language, true),
            FavoritedAt = f.CreatedTs
        }).ToList();

        var result = new PaginatedBase<AffiliateFavoriteDto>
        {
            Items = favoriteDtos,
            TotalRecords = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return new BaseResponse<PaginatedBase<AffiliateFavoriteDto>>(result);
    }

    public async Task<BaseResponse<bool>> TrackEventAsync(TrackingEventRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await TrackEventInternalAsync(context, request);
        return new BaseResponse<bool>(true);
    }

    #region Private Methods

    private string GetCurrentLanguage()
    {
        var culture = Current.CurrentCulture?.Name ?? DefaultLanguage;

        // Extract language code only (e.g., "en-US" -> "en", "vi-VN" -> "vi")
        if (culture.Contains('-'))
        {
            return culture.Split('-')[0];
        }

        return culture;
    }

    private AffiliateCategoryDto MapCategoryToDto(AffiliateCategory category, List<AffiliateCategory> allCategories, string language)
    {
        var localizedContent = GetLocalizedContent<AffiliateCategoryLocalizedContent>(category.LocalizedContent, language);

        var dto = new AffiliateCategoryDto
        {
            Id = category.Id,
            Code = category.Code,
            Name = localizedContent?.Name ?? category.Code,
            Description = localizedContent?.Description,
            ParentId = category.ParentId
        };

        // Add children
        var children = allCategories
            .Where(c => c.ParentId == category.Id)
            .Select(c => MapCategoryToDto(c, allCategories, language))
            .ToList();

        if (children.Count != 0)
        {
            dto.Children = children;
        }

        return dto;
    }

    private AffiliateProductListDto MapProductToListDto(AffiliateProduct product, string language, bool isFavorite, List<string> filterAttributes = null)
    {
        var localizedContent = GetLocalizedContent<AffiliateProductLocalizedContent>(product.LocalizedContent, language);
        var images = GetJsonObject<ProductImages>(product.Images);
        var labels = GetLocalizedContent<List<string>>(product.Labels, language) ?? [];
        var attributes = GetLocalizedContent<List<ProductAttribute>>(product.Attributes, language) ?? [];

        // Process attributes with matching logic if filter is provided
        if (filterAttributes?.Any() == true)
        {
            attributes = ProcessAttributesWithMatching(attributes, filterAttributes);
        }

        return new AffiliateProductListDto
        {
            Id = product.Id,
            AutoId = product.AutoId,
            Provider = product.Provider,
            ProviderUrl = product.ProviderUrl,
            Status = product.Status,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            DiscountPercentage = product.DiscountPercentage,
            Stock = product.Stock,
            Rating = product.Rating,
            TotalSold = product.TotalSold,
            Name = localizedContent?.Name ?? "N/A",
            ThumbnailImage = images?.Thumbnail,
            Attributes = attributes,
            Labels = labels,
            IsFavorite = isFavorite
        };
    }

    private AffiliateProductDetailDto MapProductToDetailDto(AffiliateProduct product, string language, bool isFavorite, List<string> filterAttributes = null)
    {
        var localizedContent = GetLocalizedContent<AffiliateProductLocalizedContent>(product.LocalizedContent, language);
        var images = GetJsonObject<ProductImages>(product.Images);
        var attributes = GetLocalizedContent<List<ProductAttribute>>(product.Attributes, language) ?? [];
        var labels = GetLocalizedContent<List<string>>(product.Labels, language) ?? [];
        var variants = GetLocalizedContent<List<ProductVariant>>(product.Variants, language) ?? [];
        var seller = GetLocalizedContent<ProductSeller>(product.SellerInfo, language);
        var shippingOptions = GetLocalizedContent<ProductShippingOptions>(product.ShippingOptions, language);
        var categories = product.ProductCategories?.Select(pc => MapCategoryToDto(pc.Category, [], language)).ToList() ?? [];

        // Process attributes with matching logic if filter is provided
        if (filterAttributes?.Any() == true)
        {
            attributes = ProcessAttributesWithMatching(attributes, filterAttributes);
        }

        return new AffiliateProductDetailDto
        {
            Id = product.Id,
            AutoId = product.AutoId,
            Provider = product.Provider,
            ProviderUrl = product.ProviderUrl,
            Status = product.Status,
            Name = localizedContent?.Name ?? "N/A",
            Description = localizedContent?.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            DiscountPercentage = product.DiscountPercentage,
            Stock = product.Stock,
            Rating = product.Rating,
            TotalSold = product.TotalSold,
            SaleLocation = product.SaleLocation,
            Promotion = localizedContent?.Promotion,
            Warranty = localizedContent?.Warranty,
            Shipping = localizedContent?.Shipping,
            Images = images,
            Attributes = attributes,
            Labels = labels,
            Variants = variants,
            Seller = seller,
            ShippingOptions = shippingOptions,
            Categories = categories,
            IsFavorite = isFavorite
        };
    }

    private T GetLocalizedContent<T>(string localizedContentJson, string language) where T : class
    {
        if (localizedContentJson.IsMissing())
            return null;

        try
        {
            // First deserialize as JsonElement to handle any structure
            using var document = JsonDocument.Parse(localizedContentJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            // Try current language first
            if (root.TryGetProperty(language, out var langElement))
            {
                return JsonSerializer.Deserialize<T>(langElement.GetRawText(), JsonSerializerOptions);
            }

            // Try default language
            if (root.TryGetProperty(DefaultLanguage, out var defaultElement))
            {
                return JsonSerializer.Deserialize<T>(defaultElement.GetRawText(), JsonSerializerOptions);
            }

            // Return first available language
            foreach (var prop in root.EnumerateObject())
            {
                return JsonSerializer.Deserialize<T>(prop.Value.GetRawText(), JsonSerializerOptions);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize localized content: {Content}", localizedContentJson);
            return null;
        }
    }

    private T GetJsonObject<T>(string jsonString) where T : class
    {
        if (jsonString.IsMissing())
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(jsonString, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON: {Json}", jsonString);
            return null;
        }
    }

    private async Task TrackEventInternalAsync(ApplicationDbContext context, TrackingEventRequest request)
    {
        try
        {
            var trackingEvent = new AffiliateTrackingEvent
            {
                UserId = Current.UserId,
                ProductId = request.ProductId,
                CategoryId = request.CategoryId,
                Action = request.Action,
                SessionId = request.SessionId ?? Current.AccessTokenId ?? Current.ClientIP,
                UserAgent = Current.UA?.RawUserAgent,
                ClientIP = Current.ClientIP,
                Referrer = CurrentRequest()?.Headers.Referer.ToString(),
                MetaData = request.MetaData != null ? JsonSerializer.Serialize(request.MetaData) : null
            };

            context.AffiliateTrackingEvents.Add(trackingEvent);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track event: {Action} for Product: {ProductId}", request.Action, request.ProductId);
        }
    }

    private List<ProductAttribute> ProcessAttributesWithMatching(List<ProductAttribute> attributes, List<string> filterAttributes)
    {
        if (attributes == null || !attributes.Any())
            return new List<ProductAttribute>();

        if (filterAttributes == null || !filterAttributes.Any())
            return attributes;

        // Process each attribute to check for matching
        var processedAttributes = new List<ProductAttribute>();

        foreach (var attribute in attributes)
        {
            var processedAttribute = new ProductAttribute
            {
                Name = attribute.Name,
                Value = attribute.Value,
                Type = attribute.Type,
                IsMatched = IsAttributeMatched(attribute, filterAttributes)
            };

            processedAttributes.Add(processedAttribute);
        }

        // Sort by IsMatched: matched items first, then by name
        return processedAttributes
            .OrderByDescending(a => a.IsMatched)
            .ThenBy(a => a.Name)
            .ToList();
    }

    private bool IsAttributeMatched(ProductAttribute attribute, List<string> filterAttributes)
    {
        if (attribute == null || attribute.Value == null || !attribute.Value.Any() || filterAttributes == null || !filterAttributes.Any())
            return false;

        // Check if any filter attribute matches this product attribute
        // Filter format: "name:value" or just "value"
        foreach (var filter in filterAttributes)
        {
            if (filter.Contains(':'))
            {
                var parts = filter.Split(':', 2);
                if (parts.Length == 2)
                {
                    var filterName = parts[0].Trim();
                    var filterValue = parts[1].Trim();
                    
                    if (string.Equals(attribute.Name, filterName, StringComparison.OrdinalIgnoreCase) &&
                        attribute.Value.Any(v => string.Equals(v, filterValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Just value matching - check if any value in the list matches
                if (attribute.Value.Any(v => string.Equals(v, filter.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ProductMatchesAttributes(AffiliateProduct product, List<string> filterAttributes, string language)
    {
        if (product.Attributes.IsMissing() || filterAttributes == null || !filterAttributes.Any())
            return true;

        try
        {
            var attributes = GetLocalizedContent<List<ProductAttribute>>(product.Attributes, language);
            if (attributes == null || !attributes.Any())
                return false;

            // Check if all filter attributes match
            foreach (var filter in filterAttributes)
            {
                var parts = filter.Split(':');
                if (parts.Length == 2)
                {
                    var filterName = parts[0].Trim();
                    var filterValue = parts[1].Trim();
                    
                    var hasMatch = attributes.Any(attr => 
                        string.Equals(attr.Name, filterName, StringComparison.OrdinalIgnoreCase) &&
                        attr.Value != null && attr.Value.Any(v => string.Equals(v, filterValue, StringComparison.OrdinalIgnoreCase)));
                    
                    if (!hasMatch)
                        return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse product attributes for filtering: {ProductId}", product.Id);
            return false;
        }
    }

    private bool ProductMatchesLabels(AffiliateProduct product, List<string> filterLabels, string language)
    {
        if (product.Labels.IsMissing() || filterLabels == null || !filterLabels.Any())
            return true;

        try
        {
            var labels = GetLocalizedContent<List<string>>(product.Labels, language);
            if (labels == null || !labels.Any())
                return false;

            // Check if any filter label matches
            return filterLabels.Any(filterLabel => 
                labels.Any(label => string.Equals(label, filterLabel, StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse product labels for filtering: {ProductId}", product.Id);
            return false;
        }
    }

    #endregion
}