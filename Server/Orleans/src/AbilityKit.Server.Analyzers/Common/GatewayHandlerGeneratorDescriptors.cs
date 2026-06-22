using Microsoft.CodeAnalysis;

namespace AbilityKit.Server.Analyzers;

internal static class GatewayHandlerGeneratorDescriptors
{
    public static readonly DiagnosticDescriptor DuplicateGatewayHandlerOpCode = new(
        id: "AKS0201",
        title: "Duplicate gateway handler OpCode",
        messageFormat: "Gateway handler OpCode '{0}' is already used by '{1}'",
        category: AnalyzerCategories.Architecture,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each gateway handler must declare a unique OpCode so the generated registry and DI wiring remain deterministic.");

    public static readonly DiagnosticDescriptor MissingGatewayHandlerAttribute = new(
        id: "AKS0202",
        title: "Missing gateway handler attribute",
        messageFormat: "Gateway handler '{0}' must be annotated with GatewayHandlerAttribute",
        category: AnalyzerCategories.Architecture,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Gateway handlers are discovered and generated from GatewayHandlerAttribute metadata.");
}
