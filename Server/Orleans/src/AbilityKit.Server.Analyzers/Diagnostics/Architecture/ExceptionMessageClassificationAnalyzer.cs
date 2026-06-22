using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AbilityKit.Server.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExceptionMessageClassificationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        ServerAnalyzerDescriptors.AvoidExceptionMessageClassification);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!ServerSourceFileFilter.IsProductionServerSourceFile(invocation.SyntaxTree.FilePath))
        {
            return;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var memberName = memberAccess.Name.Identifier.ValueText;
        if (memberName != "Contains" && memberName != "StartsWith" && memberName != "EndsWith" && memberName != "Equals")
        {
            return;
        }

        var receiver = memberAccess.Expression.ToString();
        if (!receiver.EndsWith(".Message", StringComparison.Ordinal) && receiver != "Message")
        {
            return;
        }

        var firstArgument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (firstArgument?.Expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return;
        }

        var value = literal.Token.ValueText;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            ServerAnalyzerDescriptors.AvoidExceptionMessageClassification,
            literal.GetLocation(),
            value));
    }

}
