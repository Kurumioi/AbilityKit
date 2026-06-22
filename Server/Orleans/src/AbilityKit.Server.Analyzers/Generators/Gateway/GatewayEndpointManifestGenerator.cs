using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AbilityKit.Server.Analyzers;

[Generator(LanguageNames.CSharp)]
public sealed class GatewayEndpointManifestGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax invocation && IsEndpointMapInvocation(invocation),
                static (context, _) => CreateEndpointCandidate((InvocationExpressionSyntax)context.Node))
            .Where(static endpoint => endpoint is not null)
            .Select(static (endpoint, _) => endpoint!)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(endpoints), static (context, source) =>
        {
            if (!GeneratorAssemblyTargets.IsTargetAssembly(source.Left, ServerAssemblyNames.Gateway))
            {
                return;
            }

            var ordered = source.Right
                .Distinct(EndpointCandidateComparer.Instance)
                .OrderBy(static endpoint => endpoint.Path, StringComparer.Ordinal)
                .ThenBy(static endpoint => endpoint.HttpMethod, StringComparer.Ordinal)
                .ToImmutableArray();

            context.AddSource("GeneratedGatewayEndpointManifest.g.cs", SourceText.From(Render(ordered), Encoding.UTF8));
        });
    }

    private static bool IsEndpointMapInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && IsEndpointMapMethod(memberAccess.Name.Identifier.ValueText)
            && invocation.ArgumentList.Arguments.Count > 0
            && invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression);
    }

    private static bool IsEndpointMapMethod(string methodName)
    {
        return methodName == "MapGet"
            || methodName == "MapPost"
            || methodName == "MapPut"
            || methodName == "MapDelete"
            || methodName == "MapPatch";
    }

    private static EndpointCandidate? CreateEndpointCandidate(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        var route = GeneratorSyntaxReader.GetStringLiteralValue(invocation.ArgumentList.Arguments[0].Expression) ?? string.Empty;
        var prefix = ResolveRoutePrefix(invocation, memberAccess.Expression);
        var expressionStatement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        var chainRoot = expressionStatement?.Expression ?? invocation;
        var name = ResolveStringArgument(chainRoot, "WithName");
        var requestType = ResolveAcceptsType(chainRoot);
        var statusCodes = ResolveProducedStatusCodes(chainRoot);

        return new EndpointCandidate(
            NormalizeHttpMethod(memberAccess.Name.Identifier.ValueText),
            CombinePath(prefix, route),
            name,
            requestType,
            statusCodes);
    }

    private static string ResolveRoutePrefix(SyntaxNode node, ExpressionSyntax receiver)
    {
        if (receiver is not IdentifierNameSyntax identifier)
        {
            return string.Empty;
        }

        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
        {
            return string.Empty;
        }

        foreach (var variable in method.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (!string.Equals(variable.Identifier.ValueText, identifier.Identifier.ValueText, StringComparison.Ordinal))
            {
                continue;
            }

            var initializer = variable.Initializer?.Value;
            if (initializer is null)
            {
                continue;
            }

            foreach (var invocation in initializer.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (GeneratorSyntaxReader.IsInvocationNamed(invocation, "MapGroup")
                    && invocation.ArgumentList.Arguments.Count > 0
                    && GeneratorSyntaxReader.GetStringLiteralValue(invocation.ArgumentList.Arguments[0].Expression) is { } routePrefix)
                {
                    return routePrefix;
                }
            }
        }

        return string.Empty;
    }

    private static string? ResolveStringArgument(SyntaxNode chainRoot, string methodName)
    {
        foreach (var invocation in chainRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (GeneratorSyntaxReader.IsInvocationNamed(invocation, methodName)
                && invocation.ArgumentList.Arguments.Count > 0
                && GeneratorSyntaxReader.GetStringLiteralValue(invocation.ArgumentList.Arguments[0].Expression) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ResolveAcceptsType(SyntaxNode chainRoot)
    {
        foreach (var invocation in chainRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName }
                && genericName.Identifier.ValueText == "Accepts"
                && genericName.TypeArgumentList.Arguments.Count > 0)
            {
                return genericName.TypeArgumentList.Arguments[0].ToString();
            }
        }

        return null;
    }

    private static ImmutableArray<string> ResolveProducedStatusCodes(SyntaxNode chainRoot)
    {
        var statusCodes = ImmutableArray.CreateBuilder<string>();
        foreach (var invocation in chainRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
                || memberAccess.Name.Identifier.ValueText != "Produces"
                || invocation.ArgumentList.Arguments.Count == 0)
            {
                continue;
            }

            statusCodes.Add(invocation.ArgumentList.Arguments[0].Expression.ToString());
        }

        return statusCodes.ToImmutable();
    }

    private static string NormalizeHttpMethod(string methodName)
    {
        return methodName.StartsWith("Map", StringComparison.Ordinal)
            ? methodName.Substring(3).ToUpperInvariant()
            : methodName.ToUpperInvariant();
    }

    private static string CombinePath(string prefix, string route)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.IsNullOrWhiteSpace(route) ? "/" : route;
        }

        if (string.IsNullOrWhiteSpace(route) || route == "/")
        {
            return prefix;
        }

        return prefix.TrimEnd('/') + "/" + route.TrimStart('/');
    }

    private static string Render(ImmutableArray<EndpointCandidate> endpoints)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("namespace AbilityKit.Orleans.Gateway.HttpApi;");
        builder.AppendLine();
        builder.AppendLine("internal sealed record GeneratedGatewayEndpointManifestEntry(");
        builder.AppendLine("    string HttpMethod,");
        builder.AppendLine("    string Path,");
        builder.AppendLine("    string? Name,");
        builder.AppendLine("    string? RequestType,");
        builder.AppendLine("    IReadOnlyList<string> ProducedStatusCodes);");
        builder.AppendLine();
        builder.AppendLine("internal static class GeneratedGatewayEndpointManifest");
        builder.AppendLine("{");
        builder.AppendLine("    public static IReadOnlyList<GeneratedGatewayEndpointManifestEntry> Entries { get; } = new GeneratedGatewayEndpointManifestEntry[]");
        builder.AppendLine("    {");

        foreach (var endpoint in endpoints)
        {
            builder.Append("        new GeneratedGatewayEndpointManifestEntry(");
            builder.Append(CSharpLiteral.String(endpoint.HttpMethod));
            builder.Append(", ");
            builder.Append(CSharpLiteral.String(endpoint.Path));
            builder.Append(", ");
            builder.Append(CSharpLiteral.NullableString(endpoint.Name));
            builder.Append(", ");
            builder.Append(CSharpLiteral.NullableString(endpoint.RequestType));
            builder.Append(", new[] { ");
            builder.Append(string.Join(", ", endpoint.ProducedStatusCodes.Select(CSharpLiteral.String)));
            builder.AppendLine(" }),");
        }

        builder.AppendLine("    };");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private sealed record EndpointCandidate(
        string HttpMethod,
        string Path,
        string? Name,
        string? RequestType,
        ImmutableArray<string> ProducedStatusCodes);

    private sealed class EndpointCandidateComparer : IEqualityComparer<EndpointCandidate>
    {
        public static EndpointCandidateComparer Instance { get; } = new();

        public bool Equals(EndpointCandidate? x, EndpointCandidate? y)
        {
            return x is not null
                && y is not null
                && string.Equals(x.HttpMethod, y.HttpMethod, StringComparison.Ordinal)
                && string.Equals(x.Path, y.Path, StringComparison.Ordinal)
                && string.Equals(x.Name, y.Name, StringComparison.Ordinal);
        }

        public int GetHashCode(EndpointCandidate obj)
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(obj.HttpMethod);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(obj.Path);
                hash = (hash * 397) ^ (obj.Name is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Name));
                return hash;
            }
        }
    }
}
