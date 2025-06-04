using System;
using System.Collections.Generic;

public class LuckyNumberRecordByKind
{
    public Guid Id { get; set; }

    public string Kind { get; set; }

    public DateOnly Date { get; set; }

    public string Url { get; set; }

    public List<string> Numbers { get; set; }

    public DateTime CreatedTs { get; set; }
}