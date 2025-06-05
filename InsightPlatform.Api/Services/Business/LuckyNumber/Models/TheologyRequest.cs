using System;
using System.Text.Json.Serialization;

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

    [JsonPropertyName("dob")]
    public DateTime? DoB { get; set; }

    [JsonPropertyName("numberLength")]
    public int? NumberLength { get; set; } = 2;
    
    [JsonPropertyName("gender")]
    public Gender? Gender { get; set; }
    
    [JsonPropertyName("location")]
    public string Location { get; set; }    
    
    [JsonPropertyName("religion")]
    public Religion? Religion { get; set; }
}