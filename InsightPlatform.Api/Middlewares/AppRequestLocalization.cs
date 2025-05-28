using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Linq;

public class AppRequestLocalization
{
    private readonly LocalizationSettings _localizationOptions;

    public AppRequestLocalization(IOptions<LocalizationSettings> options)
    {
        _localizationOptions = options.Value;
    }

    public RequestLocalizationOptions GetRequestLocalizationOptions()
    {
        var supportedCultures = _localizationOptions.AcceptAllLocales ? 
                                CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => c.Name.IsPresent()).ToList() : 
                                _localizationOptions.SupportedCultures.Select(c => new CultureInfo(c)).ToList();

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
