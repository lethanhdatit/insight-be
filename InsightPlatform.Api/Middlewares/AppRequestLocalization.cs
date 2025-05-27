using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class AppRequestLocalization
{
    private readonly LocalizationOptions _localizationOptions;

    public AppRequestLocalization(IOptions<LocalizationOptions> options)
    {
        _localizationOptions = options.Value;
    }

    public RequestLocalizationOptions GetRequestLocalizationOptions()
    {
        var supportedCultures = _localizationOptions.SupportedCultures
            .Select(c => new CultureInfo(c))
            .ToList();

        return new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(_localizationOptions.DefaultCulture),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider { QueryStringKey = "lang" },
                new AcceptLanguageHeaderRequestCultureProvider(),
                new CookieRequestCultureProvider()
            ]
        };
    }
}
