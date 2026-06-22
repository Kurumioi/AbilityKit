using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AbilityKit.Server.Analyzers;

internal static class GeneratorSyntaxReader
{
    public static string? GetStringLiteralValue(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
    }

    public static string GetStringLiteralValueOrExpression(ExpressionSyntax expression)
    {
        return GetStringLiteralValue(expression) ?? expression.ToString();
    }

    public static bool IsInvocationNamed(InvocationExpressionSyntax invocation, string methodName)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, methodName, StringComparison.Ordinal);
    }

    public static string? GetMemberInvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.ValueText
            : null;
    }
}
