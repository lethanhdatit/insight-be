using System;
using System.Threading.Tasks;
public interface IAffiliateBusiness
{
    // Product APIs
    Task<BaseResponse<AffiliateFilterOptionsDto>> GetFilterOptionsAsync();
    Task<BaseResponse<PaginatedBase<AffiliateProductListDto>>> GetProductsAsync(AffiliateProductFilterRequest request);
    Task<BaseResponse<AffiliateProductDetailDto>> GetProductDetailAsync(Guid productId);

    // Favorite APIs
    Task<BaseResponse<bool>> AddToFavoriteAsync(AddFavoriteRequest request);
    Task<BaseResponse<bool>> RemoveFromFavoriteAsync(Guid productId);
    Task<BaseResponse<PaginatedBase<AffiliateFavoriteDto>>> GetMyFavoritesAsync(int pageSize = 20, int pageNumber = 1);

    // Tracking API
    Task<BaseResponse<bool>> TrackEventAsync(TrackingEventRequest request);
}