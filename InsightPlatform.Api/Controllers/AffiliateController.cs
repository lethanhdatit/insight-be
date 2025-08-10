using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AffiliateController(IWebHostEnvironment env
        , ILogger<AffiliateController> logger
        , IAffiliateBusiness affiliateBusiness
        , IAffiliateInitBusiness affiliateInitBusiness
    ) : BaseController(env, logger)
{
    private readonly IAffiliateBusiness _affiliateBusiness = affiliateBusiness;
    private readonly IAffiliateInitBusiness _affiliateInitBusiness = affiliateInitBusiness;

    [AllowAnonymous]
    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _affiliateBusiness.GetFilterOptionsAsync();
        return HandleOk(result);
    }

    [AllowAnonymous]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] AffiliateProductFilterRequest request)
    {
        var result = await _affiliateBusiness.GetProductsAsync(request);
        return HandleOk(result);
    }

    [AllowAnonymous]
    [HttpGet("products/{productId}")]
    public async Task<IActionResult> GetProductDetail([FromRoute] string productId, [FromQuery] AffiliateProductDetailRequest request = null)
    {
        var result = await _affiliateBusiness.GetProductDetailAsync(productId, request);
        return HandleOk(result);
    }

    [Authorize]
    [HttpPost("favorites")]
    public async Task<IActionResult> AddToFavorite([FromBody] AddFavoriteRequest request)
    {
        var result = await _affiliateBusiness.AddToFavoriteAsync(request);
        return HandleOk(result);
    }

    [Authorize]
    [HttpDelete("favorites/{productId}")]
    public async Task<IActionResult> RemoveFromFavorite([FromRoute] Guid productId)
    {
        var result = await _affiliateBusiness.RemoveFromFavoriteAsync(productId);
        return HandleOk(result);
    }

    [Authorize]
    [HttpGet("favorites")]
    public async Task<IActionResult> GetMyFavorites([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        var result = await _affiliateBusiness.GetMyFavoritesAsync(pageSize, pageNumber);
        return HandleOk(result);
    }

    [AllowAnonymous]
    [HttpPost("tracking")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackingEventRequest request)
    {
        var result = await _affiliateBusiness.TrackEventAsync(request);
        return HandleOk(result);
    }

    [AllowAnonymous]
    [HttpPost("seed-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        var result = await _affiliateInitBusiness.SeedSampleDataAsync();
        return HandleOk(result);
    }
}
