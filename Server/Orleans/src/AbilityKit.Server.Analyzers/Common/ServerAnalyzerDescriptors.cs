using Microsoft.CodeAnalysis;

namespace AbilityKit.Server.Analyzers;

internal static class ServerAnalyzerDescriptors
{
    public static readonly DiagnosticDescriptor AvoidHardcodedGrainKey = new(
        id: "AKS0001",
        title: "Avoid hardcoded Orleans grain keys",
        messageFormat: "Use a shared constant instead of hardcoded grain key '{0}'",
        category: AnalyzerCategories.Maintainability,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Server grain keys such as global/default should be centralized to avoid protocol drift and hidden coupling.");

    public static readonly DiagnosticDescriptor AvoidServerMagicString = new(
        id: "AKS0002",
        title: "Avoid hardcoded server gameplay strings",
        messageFormat: "Use a shared constant or options value instead of hardcoded server string '{0}'",
        category: AnalyzerCategories.Maintainability,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Gameplay ids, room tag keys, sync template ids, and gateway route defaults should be centralized.");

    public static readonly DiagnosticDescriptor AvoidExceptionMessageClassification = new(
        id: "AKS0101",
        title: "Avoid classifying business errors by exception message text",
        messageFormat: "Classify business errors with typed exceptions or error codes instead of message text '{0}'",
        category: AnalyzerCategories.Architecture,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Business error mapping should not depend on exception message substrings because text changes can silently break gateway behavior.");

    public static readonly DiagnosticDescriptor EnforceServerProjectBoundary = new(
        id: "AKS0102",
        title: "Server project boundary violation",
        messageFormat: "Project '{0}' must not reference '{1}'",
        category: AnalyzerCategories.Architecture,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Contracts must remain independent from gateway/grain implementation assemblies, and shared layers should not depend on application hosts.");
}
