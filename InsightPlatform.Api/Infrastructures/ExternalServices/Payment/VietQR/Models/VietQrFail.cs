

using System.Text.Json.Serialization;

public class VietQrFail
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}