using System.Collections.Generic;

public class AppSettings
{
    public const string Path = "App";

    public string FeDomain { get; set; }

    public long SystemHttpRequestTimeout { get; set; }
}
