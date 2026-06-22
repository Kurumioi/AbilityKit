namespace AbilityKit.Server.Analyzers;

internal static class CSharpLiteral
{
    public static string String(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static string NullableString(string? value)
    {
        return value is null ? "null" : String(value);
    }

    public static string NullableStringOrNullExpression(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.Ordinal)
            ? "null"
            : String(value!);
    }
}
