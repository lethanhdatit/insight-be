using System;

public static class DelegateExtensions
{
    public static string GetName(this Delegate del)
    {
        if (del == null)
            return "<null>";

        if (del.Target != null)
            return $"{del.Target.GetType().FullName}.{del.Method.Name}";

        return del.Method.Name;
    }
}
