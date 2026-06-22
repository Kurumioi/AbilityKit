using Microsoft.CodeAnalysis;

namespace AbilityKit.Server.Analyzers;

internal static class GeneratorAssemblyTargets
{
    public static bool IsTargetAssembly(Compilation compilation, string assemblyName)
    {
        return string.Equals(compilation.AssemblyName, assemblyName, StringComparison.Ordinal);
    }
}
