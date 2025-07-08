using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public interface IPayPalService
{
    Task<string> GenerateTokenAsync();

    Task<string> CreateOrderAsync(
        decimal amount,
        string returnUrl,
        string cancelUrl,
        string orderId,
        string description = null,
        string invoiceId = null,
        string noteToPayer = null
);

    Task<JsonElement> CaptureOrderAsync(string orderId);

    Task<(bool verify, PayPalWebhookEvent data)> VerifyHookRequestAsync(HttpRequest request);
}

public class PayPalService : IPayPalService
{
    private readonly IHttpClientService _httpClientService;
    private readonly PaypalOptions _config;
    private static readonly string LiveBaseUrl = "https://api-m.paypal.com";
    private static readonly string SandboxBaseUrl = "https://api-m.sandbox.paypal.com";

    public PayPalService(IOptions<PaymentGateOptions> settings, IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
        _config = settings.Value.Paypal;
    }

    private string BaseUrl => _config.UseSandbox ? SandboxBaseUrl : LiveBaseUrl;

    public async Task<string> GenerateTokenAsync()
    {
        var clientId = _config.ClientId;
        var secret = _config.Secret;

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Basic {authHeader}" },
            { "Content-Type", "application/x-www-form-urlencoded" }
        };

        var content = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials")
        };

        var response = await _httpClientService.PostAsync<JsonElement>(
            $"{BaseUrl}/v1/oauth2/token",
            new FormUrlEncodedContent(content),
            headers
        );

        return response.GetProperty("access_token").GetString()!;
    }

    public async Task<string> CreateOrderAsync(
        decimal amountUSD,
        string returnUrl,
        string cancelUrl,
        string orderId,
        string description = null,
        string invoiceId = null,
        string noteToPayer = null
)
    {
        var accessToken = await GenerateTokenAsync();

        var headers = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer {accessToken}" },
        { "Content-Type", "application/json" }
    };

        var body = new
        {
            intent = "CAPTURE",
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl,
                user_action = "PAY_NOW",
                note_to_payer = noteToPayer
            },
            purchase_units = new[]
            {
            new
            {
                reference_id = orderId,
                description = description,
                custom_id = orderId,
                invoice_id = invoiceId ?? orderId,
                amount = new
                {
                    currency_code = "USD",
                    value = amountUSD.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        }
        };

        var response = await _httpClientService.PostAsync<JsonElement>(
            $"{BaseUrl}/v2/checkout/orders",
            body,
            headers
        );

        var approveLink = response.GetProperty("links")
            .EnumerateArray()
            .FirstOrDefault(l => l.GetProperty("rel").GetString() == "approve")
            .GetProperty("href")
            .GetString();

        return approveLink;
    }

    public async Task<JsonElement> CaptureOrderAsync(string orderId)
    {
        var accessToken = await GenerateTokenAsync();

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" },
            { "Content-Type", "application/json" }
        };

        var body = new { };

        var response = await _httpClientService.PostAsync<JsonElement>(
            $"{BaseUrl}/v2/checkout/orders/{orderId}/capture",
            body,
            headers
        );

        return response;
    }

    public async Task<(bool verify, PayPalWebhookEvent data)> VerifyHookRequestAsync(HttpRequest request)
    {
        var webhookId = _config.WebhookId;

        var body = await new StreamReader(request.Body).ReadToEndAsync();

        // PayPal headers
        var transmissionId = request.Headers["PayPal-Transmission-Id"].ToString();
        var transmissionTime = request.Headers["PayPal-Transmission-Time"].ToString();
        var certUrl = request.Headers["PayPal-Cert-Url"].ToString();
        var authAlgo = request.Headers["PayPal-Auth-Algo"].ToString();
        var transmissionSig = request.Headers["PayPal-Transmission-Sig"].ToString();

        // Get access token
        var accessToken = await GenerateTokenAsync();

        // Verify signature
        var verificationRequest = new
        {
            auth_algo = authAlgo,
            cert_url = certUrl,
            transmission_id = transmissionId,
            transmission_sig = transmissionSig,
            transmission_time = transmissionTime,
            webhook_id = webhookId,
            webhook_event = JsonSerializer.Deserialize<JsonElement>(body)
        };

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" },
            { "Content-Type", "application/json" }
        };

        var jsonDoc = await _httpClientService.PostAsync<JsonDocument>(
           $"{BaseUrl}/v1/notifications/verify-webhook-signature",
           verificationRequest,
           headers
       );

        var verificationStatus = jsonDoc.RootElement.GetProperty("verification_status").GetString();

        return (verificationStatus?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false, JsonSerializer.Deserialize<PayPalWebhookEvent>(body));
    }
}
