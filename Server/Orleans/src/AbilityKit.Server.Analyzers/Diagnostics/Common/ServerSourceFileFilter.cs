namespace AbilityKit.Server.Analyzers;

internal static class ServerSourceFileFilter
{
    public static bool IsProductionServerSourceFile(string? filePath)
    {
        return IsServerSourceFile(filePath) && !IsTestOrBuildOutputSourceFile(filePath);
    }

    public static bool IsServerSourceFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var normalized = NormalizePath(filePath!);
        return normalized.Contains("/Server/Orleans/src/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTestOrBuildOutputSourceFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var normalized = NormalizePath(filePath!);
        return normalized.Contains(".Tests/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string filePath)
    {
        return filePath.Replace('\\', '/');
    }
}
