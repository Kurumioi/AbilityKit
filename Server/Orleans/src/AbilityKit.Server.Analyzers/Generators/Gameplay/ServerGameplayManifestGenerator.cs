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
public sealed class ServerGameplayManifestGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var descriptorCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is PropertyDeclarationSyntax property && property.Initializer is not null,
                static (context, _) => CreateDescriptorCandidate((PropertyDeclarationSyntax)context.Node))
            .Where(static descriptor => descriptor is not null)
            .Select(static (descriptor, _) => descriptor!)
            .Collect();

        var moduleCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ObjectCreationExpressionSyntax creation && IsServerGameplayModuleCreation(creation),
                static (context, _) => CreateModuleCandidate((ObjectCreationExpressionSyntax)context.Node))
            .Where(static module => module is not null)
            .Select(static (module, _) => module!)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(descriptorCandidates).Combine(moduleCandidates), static (context, source) =>
        {
            var compilation = source.Left.Left;
            if (!GeneratorAssemblyTargets.IsTargetAssembly(compilation, ServerAssemblyNames.Grains))
            {
                return;
            }

            var descriptors = source.Left.Right;
            var modules = source.Right;
            var entries = MergeEntries(descriptors, modules);
            context.AddSource("GeneratedServerGameplayManifest.g.cs", SourceText.From(Render(entries), Encoding.UTF8));
        });
    }

    private static DescriptorCandidate? CreateDescriptorCandidate(PropertyDeclarationSyntax property)
    {
        if (property.Initializer?.Value is not ObjectCreationExpressionSyntax creation
            || creation.ArgumentList is null
            || creation.ArgumentList.Arguments.Count < 7
            || !IsGameplayRoomDescriptorCreation(creation))
        {
            return null;
        }

        return new DescriptorCandidate(
            property.Identifier.ValueText,
            creation.ArgumentList.Arguments[0].Expression.ToString(),
            GeneratorSyntaxReader.GetStringLiteralValueOrExpression(creation.ArgumentList.Arguments[1].Expression),
            creation.ArgumentList.Arguments[2].Expression.ToString(),
            creation.ArgumentList.Arguments[3].Expression.ToString(),
            creation.ArgumentList.Arguments[4].Expression.ToString(),
            creation.ArgumentList.Arguments[5].Expression.ToString(),
            GeneratorSyntaxReader.GetStringLiteralValueOrExpression(creation.ArgumentList.Arguments[6].Expression));
    }

    private static ModuleCandidate? CreateModuleCandidate(ObjectCreationExpressionSyntax creation)
    {
        if (creation.ArgumentList is null || creation.ArgumentList.Arguments.Count < 5)
        {
            return null;
        }

        var descriptorExpression = creation.ArgumentList.Arguments[0].Expression.ToString();
        var syncProfile = creation.ArgumentList.Arguments[1].Expression;
        var roomAdapter = ResolveReturnedCreationType(creation.ArgumentList.Arguments[2].Expression);
        var battleAdapter = ResolveReturnedCreationType(creation.ArgumentList.Arguments[3].Expression);
        var worldBlueprints = ResolveWorldBlueprintTypes(creation.ArgumentList.Arguments[4].Expression);
        var syncTemplates = ResolveSyncTemplates(syncProfile);

        return new ModuleCandidate(
            descriptorExpression,
            syncTemplates.DefaultSyncMode,
            syncTemplates.SupportedSyncTemplateIds,
            roomAdapter,
            battleAdapter,
            worldBlueprints);
    }

    private static bool IsGameplayRoomDescriptorCreation(ObjectCreationExpressionSyntax creation)
    {
        return creation.Type.ToString().EndsWith("GameplayRoomDescriptor", StringComparison.Ordinal);
    }

    private static bool IsServerGameplayModuleCreation(ObjectCreationExpressionSyntax creation)
    {
        return creation.Type.ToString().EndsWith("ServerGameplayModule", StringComparison.Ordinal);
    }

    private static string? ResolveReturnedCreationType(ExpressionSyntax expression)
    {
        var creation = expression.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
        return creation?.Type.ToString();
    }

    private static ImmutableArray<string> ResolveWorldBlueprintTypes(ExpressionSyntax expression)
    {
        return expression.DescendantNodesAndSelf()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(static creation => !creation.Type.ToString().EndsWith("Func<IWorldBlueprint>", StringComparison.Ordinal))
            .Select(static creation => creation.Type.ToString())
            .Where(static type => type.EndsWith("WorldBlueprint", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static SyncTemplateCandidate ResolveSyncTemplates(ExpressionSyntax expression)
    {
        if (expression is not InvocationExpressionSyntax invocation
            || invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || invocation.ArgumentList.Arguments.Count == 0)
        {
            return new SyncTemplateCandidate(null, ImmutableArray<string>.Empty);
        }

        var mode = memberAccess.Name.Identifier.ValueText;
        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var value = GeneratorSyntaxReader.GetStringLiteralValueOrExpression(argument.Expression);
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.Add(value);
            }
        }

        return new SyncTemplateCandidate(mode, builder.ToImmutable());
    }

    private static ImmutableArray<GameplayManifestEntry> MergeEntries(
        ImmutableArray<DescriptorCandidate> descriptors,
        ImmutableArray<ModuleCandidate> modules)
    {
        var descriptorByExpression = descriptors.ToDictionary(
            static descriptor => "ServerGameplayDescriptors." + descriptor.PropertyName,
            static descriptor => descriptor,
            StringComparer.Ordinal);

        var entries = ImmutableArray.CreateBuilder<GameplayManifestEntry>();
        foreach (var module in modules)
        {
            if (!descriptorByExpression.TryGetValue(module.DescriptorExpression, out var descriptor))
            {
                continue;
            }

            entries.Add(new GameplayManifestEntry(
                descriptor.RoomTypeExpression,
                descriptor.DisplayName,
                descriptor.DefaultMaxPlayersExpression,
                descriptor.RequiresPlayerLoadoutExpression,
                descriptor.DefaultWorldTypeExpression,
                descriptor.DefaultTickRateExpression,
                descriptor.DefaultSyncTemplateId,
                module.DefaultSyncMode,
                module.SupportedSyncTemplateIds,
                module.RoomAdapterType,
                module.BattleRuntimeAdapterType,
                module.WorldBlueprintTypes));
        }

        return entries
            .OrderBy(static entry => entry.RoomTypeExpression, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static string Render(ImmutableArray<GameplayManifestEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("namespace AbilityKit.Orleans.Grains.Gameplay;");
        builder.AppendLine();
        builder.AppendLine("internal sealed record GeneratedServerGameplayManifestEntry(");
        builder.AppendLine("    string RoomType,");
        builder.AppendLine("    string DisplayName,");
        builder.AppendLine("    int DefaultMaxPlayers,");
        builder.AppendLine("    bool RequiresPlayerLoadout,");
        builder.AppendLine("    string? DefaultWorldType,");
        builder.AppendLine("    int DefaultTickRate,");
        builder.AppendLine("    string? DefaultSyncTemplateId,");
        builder.AppendLine("    string? DefaultSyncMode,");
        builder.AppendLine("    IReadOnlyList<string> SupportedSyncTemplateIds,");
        builder.AppendLine("    string? RoomAdapterType,");
        builder.AppendLine("    string? BattleRuntimeAdapterType,");
        builder.AppendLine("    IReadOnlyList<string> WorldBlueprintTypes);");
        builder.AppendLine();
        builder.AppendLine("internal static class GeneratedServerGameplayManifest");
        builder.AppendLine("{");
        builder.AppendLine("    public static IReadOnlyList<GeneratedServerGameplayManifestEntry> Entries { get; } = new GeneratedServerGameplayManifestEntry[]");
        builder.AppendLine("    {");

        foreach (var entry in entries)
        {
            builder.Append("        new GeneratedServerGameplayManifestEntry(");
            builder.Append(entry.RoomTypeExpression);
            builder.Append(", ");
            builder.Append(CSharpLiteral.String(entry.DisplayName));
            builder.Append(", ");
            builder.Append(entry.DefaultMaxPlayersExpression);
            builder.Append(", ");
            builder.Append(entry.RequiresPlayerLoadoutExpression);
            builder.Append(", ");
            builder.Append(entry.DefaultWorldTypeExpression);
            builder.Append(", ");
            builder.Append(entry.DefaultTickRateExpression);
            builder.Append(", ");
            builder.Append(CSharpLiteral.NullableStringOrNullExpression(entry.DefaultSyncTemplateId));
            builder.Append(", ");
            builder.Append(CSharpLiteral.NullableStringOrNullExpression(entry.DefaultSyncMode));
            builder.Append(", new[] { ");
            builder.Append(string.Join(", ", entry.SupportedSyncTemplateIds.Select(CSharpLiteral.String)));
            builder.Append(" }, ");
            builder.Append(CSharpLiteral.NullableStringOrNullExpression(entry.RoomAdapterType));
            builder.Append(", ");
            builder.Append(CSharpLiteral.NullableStringOrNullExpression(entry.BattleRuntimeAdapterType));
            builder.Append(", new[] { ");
            builder.Append(string.Join(", ", entry.WorldBlueprintTypes.Select(CSharpLiteral.String)));
            builder.AppendLine(" }),");
        }

        builder.AppendLine("    };");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private sealed record DescriptorCandidate(
        string PropertyName,
        string RoomTypeExpression,
        string DisplayName,
        string DefaultMaxPlayersExpression,
        string RequiresPlayerLoadoutExpression,
        string DefaultWorldTypeExpression,
        string DefaultTickRateExpression,
        string? DefaultSyncTemplateId);

    private sealed record ModuleCandidate(
        string DescriptorExpression,
        string? DefaultSyncMode,
        ImmutableArray<string> SupportedSyncTemplateIds,
        string? RoomAdapterType,
        string? BattleRuntimeAdapterType,
        ImmutableArray<string> WorldBlueprintTypes);

    private sealed record SyncTemplateCandidate(
        string? DefaultSyncMode,
        ImmutableArray<string> SupportedSyncTemplateIds);

    private sealed record GameplayManifestEntry(
        string RoomTypeExpression,
        string DisplayName,
        string DefaultMaxPlayersExpression,
        string RequiresPlayerLoadoutExpression,
        string DefaultWorldTypeExpression,
        string DefaultTickRateExpression,
        string? DefaultSyncTemplateId,
        string? DefaultSyncMode,
        ImmutableArray<string> SupportedSyncTemplateIds,
        string? RoomAdapterType,
        string? BattleRuntimeAdapterType,
        ImmutableArray<string> WorldBlueprintTypes);
}
