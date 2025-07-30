public class EmailProviderSettings
{
    public const string Path = "EmailProvider";

    public string SmtpServer { get; set; }

    public int SmtpPort { get; set; }

    public string FromAddress { get; set; }

    public string SmtpUsername { get; set; }

    public string SmtpPassword { get; set; }
}
