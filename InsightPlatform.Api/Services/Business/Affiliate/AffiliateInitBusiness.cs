using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public interface IAffiliateInitBusiness
{
    Task<BaseResponse<bool>> SeedSampleDataAsync();
}

public class AffiliateInitBusiness(ILogger<AffiliateInitBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor) : BaseHttpBusiness<AffiliateInitBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IAffiliateInitBusiness
{
    public async Task<BaseResponse<bool>> SeedSampleDataAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Clear existing data to re-seed with shipping options
            context.AffiliateTrackingEvents.RemoveRange(context.AffiliateTrackingEvents);
            context.AffiliateFavorites.RemoveRange(context.AffiliateFavorites);
            context.AffiliateProductCategories.RemoveRange(context.AffiliateProductCategories);
            context.AffiliateProducts.RemoveRange(context.AffiliateProducts);
            context.AffiliateCategories.RemoveRange(context.AffiliateCategories);
            await context.SaveChangesAsync();

            // Create sample categories
            var categories = new List<AffiliateCategory>
                {
                    new AffiliateCategory
                    {
                        Id = Guid.NewGuid(),
                        Code = "phong-thuy",
                        SortOrder = 1,
                        LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new { name = "Phong Thủy", description = "Sản phẩm phong thủy mang lại may mắn" },
                            ["en"] = new { name = "Feng Shui", description = "Feng shui products bringing good luck" }
                        })
                    },
                    new AffiliateCategory
                    {
                        Id = Guid.NewGuid(),
                        Code = "bat-tu",
                        SortOrder = 2,
                        LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new { name = "Bát Tự", description = "Vật phẩm liên quan đến bát tự học" },
                            ["en"] = new { name = "Ba Zi", description = "Items related to Ba Zi astrology" }
                        })
                    },
                    new AffiliateCategory
                    {
                        Id = Guid.NewGuid(),
                        Code = "trang-suc",
                        SortOrder = 3,
                        LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new { name = "Trang Sức", description = "Trang sức phong thủy, may mắn" },
                            ["en"] = new { name = "Jewelry", description = "Feng shui and lucky jewelry" }
                        })
                    }
                };

            context.AffiliateCategories.AddRange(categories);
            await context.SaveChangesAsync();

            // Create sample products
            var products = new List<AffiliateProduct>
                {
                    new AffiliateProduct
                    {
                        Id = Guid.NewGuid(),
                        ProviderId = "shopee-123456",
                        Provider = AffiliateProvider.Shopee,
                        ProviderUrl = "https://shopee.vn/product/123456",
                        Price = 299000,
                        DiscountPrice = 199000,
                        DiscountPercentage = 33.44m,
                        Stock = 50,
                        Rating = 4.5m,
                        TotalSold = 234,
                        SaleLocation = "TP.HCM",
                        LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                name = "Vòng tay thạch anh đỏ may mắn",
                                description = "Vòng tay thạch anh đỏ thiên nhiên, mang lại may mắn và tài lộc",
                                promotion = "Giảm 33% - Chỉ hôm nay!",
                                warranty = "Bảo hành 12 tháng",
                                shipping = "Miễn phí vận chuyển toàn quốc"
                            },
                            ["en"] = new {
                                name = "Red crystal lucky bracelet",
                                description = "Natural red crystal bracelet bringing luck and prosperity",
                                promotion = "33% OFF - Today only!",
                                warranty = "12 months warranty",
                                shipping = "Free nationwide shipping"
                            }
                        }),
                        Images = JsonSerializer.Serialize(new
                        {
                            thumbnail = "https://example.com/bracelet-thumb.jpg",
                            images = new[] {
                                "https://example.com/bracelet-1.jpg",
                                "https://example.com/bracelet-2.jpg",
                                "https://example.com/bracelet-3.jpg"
                            }
                        }),
                        Attributes = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[]
                            {
                                new { name = "Màu sắc", value = "Đỏ", type = "color" },
                                new { name = "Kích thước", value = "16-18cm", type = "size" },
                                new { name = "Chất liệu", value = "Thạch anh thiên nhiên", type = "material" }
                            },
                            ["en"] = new[]
                            {
                                new { name = "Color", value = "Red", type = "color" },
                                new { name = "Size", value = "16-18cm", type = "size" },
                                new { name = "Material", value = "Natural crystal", type = "material" }
                            }
                        }),
                        Labels = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[] { "hot", "bestseller", "mới" },
                            ["en"] = new[] { "hot", "bestseller", "new" }
                        }),
                        Variants = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[]
                            {
                                new {
                                    name = "Kích thước",
                                    imageUrl = "https://example.com/size-guide.jpg",
                                    values = new[]
                                    {
                                        new { valueText = "16cm", imageUrl = "https://example.com/16cm.jpg" },
                                        new { valueText = "17cm", imageUrl = "https://example.com/17cm.jpg" },
                                        new { valueText = "18cm", imageUrl = "https://example.com/18cm.jpg" }
                                    }
                                }
                            },
                            ["en"] = new[]
                            {
                                new {
                                    name = "Size",
                                    imageUrl = "https://example.com/size-guide.jpg",
                                    values = new[]
                                    {
                                        new { valueText = "16cm", imageUrl = "https://example.com/16cm.jpg" },
                                        new { valueText = "17cm", imageUrl = "https://example.com/17cm.jpg" },
                                        new { valueText = "18cm", imageUrl = "https://example.com/18cm.jpg" }
                                    }
                                }
                            }
                        }),
                        SellerInfo = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                name = "PhongThuyStore",
                                imageUrl = "https://example.com/seller-logo.jpg",
                                description = "Chuyên bán đồ phong thủy chính hãng",
                                labels = new[]
                                {
                                    new { name = "Uy tín", imageUrl = "https://example.com/trusted.jpg" },
                                    new { name = "Bán chạy", imageUrl = "https://example.com/bestseller.jpg" }
                                }
                            },
                            ["en"] = new {
                                name = "PhongThuyStore",
                                imageUrl = "https://example.com/seller-logo.jpg",
                                description = "Specialized in authentic feng shui products",
                                labels = new[]
                                {
                                    new { name = "Trusted", imageUrl = "https://example.com/trusted.jpg" },
                                    new { name = "Bestseller", imageUrl = "https://example.com/bestseller.jpg" }
                                }
                            }
                        }),
                        ShippingOptions = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                options = new[]
                                {
                                    new {
                                        id = "standard",
                                        name = "Giao hàng tiêu chuẩn",
                                        description = "Giao trong 3-5 ngày làm việc",
                                        price = 30000m,
                                        isFree = false,
                                        estimatedDays = 4,
                                        provider = "GHN",
                                        isDefault = true
                                    },
                                    new {
                                        id = "fast",
                                        name = "Giao hàng nhanh",
                                        description = "Giao trong 1-2 ngày làm việc",
                                        price = 50000m,
                                        isFree = false,
                                        estimatedDays = 1,
                                        provider = "GHTK",
                                        isDefault = false
                                    },
                                    new {
                                        id = "free",
                                        name = "Miễn phí vận chuyển",
                                        description = "Miễn phí cho đơn hàng trên 500K",
                                        price = 0m,
                                        isFree = true,
                                        estimatedDays = 7,
                                        provider = "ViettelPost",
                                        isDefault = false
                                    }
                                },
                                defaultShippingId = "standard",
                                freeShippingAvailable = true,
                                freeShippingThreshold = 500000m,
                                shippingFrom = "Hồ Chí Minh"
                            },
                            ["en"] = new {
                                options = new[]
                                {
                                    new {
                                        id = "standard",
                                        name = "Standard shipping",
                                        description = "Delivery in 3-5 business days",
                                        price = 30000m,
                                        isFree = false,
                                        estimatedDays = 4,
                                        provider = "GHN",
                                        isDefault = true
                                    },
                                    new {
                                        id = "fast",
                                        name = "Fast shipping",
                                        description = "Delivery in 1-2 business days",
                                        price = 50000m,
                                        isFree = false,
                                        estimatedDays = 1,
                                        provider = "GHTK",
                                        isDefault = false
                                    },
                                    new {
                                        id = "free",
                                        name = "Free shipping",
                                        description = "Free for orders over 500K",
                                        price = 0m,
                                        isFree = true,
                                        estimatedDays = 7,
                                        provider = "ViettelPost",
                                        isDefault = false
                                    }
                                },
                                defaultShippingId = "standard",
                                freeShippingAvailable = true,
                                freeShippingThreshold = 500000m,
                                shippingFrom = "Ho Chi Minh City"
                            }
                        }),
                        LastSyncedTs = DateTime.UtcNow
                    },
                    new AffiliateProduct
                    {
                        Id = Guid.NewGuid(),
                        ProviderId = "lazada-789012",
                        Provider = AffiliateProvider.Lazada,
                        ProviderUrl = "https://lazada.vn/product/789012",
                        Price = 150000,
                        Stock = 100,
                        Rating = 4.2m,
                        TotalSold = 567,
                        SaleLocation = "Hà Nội",
                        LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                name = "Tiền vàng phong thủy may mắn",
                                description = "Tiền vàng phong thủy mạ vàng 24k, thu hút tài lộc",
                                promotion = "Mua 2 tặng 1",
                                warranty = "Bảo hành vĩnh viễn",
                                shipping = "Giao hàng trong 24h"
                            },
                            ["en"] = new {
                                name = "Lucky feng shui gold coin",
                                description = "24k gold plated feng shui coin attracting wealth",
                                promotion = "Buy 2 get 1 free",
                                warranty = "Lifetime warranty",
                                shipping = "24h delivery"
                            }
                        }),
                        Images = JsonSerializer.Serialize(new
                        {
                            thumbnail = "https://example.com/coin-thumb.jpg",
                            images = new[] {
                                "https://example.com/coin-1.jpg",
                                "https://example.com/coin-2.jpg"
                            }
                        }),
                        Attributes = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[]
                            {
                                new { name = "Chất liệu", value = "Mạ vàng 24k", type = "material" },
                                new { name = "Đường kính", value = "3cm", type = "size" },
                                new { name = "Xuất xứ", value = "Việt Nam", type = "origin" }
                            },
                            ["en"] = new[]
                            {
                                new { name = "Material", value = "24k gold plated", type = "material" },
                                new { name = "Diameter", value = "3cm", type = "size" },
                                new { name = "Origin", value = "Vietnam", type = "origin" }
                            }
                        }),
                        Labels = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[] { "phổ biến", "truyền thống", "cao cấp" },
                            ["en"] = new[] { "popular", "traditional", "premium" }
                        }),
                        Variants = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new[]
                            {
                                new {
                                    name = "Màu sắc",
                                    imageUrl = "https://example.com/color-guide.jpg",
                                    values = new[]
                                    {
                                        new { valueText = "Vàng", imageUrl = "https://example.com/gold.jpg" },
                                        new { valueText = "Bạc", imageUrl = "https://example.com/silver.jpg" }
                                    }
                                }
                            },
                            ["en"] = new[]
                            {
                                new {
                                    name = "Color",
                                    imageUrl = "https://example.com/color-guide.jpg",
                                    values = new[]
                                    {
                                        new { valueText = "Gold", imageUrl = "https://example.com/gold.jpg" },
                                        new { valueText = "Silver", imageUrl = "https://example.com/silver.jpg" }
                                    }
                                }
                            }
                        }),
                        SellerInfo = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                name = "TaiLocShop",
                                imageUrl = "https://example.com/tailoc-logo.jpg",
                                description = "Chuyên phong thủy truyền thống",
                                labels = new[]
                                {
                                    new { name = "Chất lượng", imageUrl = "https://example.com/quality.jpg" },
                                    new { name = "Uy tín", imageUrl = "https://example.com/trusted.jpg" }
                                }
                            },
                            ["en"] = new {
                                name = "TaiLocShop",
                                imageUrl = "https://example.com/tailoc-logo.jpg",
                                description = "Specialized in traditional feng shui",
                                labels = new[]
                                {
                                    new { name = "Quality", imageUrl = "https://example.com/quality.jpg" },
                                    new { name = "Trusted", imageUrl = "https://example.com/trusted.jpg" }
                                }
                            }
                        }),
                        ShippingOptions = JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["vi"] = new {
                                options = new[]
                                {
                                    new {
                                        id = "express",
                                        name = "Giao hàng hỏa tốc",
                                        description = "Giao trong 24h",
                                        price = 80000m,
                                        isFree = false,
                                        estimatedDays = 1,
                                        provider = "Grab",
                                        isDefault = true
                                    },
                                    new {
                                        id = "standard",
                                        name = "Giao hàng tiêu chuẩn",
                                        description = "Giao trong 2-3 ngày",
                                        price = 25000m,
                                        isFree = false,
                                        estimatedDays = 2,
                                        provider = "ViettelPost",
                                        isDefault = false
                                    }
                                },
                                defaultShippingId = "express",
                                freeShippingAvailable = false,
                                shippingFrom = "Hà Nội"
                            },
                            ["en"] = new {
                                options = new[]
                                {
                                    new {
                                        id = "express",
                                        name = "Express delivery",
                                        description = "Delivery within 24h",
                                        price = 80000m,
                                        isFree = false,
                                        estimatedDays = 1,
                                        provider = "Grab",
                                        isDefault = true
                                    },
                                    new {
                                        id = "standard",
                                        name = "Standard shipping",
                                        description = "Delivery in 2-3 days",
                                        price = 25000m,
                                        isFree = false,
                                        estimatedDays = 2,
                                        provider = "ViettelPost",
                                        isDefault = false
                                    }
                                },
                                defaultShippingId = "express",
                                freeShippingAvailable = false,
                                shippingFrom = "Hanoi"
                            }
                        }),
                        LastSyncedTs = DateTime.UtcNow
                    }
                };

            context.AffiliateProducts.AddRange(products);
            await context.SaveChangesAsync();

            // Create product-category relationships
            var productCategories = new List<AffiliateProductCategory>
                {
                    new AffiliateProductCategory
                    {
                        Id = Guid.NewGuid(),
                        ProductId = products[0].Id,
                        CategoryId = categories[0].Id // phong-thuy
                    },
                    new AffiliateProductCategory
                    {
                        Id = Guid.NewGuid(),
                        ProductId = products[0].Id,
                        CategoryId = categories[2].Id // trang-suc
                    },
                    new AffiliateProductCategory
                    {
                        Id = Guid.NewGuid(),
                        ProductId = products[1].Id,
                        CategoryId = categories[0].Id // phong-thuy
                    }
                };

            context.AffiliateProductCategories.AddRange(productCategories);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return new BaseResponse<bool>(true, "Sample data seeded successfully with shipping options!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error seeding affiliate sample data");
            throw new BusinessException("SeedDataError", "Failed to seed sample data", ex);
        }
    }
}