using System.Collections.Generic;

public class CorsWhiteListSettings
{
    public const string Path = "CorsWhitelistDomains";
    public const string Policy = "CorsSpecificOrigins";

    public string Key { get; set; }
    public List<string> Origins { get; set; }
}