using System;
using System.Collections.Generic;

public class LuckyNumberRecordByKind : Trackable
{
    public Guid Id { get; set; }

    public string Kind { get; set; }

    public DateOnly Date { get; set; }

    public string Url { get; set; }

    public List<string> Numbers { get; set; }
}