using System;
using System.Collections.Generic;

public class LuckyNumberProvider
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string HomePage { get; set; }

    public string UrlPathTemplate { get; set; }

    public string Description { get; set; }

    public DateTime CreatedTs { get; set; }

    public ICollection<LuckyNumberRecord> Records { get; set; }
}