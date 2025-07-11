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

    Task<(string approveLink, decimal feeAmount, decimal vatAmount, decimal finalAmount, string note)> CreateOrderAsync(
        decimal total,                         // Giá gốc chưa giảm, chưa VAT, chưa phí
        decimal subtotal,                      // Giá sau giảm, chưa VAT, chưa phí
        string returnUrl,
        string cancelUrl,
        string orderId,
        string brandName,
        string locale = "vi-VN",
        string description = null,
        string invoiceId = null,
        string noteToPayer = null,
        decimal? feeRate = null,              // e.g. 0.044m = 4.4%
        bool buyerPaysFee = false,
        bool includeVAT = false,
        decimal? VATaxRate = null             // e.g. 0.1m = 10%
);

    Task<bool> CaptureOrderAsync(string orderId);

    Task<(bool verify, PayPalWebhookEvent data)> VerifyHookRequestAsync(HttpRequest request);
}

public class PayPalService(IOptions<PaymentOptions> settings, IHttpClientService httpClientService) : IPayPalService
{
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly GateConnectionOptions _config = settings.Value.Gates[TransactionProvider.Paypal].GateConnection;
    private static readonly string LiveBaseUrl = "https://api-m.paypal.com";
    private static readonly string SandboxBaseUrl = "https://api-m.sandbox.paypal.com";

    private string BaseUrl => _config.UseSandbox ? SandboxBaseUrl : LiveBaseUrl;

    public async Task<string> GenerateTokenAsync()
    {
        var clientId = _config.Username;
        var secret = _config.Password;

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

    public async Task<(string approveLink, decimal feeAmount, decimal vatAmount, decimal finalAmount, string note)> CreateOrderAsync(
        decimal total,                         // Giá gốc chưa giảm, chưa VAT, chưa phí
        decimal subtotal,                      // Giá sau giảm, chưa VAT, chưa phí
        string returnUrl,
        string cancelUrl,
        string orderId,
        string brandName,
        string locale = "vi-VN",
        string description = null,
        string invoiceId = null,
        string noteToPayer = null,
        decimal? feeRate = null,              // e.g. 0.044m = 4.4%
        bool buyerPaysFee = false,
        bool includeVAT = false,
        decimal? VATaxRate = null             // e.g. 0.1m = 10%
)
    {
        buyerPaysFee = buyerPaysFee && feeRate.HasValue;
        includeVAT = includeVAT && VATaxRate.HasValue;

        PaymentUtils.CalculateFeeAndTaxV1(total
            , subtotal
            , feeRate
            , buyerPaysFee
            , includeVAT
            , VATaxRate
            , orderId
            , description
            , out decimal feeAmount, out decimal discount, out decimal vatAmount, out decimal finalAmount, out string effectiveDescription);

        total = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, total);
        subtotal = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, subtotal);
        vatAmount = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, vatAmount);
        feeAmount = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, feeAmount);
        discount = PaymentUtils.RoundAmountByGateProvider(TransactionProvider.Paypal, discount);
        finalAmount = total + vatAmount + feeAmount - discount;

        var body = new
        {
            intent = "CAPTURE",
            application_context = new
            {
                brand_name = brandName,
                locale = locale,
                landing_page = "LOGIN",
                shipping_preference = "NO_SHIPPING",
                user_action = "PAY_NOW",
                return_url = returnUrl,
                cancel_url = cancelUrl,
                note_to_payer = noteToPayer
            },
            purchase_units = new[]
            {
                new
                {
                    reference_id = orderId,
                    description = effectiveDescription,
                    custom_id = orderId,
                    invoice_id = invoiceId ?? $"{orderId}-{(buyerPaysFee ? "FEE" : "NOFEE")}",
                    amount = new
                    {
                        currency_code = "USD",
                        // item_total + tax_total + shipping + handling + insurance - shipping_discount - discount
                        value = finalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        breakdown = new
                        {
                            item_total = new
                            {
                                currency_code = "USD",
                                value = total.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            tax_total = new
                            {
                                currency_code = "USD",
                                value = vatAmount.ToString("F2", CultureInfo.InvariantCulture)
                            },                        
                            handling = new
                            {
                                currency_code = "USD",
                                value = feeAmount.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            discount = new
                            {
                                currency_code = "USD",
                                value = discount.ToString("F2", CultureInfo.InvariantCulture)
                            }
                        }
                    }
                }
            }
        };

        var accessToken = await GenerateTokenAsync();

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" },
            { "Content-Type", "application/json" }
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
            .ToString();

        return (approveLink, feeAmount, vatAmount, finalAmount, effectiveDescription);
    }    

    public async Task<bool> CaptureOrderAsync(string orderId)
    {
        var accessToken = await GenerateTokenAsync();

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" },
            { "Content-Type", "application/json" }
        };

        var response = await _httpClientService.PostAsync<JsonElement>(
            $"{BaseUrl}/v2/checkout/orders/{orderId}/capture",
            new { }, headers);

        var status = response.GetProperty("status").GetString();

        return status == "COMPLETED";
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
