using System;

public static class NumberExtensions
{
    public static bool IsEven<T>(this T number) where T : struct, IConvertible
    {
        long value = Convert.ToInt64(number);
        return (value & 1) == 0;
    }
}