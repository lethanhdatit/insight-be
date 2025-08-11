using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

            // Create sample categories with parent-child structure
            var categories = new List<AffiliateCategory>();
            
            // Parent categories
            var parentCategories = new[]
            {
                new { code = "phong-thuy", sortOrder = 1, nameVi = "Phong Thủy", nameEn = "Feng Shui", descVi = "Sản phẩm phong thủy mang lại may mắn", descEn = "Feng shui products bringing good luck" },
                new { code = "trang-suc", sortOrder = 2, nameVi = "Trang Sức", nameEn = "Jewelry", descVi = "Trang sức phong thủy và may mắn", descEn = "Feng shui and lucky jewelry" },
                new { code = "nha-cua", sortOrder = 3, nameVi = "Nhà Cửa", nameEn = "Home & Decor", descVi = "Đồ trang trí nội thất phong thủy", descEn = "Feng shui home decoration" },
                new { code = "van-phong", sortOrder = 4, nameVi = "Văn Phòng", nameEn = "Office", descVi = "Vật phẩm phong thủy cho văn phòng", descEn = "Office feng shui items" },
                new { code = "suc-khoe", sortOrder = 5, nameVi = "Sức Khỏe", nameEn = "Health", descVi = "Sản phẩm hỗ trợ sức khỏe", descEn = "Health support products" }
            };

            foreach (var parent in parentCategories)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = parent.code,
                    SortOrder = parent.sortOrder,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = parent.nameVi, description = parent.descVi },
                        ["en"] = new { name = parent.nameEn, description = parent.descEn }
                    })
                });
            }

            // Child categories for Phong Thủy
            var phongThuyParent = categories.First(c => c.Code == "phong-thuy");
            var phongThuyChildren = new[]
            {
                new { code = "dong-tien", nameVi = "Đồng Tiền", nameEn = "Coins", descVi = "Tiền xu phong thủy", descEn = "Feng shui coins" },
                new { code = "tuong-phat", nameVi = "Tượng Phật", nameEn = "Buddha Statues", descVi = "Tượng Phật và Quan Âm", descEn = "Buddha and Guanyin statues" },
                new { code = "cay-tai-loc", nameVi = "Cây Tài Lộc", nameEn = "Money Trees", descVi = "Cây phong thủy thu hút tài lộc", descEn = "Money trees attracting wealth" }
            };

            foreach (var child in phongThuyChildren)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = child.code,
                    ParentId = phongThuyParent.Id,
                    SortOrder = categories.Count + 1,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = child.nameVi, description = child.descVi },
                        ["en"] = new { name = child.nameEn, description = child.descEn }
                    })
                });
            }

            // Child categories for Trang Sức
            var trangSucParent = categories.First(c => c.Code == "trang-suc");
            var trangSucChildren = new[]
            {
                new { code = "vong-tay", nameVi = "Vòng Tay", nameEn = "Bracelets", descVi = "Vòng tay đá phong thủy", descEn = "Feng shui stone bracelets" },
                new { code = "day-chuyen", nameVi = "Dây Chuyền", nameEn = "Necklaces", descVi = "Dây chuyền mặt đá quý", descEn = "Gemstone necklaces" },
                new { code = "nhan", nameVi = "Nhẫn", nameEn = "Rings", descVi = "Nhẫn đá phong thủy", descEn = "Feng shui rings" }
            };

            foreach (var child in trangSucChildren)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = child.code,
                    ParentId = trangSucParent.Id,
                    SortOrder = categories.Count + 1,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = child.nameVi, description = child.descVi },
                        ["en"] = new { name = child.nameEn, description = child.descEn }
                    })
                });
            }

            // Child categories for Nhà Cửa
            var nhaCuaParent = categories.First(c => c.Code == "nha-cua");
            var nhaCuaChildren = new[]
            {
                new { code = "tranh-phong-thuy", nameVi = "Tranh Phong Thủy", nameEn = "Feng Shui Paintings", descVi = "Tranh treo tường phong thủy", descEn = "Feng shui wall paintings" },
                new { code = "guong-bat-quai", nameVi = "Gương Bát Quái", nameEn = "Bagua Mirrors", descVi = "Gương bát quái hóa giải", descEn = "Bagua mirrors for protection" },
                new { code = "den-tho", nameVi = "Đèn Thờ", nameEn = "Altar Lamps", descVi = "Đèn thờ gia tiên", descEn = "Ancestor altar lamps" }
            };

            foreach (var child in nhaCuaChildren)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = child.code,
                    ParentId = nhaCuaParent.Id,
                    SortOrder = categories.Count + 1,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = child.nameVi, description = child.descVi },
                        ["en"] = new { name = child.nameEn, description = child.descEn }
                    })
                });
            }

            // Child categories for Văn Phòng
            var vanPhongParent = categories.First(c => c.Code == "van-phong");
            var vanPhongChildren = new[]
            {
                new { code = "cau-da", nameVi = "Cầu Đá", nameEn = "Stone Bridges", descVi = "Cầu đá phong thủy", descEn = "Feng shui stone bridges" },
                new { code = "trum-tre", nameVi = "Trúm Tre", nameEn = "Bamboo Items", descVi = "Đồ vật bằng tre", descEn = "Bamboo feng shui items" },
                new { code = "ban-tho-than-tai", nameVi = "Bàn Thờ Thần Tài", nameEn = "Wealth God Altars", descVi = "Bàn thờ thần tài", descEn = "Wealth god altars" }
            };

            foreach (var child in vanPhongChildren)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = child.code,
                    ParentId = vanPhongParent.Id,
                    SortOrder = categories.Count + 1,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = child.nameVi, description = child.descVi },
                        ["en"] = new { name = child.nameEn, description = child.descEn }
                    })
                });
            }

            // Child categories for Sức Khỏe
            var sucKhoeParent = categories.First(c => c.Code == "suc-khoe");
            var sucKhoeChildren = new[]
            {
                new { code = "da-phong-thuy", nameVi = "Đá Phong Thủy", nameEn = "Feng Shui Stones", descVi = "Đá quý hỗ trợ sức khỏe", descEn = "Healing gemstones" },
                new { code = "vong-co", nameVi = "Vòng Cổ", nameEn = "Neck Accessories", descVi = "Vòng cổ đá phong thủy", descEn = "Feng shui neck accessories" }
            };

            foreach (var child in sucKhoeChildren)
            {
                categories.Add(new AffiliateCategory
                {
                    Id = Guid.NewGuid(),
                    Code = child.code,
                    ParentId = sucKhoeParent.Id,
                    SortOrder = categories.Count + 1,
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new { name = child.nameVi, description = child.descVi },
                        ["en"] = new { name = child.nameEn, description = child.descEn }
                    })
                });
            }

            context.AffiliateCategories.AddRange(categories);
            await context.SaveChangesAsync();

            // Create 30 diverse sample products
            var products = new List<AffiliateProduct>();
            var random = new Random(42); // Fixed seed for consistent data
            
            var productTemplates = new[]
            {
                // Vòng tay - có discount
                new { 
                    nameVi = "Vòng tay thạch anh đỏ may mắn", nameEn = "Red crystal lucky bracelet",
                    descVi = "Vòng tay thạch anh đỏ thiên nhiên", descEn = "Natural red crystal bracelet",
                    price = 299000m, discount = 199000m, provider = AffiliateProvider.Shopee,
                    colors = new[] { "Đỏ", "Xanh", "Tím" }, sizes = new[] { "16cm", "17cm", "18cm" },
                    materials = new[] { "Thạch anh thiên nhiên" }, labels = new[] { "hot", "bestseller", "mới" },
                    categoryCode = "vong-tay", stock = 50, rating = 4.5m, ratingCount = 1500, sold = 234
                },
                // Tiền xu - không discount
                new { 
                    nameVi = "Tiền vàng phong thủy may mắn", nameEn = "Lucky feng shui gold coin",
                    descVi = "Tiền vàng mạ vàng 24k thu hút tài lộc", descEn = "24k gold plated coin attracting wealth",
                    price = 150000m, discount = 0m, provider = AffiliateProvider.Lazada,
                    colors = new[] { "Vàng", "Bạc" }, sizes = new[] { "3cm", "3.5cm", "4cm" },
                    materials = new[] { "Mạ vàng 24k", "Mạ bạc" }, labels = new[] { "phổ biến", "truyền thống" },
                    categoryCode = "dong-tien", stock = 100, rating = 4.2m, ratingCount = 150, sold = 567
                },
                // Tượng Phật - có discount
                new { 
                    nameVi = "Tượng Phật Di Lặc cầu an", nameEn = "Laughing Buddha statue",
                    descVi = "Tượng Phật Di Lặc bằng đá cẩm thạch", descEn = "Marble Laughing Buddha statue",
                    price = 850000m, discount = 650000m, provider = AffiliateProvider.Tiki,
                    colors = new[] { "Trắng", "Xanh lá" }, sizes = new[] { "15cm", "20cm", "25cm" },
                    materials = new[] { "Đá cẩm thạch", "Gỗ mun" }, labels = new[] { "cao cấp", "handmade" },
                    categoryCode = "tuong-phat", stock = 25, rating = 4.8m, ratingCount = 750, sold = 89
                },
                // Cây tài lộc - không discount
                new { 
                    nameVi = "Cây tài lộc mini để bàn", nameEn = "Mini money tree desktop",
                    descVi = "Cây tài lộc bằng đá thạch anh xanh", descEn = "Green quartz money tree",
                    price = 320000m, discount = 0m, provider = AffiliateProvider.Shopee,
                    colors = new[] { "Xanh lá", "Vàng kim" }, sizes = new[] { "12cm", "15cm" },
                    materials = new[] { "Thạch anh xanh", "Đồng thau" }, labels = new[] { "bán chạy", "văn phòng" },
                    categoryCode = "cay-tai-loc", stock = 80, rating = 4.3m, ratingCount = 35000, sold = 156
                },
                // Dây chuyền - có discount lớn
                new { 
                    nameVi = "Dây chuyền mặt Phật Quan Âm", nameEn = "Guanyin pendant necklace",
                    descVi = "Dây chuyền vàng 18k mặt Quan Âm", descEn = "18k gold Guanyin necklace",
                    price = 2500000m, discount = 1800000m, provider = AffiliateProvider.Lazada,
                    colors = new[] { "Vàng" }, sizes = new[] { "40cm", "45cm", "50cm" },
                    materials = new[] { "Vàng 18k", "Bạc 925" }, labels = new[] { "luxury", "limited" },
                    categoryCode = "day-chuyen", stock = 5, rating = 4.9m, ratingCount = 9810147, sold = 12
                },
                // Nhẫn - có discount nhỏ
                new { 
                    nameVi = "Nhẫn đá ruby phong thủy", nameEn = "Ruby feng shui ring",
                    descVi = "Nhẫn bạc 925 đá ruby thiên nhiên", descEn = "925 silver natural ruby ring",
                    price = 1200000m, discount = 1100000m, provider = AffiliateProvider.Tiki,
                    colors = new[] { "Đỏ ruby", "Xanh sapphire" }, sizes = new[] { "Size 6", "Size 7", "Size 8", "Size 9" },
                    materials = new[] { "Bạc 925", "Ruby thiên nhiên" }, labels = new[] { "precious", "authentic" },
                    categoryCode = "nhan", stock = 15, rating = 4.7m, ratingCount = 20, sold = 34
                },
                // Tranh phong thủy - không discount
                new { 
                    nameVi = "Tranh song mã phong thủy", nameEn = "Feng shui horse painting",
                    descVi = "Tranh canvas song mã phi nước đại", descEn = "Canvas galloping horses painting",
                    price = 450000m, discount = 0m, provider = AffiliateProvider.Shopee,
                    colors = new[] { "Đa màu" }, sizes = new[] { "40x60cm", "60x90cm", "80x120cm" },
                    materials = new[] { "Canvas", "Khung gỗ" }, labels = new[] { "decor", "feng-shui" },
                    categoryCode = "tranh-phong-thuy", stock = 60, rating = 4.4m, ratingCount = 1, sold = 203
                },
                // Gương bát quái - có discount
                new { 
                    nameVi = "Gương bát quái hóa giải", nameEn = "Bagua protection mirror",
                    descVi = "Gương bát quái đồng thau hóa giải", descEn = "Brass bagua mirror for protection",
                    price = 180000m, discount = 150000m, provider = AffiliateProvider.Lazada,
                    colors = new[] { "Đồng", "Bạc" }, sizes = new[] { "8cm", "10cm", "12cm" },
                    materials = new[] { "Đồng thau", "Gương thật" }, labels = new[] { "protection", "traditional" },
                    categoryCode = "guong-bat-quai", stock = 40, rating = 4.6m, ratingCount = 2, sold = 128
                }
            };

            // Generate products based on templates with variations
            for (int i = 0; i < 30; i++)
            {
                var template = productTemplates[i % productTemplates.Length];
                var variation = i / productTemplates.Length + 1;
                
                // Add variation to names
                var nameVi = variation > 1 ? $"{template.nameVi} - Phiên bản {variation}" : template.nameVi;
                var nameEn = variation > 1 ? $"{template.nameEn} - Version {variation}" : template.nameEn;
                
                // Vary prices slightly
                var priceMultiplier = (decimal)(1 + (random.NextDouble() - 0.5) * 0.3); // ±15%
                var price = template.price * priceMultiplier;
                var discountPrice = template.discount > 0 ? template.discount * priceMultiplier : (decimal?)null;
                
                // Vary stock and ratings
                var stock = Math.Max(1, template.stock + random.Next(-20, 21));
                var rating = Math.Round(template.rating + (decimal)(random.NextDouble() - 0.5) * 0.6m, 1);
                var ratingCount = template.ratingCount;
                var totalSold = template.sold + random.Next(-50, 101);
                
                // Random provider for variation
                var providers = new[] { AffiliateProvider.Shopee, AffiliateProvider.Lazada, AffiliateProvider.Tiki };
                var provider = i < 10 ? template.provider : providers[random.Next(providers.Length)];
                
                products.Add(new AffiliateProduct
                {
                    Id = Guid.NewGuid(),
                    ProviderId = $"{provider.ToString().ToLower()}-{random.Next(100000, 999999)}",
                    Provider = provider,
                    ProviderUrl = $"https://{provider.ToString().ToLower()}.vn/product/{random.Next(100000, 999999)}",
                    Price = price,
                    DiscountPrice = discountPrice,
                    DiscountPercentage = discountPrice.HasValue ? Math.Round((price - discountPrice.Value) / price * 100, 2) : null,
                    Stock = stock,
                    Rating = rating,
                    RatingCount = ratingCount,
                    TotalSold = totalSold,
                    SaleLocation = random.Next(2) == 0 ? "TP.HCM" : "Hà Nội",
                    LocalizedContent = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new {
                            name = nameVi,
                            description = template.descVi,
                            promotion = discountPrice.HasValue ? "Khuyến mãi đặc biệt!" : "Sản phẩm chất lượng cao",
                            warranty = random.Next(2) == 0 ? "Bảo hành 12 tháng" : "Bảo hành 6 tháng",
                            shipping = "Miễn phí vận chuyển"
                        },
                        ["en"] = new {
                            name = nameEn,
                            description = template.descEn,
                            promotion = discountPrice.HasValue ? "Special promotion!" : "High quality product",
                            warranty = random.Next(2) == 0 ? "12 months warranty" : "6 months warranty",
                            shipping = "Free shipping"
                        }
                    }),
                    Images = JsonSerializer.Serialize(new
                    {
                        thumbnail = $"https://example.com/product-{i + 1}-thumb.jpg",
                        images = new[] {
                            $"https://example.com/product-{i + 1}-1.jpg",
                            $"https://example.com/product-{i + 1}-2.jpg",
                            $"https://example.com/product-{i + 1}-3.jpg"
                        }
                    }),
                    Attributes = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new[]
                        {
                            new { name = "Màu sắc", value = template.colors, type = "color" },
                            new { name = "Kích thước", value = template.sizes, type = "size" },
                            new { name = "Chất liệu", value = template.materials, type = "material" },
                            new { name = "Xuất xứ", value = new[] { random.Next(2) == 0 ? "Việt Nam" : "Trung Quốc" }, type = "origin" }
                        },
                        ["en"] = new[]
                        {
                            new { name = "Color", value = template.colors, type = "color" },
                            new { name = "Size", value = template.sizes, type = "size" },
                            new { name = "Material", value = template.materials, type = "material" },
                            new { name = "Origin", value = new[] { random.Next(2) == 0 ? "Vietnam" : "China" }, type = "origin" }
                        }
                    }),
                    Labels = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = template.labels,
                        ["en"] = template.labels
                    }),
                    Variants = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new[]
                        {
                            new {
                                name = "Kích thước",
                                imageUrl = $"https://example.com/size-guide-{i + 1}.jpg",
                                values = template.sizes.Select((size, idx) => new { 
                                    valueText = size, 
                                    imageUrl = $"https://example.com/product-{i + 1}-size-{idx + 1}.jpg" 
                                }).ToArray()
                            }
                        },
                        ["en"] = new[]
                        {
                            new {
                                name = "Size",
                                imageUrl = $"https://example.com/size-guide-{i + 1}.jpg",
                                values = template.sizes.Select((size, idx) => new { 
                                    valueText = size, 
                                    imageUrl = $"https://example.com/product-{i + 1}-size-{idx + 1}.jpg" 
                                }).ToArray()
                            }
                        }
                    }),
                    SellerInfo = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new {
                            name = $"Shop{(i % 5) + 1}Store",
                            imageUrl = $"https://example.com/seller-{(i % 5) + 1}-logo.jpg",
                            description = "Chuyên bán đồ phong thủy chính hãng",
                            labels = new[]
                            {
                                new { name = "Uy tín", imageUrl = "https://example.com/trusted.jpg" },
                                new { name = random.Next(2) == 0 ? "Bán chạy" : "Chất lượng", imageUrl = "https://example.com/quality.jpg" }
                            }
                        },
                        ["en"] = new {
                            name = $"Shop{(i % 5) + 1}Store",
                            imageUrl = $"https://example.com/seller-{(i % 5) + 1}-logo.jpg",
                            description = "Specialized in authentic feng shui products",
                            labels = new[]
                            {
                                new { name = "Trusted", imageUrl = "https://example.com/trusted.jpg" },
                                new { name = random.Next(2) == 0 ? "Bestseller" : "Quality", imageUrl = "https://example.com/quality.jpg" }
                            }
                        }
                    }),
                    ShippingOptions = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["vi"] = new {
                            options = random.Next(2) == 0 ? new[]
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
                                }
                            } : new[]
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
                                }
                            },
                            defaultShippingId = random.Next(2) == 0 ? "standard" : "express",
                            freeShippingAvailable = random.Next(3) == 0,
                            freeShippingThreshold = random.Next(3) == 0 ? 500000m : (decimal?)null,
                            shippingFrom = random.Next(2) == 0 ? "Hồ Chí Minh" : "Hà Nội"
                        },
                        ["en"] = new {
                            options = random.Next(2) == 0 ? new[]
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
                                }
                            } : new[]
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
                                }
                            },
                            defaultShippingId = random.Next(2) == 0 ? "standard" : "express",
                            freeShippingAvailable = random.Next(3) == 0,
                            freeShippingThreshold = random.Next(3) == 0 ? 500000m : (decimal?)null,
                            shippingFrom = random.Next(2) == 0 ? "Ho Chi Minh City" : "Hanoi"
                        }
                    }),
                    LastSyncedTs = DateTime.UtcNow
                });
            }

            context.AffiliateProducts.AddRange(products);
            await context.SaveChangesAsync();

            // Create product-category relationships
            var productCategories = new List<AffiliateProductCategory>();
            var categoryMap = new Dictionary<string, Guid>();
            
            // Build category code to ID mapping
            foreach (var category in categories)
            {
                categoryMap[category.Code] = category.Id;
            }
            
            // Map products to categories based on their template category
            var categoryMappings = new[]
            {
                "vong-tay", "dong-tien", "tuong-phat", "cay-tai-loc", "day-chuyen", 
                "nhan", "tranh-phong-thuy", "guong-bat-quai", "den-tho"
            };
            
            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var templateIndex = i % productTemplates.Length;
                var categoryCode = productTemplates[templateIndex].categoryCode;
                
                // Add to specific category
                if (categoryMap.ContainsKey(categoryCode))
                {
                    productCategories.Add(new AffiliateProductCategory
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        CategoryId = categoryMap[categoryCode]
                    });
                    
                    // Also add to parent category
                    var childCategory = categories.First(c => c.Code == categoryCode);
                    if (childCategory.ParentId.HasValue)
                    {
                        productCategories.Add(new AffiliateProductCategory
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            CategoryId = childCategory.ParentId.Value
                        });
                    }
                }
                
                // Some products can be in multiple categories for testing
                if (i % 7 == 0 && categoryCode != "tuong-phat") // Add some products to Tượng Phật category
                {
                    if (categoryMap.ContainsKey("tuong-phat"))
                    {
                        productCategories.Add(new AffiliateProductCategory
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            CategoryId = categoryMap["tuong-phat"]
                        });
                    }
                }
            }

            context.AffiliateProductCategories.AddRange(productCategories);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return new BaseResponse<bool>(true, "Successfully seeded 30 products and 20 categories with diverse data for comprehensive testing!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error seeding affiliate sample data");
            throw new BusinessException("SeedDataError", "Failed to seed sample data", ex);
        }
    }
}