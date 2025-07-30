using System;


public static class DateTimeExtensions
{
    public static string PrettyFormatTimeSpan(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan:%d}d {timeSpan:%h}h {timeSpan:%m}m";
        else if (timeSpan.TotalHours >= 1)
            return $"{timeSpan:%h}h {timeSpan:%m}m {timeSpan:%s}s";
        else if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan:%m}m {timeSpan:%s}s";
        else
            return $"{timeSpan:%s}s";
    }

    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
}
