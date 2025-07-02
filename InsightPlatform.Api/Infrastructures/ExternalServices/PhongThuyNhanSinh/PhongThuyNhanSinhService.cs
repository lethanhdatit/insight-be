using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public interface IPhongThuyNhanSinhService
{
    Task<List<LaSoBatTuResponse>> BuildLaSoBatTuAsync(string name
        , Gender gender
        , DateTime solarBirthDateTime
        , DateTime lunarBirthDateTime);
}

public class PhongThuyNhanSinhService(IOptions<ExternalResourceSettings> settings
    , IHttpClientService httpClientService) : IPhongThuyNhanSinhService
{
    private readonly ExternalResourceSettings _settings = settings.Value;
    private readonly IHttpClientService _httpClientService = httpClientService;

    public async Task<List<LaSoBatTuResponse>> BuildLaSoBatTuAsync(string name
        , Gender gender
        , DateTime solarBirthDateTime
        , DateTime lunarBirthDateTime)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("pxg_geofesh_call"), "action" },
            { new StringContent(name.IsPresent() ? name : string.Empty), "n" },
            { new StringContent(gender.ToString().ToLower()), "s" },
            { new StringContent(solarBirthDateTime.Day.ToString()), "d" },
            { new StringContent(solarBirthDateTime.Month.ToString()), "m" },
            { new StringContent(solarBirthDateTime.Year.ToString()), "y" },
            { new StringContent(lunarBirthDateTime.Year.ToString()), "ym" },
            { new StringContent(solarBirthDateTime.Hour.ToString()), "h" },
            { new StringContent(solarBirthDateTime.Minute.ToString()), "mi" }
        };

        var option = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return await _httpClientService.PostAsync<List<LaSoBatTuResponse>>(_settings.PhongThuyNhanSinhUrl, form, options: option);
    }
}