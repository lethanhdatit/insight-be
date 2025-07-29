using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IWebHostEnvironment env
        , ILogger<AccountController> logger
        , IAccountBusiness accountBusiness) : BaseController(env, logger)
{
    private readonly IAccountBusiness _accountBusiness = accountBusiness;

    [HttpPost("init")]
    public async Task<IActionResult> Post()
    {
        var res = await _accountBusiness.InitGuest();
        return HandleOk(res);
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] string idToken)
    {
        var res = await _accountBusiness.GoogleLoginAsync(idToken);
        return HandleOk(res);
    }
    
    [HttpPost("facebook")]
    public async Task<IActionResult> FacebookLogin([FromBody] string accessToken)
    {
        var res = await _accountBusiness.FacebookLoginAsync(accessToken);
        return HandleOk(res);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest payload)
    {
        var res = await _accountBusiness.RegisterAsync(payload);
        return HandleOk(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest payload)
    {
        var res = await _accountBusiness.LoginAsync(payload);
        return HandleOk(res);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var res = await _accountBusiness.GetMeAsync();
        return HandleOk(res);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateRequest payload)
    {
        var res = await _accountBusiness.UpdateMeAsync(payload);
        return HandleOk(res);
    }
}