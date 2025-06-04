
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class LuckyNumberCrawledDto
{
    [JsonPropertyName("CrawlUrl")]
    public string CrawlUrl { get; set; }

    [JsonPropertyName("ĐB")]
    public List<string> ĐB { get; set; } = [];

    [JsonPropertyName("G1")]
    public List<string> G1 { get; set; } = [];

    [JsonPropertyName("G2")]
    public List<string> G2 { get; set; } = [];

    [JsonPropertyName("G3")]
    public List<string> G3 { get; set; } = [];

    [JsonPropertyName("G4")]
    public List<string> G4 { get; set; } = [];

    [JsonPropertyName("G5")]
    public List<string> G5 { get; set; } = [];

    [JsonPropertyName("G6")]
    public List<string> G6 { get; set; } = [];

    [JsonPropertyName("G7")]
    public List<string> G7 { get; set; } = [];

    [JsonPropertyName("G8")]
    public List<string> G8 { get; set; } = [];
}