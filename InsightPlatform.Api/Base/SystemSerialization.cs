using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class SystemSerialization
{
    public static List<JsonConverter> JsonConverters =
    [
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper),
        new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseUpper),
        new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower),
    ];
}
