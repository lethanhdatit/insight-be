using System;
using System.Text.RegularExpressions;

public class TheologyBase
{
    internal static string Normalize(string input)
    {
        if (input.IsMissing()) return "null";
        return Regex.Replace(input.ToLowerInvariant(), @"[\s\W_]+", string.Empty);
    }

    internal static string Normalize(DateOnly? input)
    {
        if (input == null) return "null";

        var dateOnly = input.Value.ToString("yyyyMMdd");

        return dateOnly;
    }

    internal static string Normalize(DateTime? input, string format)
    {
        if (input == null) return "null";

        var dateOnly = input.Value.ToString(format);

        return dateOnly;
    }
}