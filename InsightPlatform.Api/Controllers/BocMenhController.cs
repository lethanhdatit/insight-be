using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BocMenhController(IWebHostEnvironment env
        , ILogger<BocMenhController> logger
        , IBocMenhBusiness bocMenhBusiness
    ) : BaseController(env, logger)
{
    private readonly IBocMenhBusiness _bocMenhBusiness = bocMenhBusiness;


    [HttpPost("theology")]
    public async Task<IActionResult> Theology([FromBody] TheologyRequest request)
    {
        var res = await _bocMenhBusiness.TheologyAndNumbersAsync(request);
        return HandleOk(res);
    }

    [HttpGet("theology/{id}")]
    public async Task<IActionResult> Theology([FromRoute] Guid id)
    {
        var res = await _bocMenhBusiness.GetTheologyAndNumbersAsync(id);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpPost("calendar")]
    public IActionResult GetLunarCalendar([FromBody] DateTime solarDate, [FromQuery] bool includeMonthDetail = false)
    {
        var res = _bocMenhBusiness.GetVietnameseCalendar(solarDate, includeMonthDetail);
        return HandleOk(res);
    }

    [HttpPost("tuTruBatTu")]
    public async Task<IActionResult> TuTruBatTu([FromBody] TuTruBatTuRequest request)
    {
        var res = await _bocMenhBusiness.TuTruBatTuAsync(request);
        return HandleOk(res);
    }

    [HttpGet("tuTruBatTu/{id}")]
    public async Task<IActionResult> TuTruBatTu([FromRoute] Guid id)
    {
        var res = await _bocMenhBusiness.GetTuTruBatTuAsync(id);
        return HandleOk(res);
    }
}