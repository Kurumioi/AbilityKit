using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AbilityKit.Server.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServerProjectBoundaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> ForbiddenNamespaceReferences =
        new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal)
        {
            [ServerAssemblyNames.Contracts] = ImmutableArray.Create(
                ServerAssemblyNames.Grains,
                ServerAssemblyNames.Gateway,
                ServerAssemblyNames.Host,
                ServerAssemblyNames.Hosting),
            [ServerAssemblyNames.Hosting] = ImmutableArray.Create(
                ServerAssemblyNames.Gateway,
                ServerAssemblyNames.Host)
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        ServerAnalyzerDescriptors.EnforceServerProjectBoundary);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var assemblyName = context.Compilation.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return;
        }

        if (!ForbiddenNamespaceReferences.TryGetValue(assemblyName!, out var forbiddenPrefixes))
        {
            return;
        }

        foreach (var reference in context.Compilation.References)
        {
            if (context.Compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol symbol)
            {
                continue;
            }

            var referencedName = symbol.Identity.Name;
            if (string.IsNullOrWhiteSpace(referencedName))
            {
                continue;
            }

            foreach (var forbiddenPrefix in forbiddenPrefixes)
            {
                if (!referencedName!.StartsWith(forbiddenPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    ServerAnalyzerDescriptors.EnforceServerProjectBoundary,
                    Location.None,
                    assemblyName,
                    referencedName));
            }
        }
    }
}
