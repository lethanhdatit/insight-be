using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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

    [HttpGet("historical-sequences")]
    public async Task<IActionResult> GetHistoricalSequences([FromQuery] string prizeType, [FromQuery] int yearsBack = 5)
    {
        var res = await _luckyNumberBusiness.GetHistoricalSequencesAsync(prizeType, yearsBack);
        return HandleOk(res);
    }
    
    [HttpGet("historical-prizetype-flat")]
    public async Task<IActionResult> GetHistoricalPrizetypeFlat([FromQuery] string fromDate)
    {
        var res = await _luckyNumberBusiness.GetHistoricalPrizetypeFlatAsync(fromDate);
        return HandleOk(res);
    }
}