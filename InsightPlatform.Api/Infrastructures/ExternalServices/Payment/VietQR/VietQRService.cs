using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public interface IVietQRService
{
    Task<VietQrTokenResponse> GenerateTokenAsync();
    Task<VietQrPaymentResponse> NewPaymentAsync(VietQrTokenResponse token, VietQrPaymentRequest detail);
}

public class VietQRService(IOptions<PaymentOptions> settings
    , IHttpClientService httpClientService) : IVietQRService
{
    private readonly GateConnectionOptions _settings = settings.Value.Gates[TransactionProvider.VietQR].GateConnection;
    private readonly IHttpClientService _httpClientService = httpClientService;

    public async Task<VietQrTokenResponse> GenerateTokenAsync()
    {
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.Password}"));
        var url = _settings.BaseUrl.WithPath(_settings.TokenPath);
       
        var (passed, failed, status) = await _httpClientService.PostAsync<VietQrTokenResponse, VietQrFail>(url, null, new Dictionary<string, string>
        {
            { "Authorization", $"Basic {basicToken}" }
        });

        if (status != System.Net.HttpStatusCode.OK || failed != null || passed == null)
            throw new BusinessException($"FailedToGenerateToken{(failed?.Status.IsPresent() == true ? $"_{failed.Status}" : string.Empty)}", 
                                        failed?.Message ?? "Failed to generate VietQR token");

        return passed;
    }

    public async Task<VietQrPaymentResponse> NewPaymentAsync(VietQrTokenResponse token, VietQrPaymentRequest detail)
    {
        var url = _settings.BaseUrl.WithPath(_settings.NewOrderPath);
        var (passed, failed, status) = await _httpClientService.PostAsync<VietQrPaymentResponse, VietQrFail>(url, detail, new Dictionary<string, string>
        {
            { "Authorization", $"{token.TokenType} {token.AccessToken}" }
        });

        if (status != System.Net.HttpStatusCode.OK || failed != null || passed == null)
            throw new BusinessException($"FailedToGenerateNewPayment{(failed?.Status.IsPresent() == true ? $"_{failed.Status}" : string.Empty)}",
                                        failed?.Message ?? "Failed to generate new payment VietQR");

        return passed;
    }
}