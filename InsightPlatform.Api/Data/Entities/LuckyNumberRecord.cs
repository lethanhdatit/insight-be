using System;

public class LuckyNumberRecord
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public DateOnly Date { get; set; }

    public string Detail { get; set; }

    public string CrawlUrl { get; set; }

    public DateTime? CrossCheckedTs { get; set; }

    public Guid? CrossCheckedProviderId { get; set; }

    public DateTime CreatedTs { get; set; }

    public LuckyNumberProvider Provider { get; set; }
}