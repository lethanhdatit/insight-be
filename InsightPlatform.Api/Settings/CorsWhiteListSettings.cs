using System.Collections.Generic;

public class CorsWhiteListSettings
{
    public const string Path = "CorsWhitelistDomains";
    public const string Policy = "CorsSpecificOrigins";

    public string FeDomain { get; set; }

    public long SystemHttpRequestTimeout { get; set; }
}
