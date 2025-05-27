using System.Collections.Generic;

public class LocalizationOptions
{
    public const string Path = "Localization";
    public string DefaultCulture { get; set; } = "en-US";
    public List<string> SupportedCultures { get; set; } = [];
}
