using System;

public static class NumberExtensions
{
    public static bool IsEven<T>(this T number) where T : struct, IConvertible
    {
        long value = Convert.ToInt64(number);
        return (value & 1) == 0;
    }

    public static string ToPercent(this decimal value, int maxZero = 2)
    {
        var scaled = value * 100;
        string format = "0." + new string('#', maxZero);
        return scaled.ToString(format) + "%";
    }
}