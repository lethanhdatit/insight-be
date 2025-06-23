using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TheologyDto
{
    [JsonPropertyName("numbers")]
    public List<string> Numbers { get; set; }

    [JsonPropertyName("explanation")]
    public Explanation Explanation { get; set; }
}

public class Explanation
{
    [JsonPropertyName("detail")]
    public List<ParagraphingItem> Detail { get; set; }

    [JsonPropertyName("warning")]
    public List<ParagraphingItem> Warning { get; set; }

    [JsonPropertyName("advice")]
    public List<ParagraphingItem> Advice { get; set; }

    [JsonPropertyName("summary")]
    public List<ParagraphingItem> Summary { get; set; }
}

public class ParagraphingItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}