using System;

public class PainRequest
{
    public Guid PainId { get; set; }
    public string Pain { get; set; } = string.Empty;
    public string Desire { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
