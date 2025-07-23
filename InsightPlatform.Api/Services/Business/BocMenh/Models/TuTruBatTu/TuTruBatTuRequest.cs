using System;
using System.Text.Json.Serialization;

public class TuTruBatTuRequest : TheologyBase
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("birthDateTime")]
    public DateTime BirthDateTime { get; set; }

    [JsonPropertyName("gender")]
    public Gender Gender { get; set; }

    [JsonPropertyName("category")]
    public TuTruBatTuCategory Category { get; set; }

    public void Standardize()
    {
        Name = Name?.Trim();
    }

    public string InitUniqueKey(TheologyKind kind, string sysPrompt = null, string userPrompt = null, string combinedPrompts = null)
    {
        return string.Join("|",
             Normalize(Name),
             Normalize(BirthDateTime, "dd/MM/yyyy HH:mm"),
             Normalize(((short?)Gender)?.ToString()),
             Normalize(((short?)Category)?.ToString()),
             Normalize(((short?)kind)?.ToString()),
             Normalize(sysPrompt),
             Normalize(userPrompt),
             Normalize(combinedPrompts)
         ).ComputeSha256Hash();
    }
}