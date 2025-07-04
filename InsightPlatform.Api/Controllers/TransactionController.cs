using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionController(IWebHostEnvironment env
        , ILogger<TransactionController> logger
        , ITransactionBusiness transactionBusiness
        , IAccountBusiness accountBusiness
        , IOptions<PaymentGateOptions> paymentSettings
    ) : BaseController(env, logger)
{
    private readonly PaymentGateOptions _paymentSettings = paymentSettings.Value;
    private readonly ITransactionBusiness _transactionBusiness = transactionBusiness;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;

    [HttpGet("topups/me")]
    public async Task<IActionResult> GetUserFates()
    {
        var res = await _transactionBusiness.GetUserFates();
        return HandleOk(res);
    }

    [AllowAnonymous]
    [HttpGet("topups")]
    public async Task<IActionResult> Topups()
    {
        var res = await _transactionBusiness.GetTopupsAsync();
        return HandleOk(res);
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

        if (username == _paymentSettings.VietQR.PlatformConnection.Username
            && password == _paymentSettings.VietQR.PlatformConnection.Password)
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
            var res = await _transactionBusiness.VietQrCallbackAsync(transactionCallback);
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
}