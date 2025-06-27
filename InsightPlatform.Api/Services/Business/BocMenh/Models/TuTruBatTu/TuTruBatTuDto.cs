using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TuTruBatTuDto
{
    [JsonPropertyName("original")]
    public string Original { get; set; }

    [JsonPropertyName("metaData")]
    public List<MetadataEntry> MetaData { get; set; }
}