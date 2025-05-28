public class ExternalLoginSettings
{
    public const string Path = "ExternalLogin";

    public ExternalProviderCredential Google { get; set; }

    public ExternalProviderCredential Facebook { get; set; }
}

public class ExternalProviderCredential
{
    public string ClientId { get; set; }
}