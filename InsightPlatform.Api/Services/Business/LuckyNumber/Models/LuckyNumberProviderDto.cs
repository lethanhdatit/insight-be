
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class LuckyNumberProviderDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("homePage")]
    public string HomePage { get; set; }

    [JsonPropertyName("urlPathTemplate")]
    public string UrlPathTemplate { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}