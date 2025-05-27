using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class PainsController(IWebHostEnvironment env
        , ILogger<PainsController> logger
        , IPainBusiness painBusiness) : BaseController(env, logger)
{
    private readonly IPainBusiness _painBusiness = painBusiness;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PainDto dto)
    {
        var res = await _painBusiness.InsertPain(dto);
        return HandleOk(res);
    }
}