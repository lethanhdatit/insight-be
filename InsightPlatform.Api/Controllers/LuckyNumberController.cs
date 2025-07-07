using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LuckyNumberController(IWebHostEnvironment env
        , ILogger<LuckyNumberController> logger
        , ILuckyNumberBusiness luckyNumberBusiness
    ) : BaseController(env, logger)
{
    private readonly ILuckyNumberBusiness _luckyNumberBusiness = luckyNumberBusiness;

    [HttpPost("crawledData")]
    public async Task<IActionResult> ImportCrawledData(IFormFile file
        , [FromQuery] LuckyNumberProviderDto provider
        , bool isOverride
        , string crossCheckProviderName = null)
    {
        var res = await _luckyNumberBusiness.ImportCrawledDataAsync(file, provider, isOverride, crossCheckProviderName);
        return HandleOk(res);
    }

    [HttpPost("crawledData/build")]
    public async Task<IActionResult> Build([FromQuery] int? yearsBack = null, [FromQuery] bool isOverride = false)
    {
        var res = await _luckyNumberBusiness.BuildCrawledDataAsync(yearsBack, isOverride);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("historical-sequences")]
    public async Task<IActionResult> GetHistoricalSequences([FromQuery] string prizeType, [FromQuery] int yearsBack = 5)
    {
        var res = await _luckyNumberBusiness.GetHistoricalSequencesAsync(prizeType, yearsBack);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("historical-prizetype-flat")]
    public async Task<IActionResult> GetHistoricalPrizetypeFlat([FromQuery] string fromDate)
    {
        var res = await _luckyNumberBusiness.GetHistoricalPrizetypeFlatAsync(fromDate);
        return HandleOk(res);
    }

    [HttpPost("theology")]
    public async Task<IActionResult> Theology([FromBody] TheologyRequest request)
    {
        var res = await _luckyNumberBusiness.TheologyAndNumbersAsync(request);
        return HandleOk(res);
    }

    [HttpGet("theology/{id}")]
    public async Task<IActionResult> Theology([FromRoute] Guid id)
    {
        var res = await _luckyNumberBusiness.GetTheologyAndNumbersAsync(id);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpPost("calendar")]
    public IActionResult GetLunarCalendar([FromBody] DateTime solarDate, [FromQuery] bool includeMonthDetail = false)
    {
        var res = _luckyNumberBusiness.GetVietnameseCalendar(solarDate, includeMonthDetail);
        return HandleOk(res);
    }
}