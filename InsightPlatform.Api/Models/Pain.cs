using System;

public class Pain
{
    public Guid Id { get; set; }
    public string PainDetail { get; set; }
    public string Desire { get; set; }
    public string Category { get; set; }
    public string Topic { get; set; }
    public string Emotion { get; set; }
    public int? UrgencyLevel { get; set; }
    public string DeviceId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}