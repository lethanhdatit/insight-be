using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class ClassStructureBuilder
{
    public static string BuildAsString(Type type)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<Type>();
        BuildStructureRecursive(type, sb, 0, visited);
        return sb.ToString();
    }

    private static void BuildStructureRecursive(Type type, StringBuilder sb, int indent, HashSet<Type> visited)
    {
        string indentStr = new string(' ', indent * 2);
        sb.AppendLine($"{indentStr}{type.Name}");

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = prop.PropertyType;
            string propTypeName = GetFriendlyTypeName(propType);
            sb.AppendLine($"{indentStr}  └─ {prop.Name}: {propTypeName}");

            if (IsSimpleType(propType))
                continue;

            Type? elementType = GetEnumerableElementType(propType) ?? propType;

            if (visited.Contains(elementType))
                continue; // tránh lặp vô hạn

            visited.Add(elementType);
            BuildStructureRecursive(elementType, sb, indent + 2, visited);
        }
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid);
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments().Select(GetFriendlyTypeName);
            return $"{genericType.Name.Split('`')[0]}<{string.Join(", ", genericArgs)}>";
        }
        return type.Name;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            return type.GetGenericArguments().FirstOrDefault();
        return null;
    }
}
