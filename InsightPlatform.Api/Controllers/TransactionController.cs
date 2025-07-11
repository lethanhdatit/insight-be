using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionController(IWebHostEnvironment env
        , ILogger<TransactionController> logger
        , ITransactionBusiness transactionBusiness
        , IAccountBusiness accountBusiness
        , IPayPalService payPalService
        , IOptions<PaymentOptions> paymentSettings
    ) : BaseController(env, logger)
{
    private readonly PaymentOptions _paymentSettings = paymentSettings.Value;
    private readonly ITransactionBusiness _transactionBusiness = transactionBusiness;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IPayPalService _payPalService = payPalService;

    [HttpGet("topups/me")]
    public async Task<IActionResult> GetMyFates()
    {
        var res = await _transactionBusiness.GetUserFates();
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("topups")]
    public async Task<IActionResult> GetTopups()
    {
        var res = await _transactionBusiness.GetTopupsAsync();
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("topups/memoCheckout")]
    public async Task<IActionResult> GetMemoCheckout([FromQuery] Guid topupPackageId, [FromQuery] TransactionProvider provider)
    {
        var res = await _transactionBusiness.GetMemoCheckoutAsync(topupPackageId, provider);
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("paymentGates")]
    public IActionResult GetPaymentGates()
    {
        var res = Enum.GetValues<TransactionProvider>().Select(s =>
        {
            var parts = s.GetDescription()?.Split('|') ?? [];
            var description = parts.ElementAtOrDefault(0);
            var icon = parts.ElementAtOrDefault(1);
            var active = parts.ElementAtOrDefault(2);

            return new
            {
                Id = (byte)s,
                Name = s.ToString(),
                Description = description,
                Icon = icon,
                Active = !active.IsMissing() && bool.TryParse(active, out var _active) && _active,
            };
        }).ToList();

        return HandleOk(new BaseResponse<dynamic>(res));
    }

    [HttpPost("topups")]
    public async Task<IActionResult> BuyTopup([FromBody] BuyTopupRequest request)
    {
        var res = await _transactionBusiness.BuyTopupAsync(request);
        return HandleOk(res);
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> CheckStatus([FromRoute] Guid id)
    {
        var res = await _transactionBusiness.CheckStatusAsync(id);
        return HandleOk(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        var res = await _transactionBusiness.CancelAsync(id);
        return HandleOk(res);
    }

    #region VietQR IPN

    [AllowAnonymous]
    [HttpPost("vqr/ipn/api/token_generate")]
    public IActionResult VietQrToken([FromHeader] string Authorization)
    {
        if (Authorization.IsMissing() || !Authorization.StartsWith("Basic "))
        {
            return BadRequest("Authorization header is missing or invalid");
        }

        var base64Credentials = Authorization.Substring("Basic ".Length).Trim();
        var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
        var values = credentials.Split(':', 2);

        if (values.Length != 2)
        {
            return BadRequest("Invalid Authorization header format");
        }

        var username = values[0];
        var password = values[1];

        var connectionOptions = _paymentSettings.Gates[TransactionProvider.VietQR].PlatformConnection;

        if (username == connectionOptions.Username
            && password == connectionOptions.Password)
        {
            int expiresIn = 300;
            var (token, _) = _accountBusiness.GenerateAccessTokenForPaymentGate(TimeSpan.FromSeconds(expiresIn), "VietQr");

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = expiresIn
            });
        }
        else
        {
            return Unauthorized("Invalid credentials");
        }
    }

    [HttpPost("vqr/ipn/bank/api/transaction-sync")]
    public async Task<IActionResult> VietQrIPN([FromBody] TransactionCallback transactionCallback)
    {
        try
        {
            var res = await _transactionBusiness.HandleVietQrCallbackAsync(transactionCallback);
            string refTransactionId = res.Data.ToString();

            return Ok(new SuccessResponse
            {
                Error = false,
                ErrorReason = null,
                ToastMessage = "Transaction processed successfully",
                Object = new TransactionResponseObject
                {
                    RefTransactionId = refTransactionId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(400, new ErrorResponse
            {
                Error = true,
                ErrorReason = "TRANSACTION_FAILED",
                ToastMessage = ex.Message,
                Object = null
            });
        }
    }

    #endregion VietQR IPN

    #region Paypal IPN

    [AllowAnonymous]
    [HttpPost("paypal/webhook")]
    public async Task<IActionResult> PaypalWebhook()
    {
        var (verify, data) = await _payPalService.VerifyHookRequestAsync(Request);

        if (!verify)
        {
            return Unauthorized("Webhook signature invalid");
        }

        await _transactionBusiness.HandlePaypalCallbackAsync(data);

        return Ok();
    }

    #endregion Paypal IPN
}