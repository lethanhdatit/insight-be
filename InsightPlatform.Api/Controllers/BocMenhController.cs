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
    
    #region BatTuTuTru

    [HttpPost("tuTruBatTu")]
    public async Task<IActionResult> InitTuTruBatTu([FromBody] TuTruBatTuRequest request)
    {
        var res = await _bocMenhBusiness.InitTuTruBatTuAsync(request);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpPost("tuTruBatTu/test")]
    public async Task<IActionResult> ExplainTuTruBatTuTest([FromBody] TuTruBatTuRequest request)
    {
        var res = await _bocMenhBusiness.ExplainTuTruBatTuTestAsync(request);
        return HandleOk(res);
    }

    [HttpGet("tuTruBatTu/{id}")]
    public async Task<IActionResult> GetTuTruBatTu([FromRoute] Guid id)
    {
        var res = await _bocMenhBusiness.GetTuTruBatTuAsync(id);
        return HandleOk(res);
    }

    #endregion BatTuTuTru    
}