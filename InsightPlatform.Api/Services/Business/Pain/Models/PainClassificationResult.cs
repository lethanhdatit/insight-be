using System.Text.Json.Serialization;

public class PainClassificationResult
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; }

    [JsonPropertyName("emotion")]
    public string Emotion { get; set; }

    [JsonPropertyName("urgencyLevel")]
    public int UrgencyLevel { get; set; }
}