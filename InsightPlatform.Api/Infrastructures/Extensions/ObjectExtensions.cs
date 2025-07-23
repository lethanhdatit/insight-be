using System.Text.Json;

public static class ObjectExtensions
{
    public static T Clone<T>(this T input)
    {
        if (input == null)
            return default!;

        var json = JsonSerializer.Serialize(input);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}