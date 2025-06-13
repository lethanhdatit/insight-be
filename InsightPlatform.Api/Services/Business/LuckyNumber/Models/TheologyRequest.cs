using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class TheologyRequest
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("dreaming")]
    public string Dreaming { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("dob")]
    public DateTime? DoB { get; set; }

    [JsonPropertyName("gender")]
    public Gender? Gender { get; set; }    
    
    [JsonPropertyName("religion")]
    public Religion? Religion { get; set; }

    public void Standardize()
    {
        FirstName = FirstName?.Trim();
        MiddleName = MiddleName?.Trim();
        LastName = LastName?.Trim();
        Dreaming = Dreaming?.Trim();
        Location = Location?.Trim();
    }

    public string InitUniqueKey(TheologyKind kind, string sysPrompt = null, string userPrompt = null)
    {
        return string.Join("|",
             Normalize(FirstName),
             Normalize(MiddleName),
             Normalize(LastName),
             Normalize(DoB != null ? DateOnly.FromDateTime(DoB.Value) : null),
             Normalize(((short?)Gender)?.ToString()),
             Normalize(((short?)Religion)?.ToString()),
             Normalize(((short?)kind)?.ToString()),
             Normalize(sysPrompt),
             Normalize(userPrompt),
             Normalize(Location),
             Normalize(Dreaming)
         ).ComputeSha256Hash();
    }

    private static string Normalize(string input)
    {
        if (input.IsMissing()) return "null";
        return Regex.Replace(input.ToLowerInvariant(), @"[\s\W_]+", string.Empty);
    }

    private static string Normalize(DateOnly? input)
    {
        if (input == null) return "null";

        var dateOnly = input.Value.ToString("yyyyMMdd");

        return dateOnly;
    }
}