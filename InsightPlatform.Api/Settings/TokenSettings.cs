public class TokenSettings
{
    public const string Path = "Token";

    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpSeconds { get; set; }
    public int AccessTokenRememberExpSeconds { get; set; }
}