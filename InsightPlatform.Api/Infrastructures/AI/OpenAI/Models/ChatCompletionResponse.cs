using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; set; }
}

public class ChatCompletionChoice
{
    [JsonPropertyName("message")]
    public ChatCompletionMessage Message { get; set; }
}

public class ChatCompletionFailedResponse
{
    [JsonPropertyName("error")]
    public ChatCompletionFailedResponseError Error { get; set; }
}

public class ChatCompletionFailedResponseError
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

     [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }
}

public class ChatCompletionMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

