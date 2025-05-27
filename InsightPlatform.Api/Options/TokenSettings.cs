public class TokenSettings
{
    public const string Path = "TokenSettings";

    public string SecretKey { get; set; }

    public int AccessTokenExpSeconds { get; set; }
}