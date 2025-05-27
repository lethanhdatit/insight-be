public class ExternalLoginSettings
{
    public const string Path = "ExternalLoginSettings";

    public ExternalProviderCredential Google { get; set; }

    public ExternalProviderCredential Facebook { get; set; }
}

public class ExternalProviderCredential
{
    public string ClientId { get; set; }
}