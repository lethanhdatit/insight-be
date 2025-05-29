public class QueueMessagingSettings
{
    public const string Path = "QueueMessaging";
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }

    public QueueRetrySettings Retry { get; set; }
}

public class QueueRetrySettings
{
    public int MaxRetryCount { get; set; }
    public int MinDelayMs { get; set; }
    public int MaxDelayMs { get; set; }
}
