namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

internal static class GatewaySkillDiagnostics
{
    private const string TraceNotConnected = "TraceNotConnected";

    public static async Task<AdminSkillDiagnosticsSummaryHttpResponse> GetSummaryAsync(
        IClusterClient client,
        string? roomId,
        string? battleId)
    {
        RoomRuntimeState? runtimeState = null;
        var warnings = new List<string>
        {
            "Skill trace sink is not connected yet. Current response only uses room and battle context already available from Orleans grains."
        };

        if (!string.IsNullOrWhiteSpace(roomId))
        {
            try
            {
                var room = client.GetGrain<IRoomGrain>(roomId);
                runtimeState = await room.GetRuntimeStateAsync();
            }
            catch (Exception exception)
            {
                warnings.Add($"Room runtime state probe failed: {exception.Message}");
            }
        }

        var resolvedBattleId = !string.IsNullOrWhiteSpace(battleId) ? battleId : runtimeState?.BattleId;
        var currentFrame = 0;
        if (!string.IsNullOrWhiteSpace(resolvedBattleId))
        {
            try
            {
                var battle = client.GetGrain<IBattleLogicHostGrain>(resolvedBattleId);
                currentFrame = await battle.GetCurrentFrameAsync();
            }
            catch (Exception exception)
            {
                warnings.Add($"Battle frame probe failed: {exception.Message}");
            }
        }

        var members = runtimeState?.Members?.ToArray() ?? Array.Empty<string>();
        var actorSummaries = members
            .Select((member, index) => new AdminSkillActorSummaryHttpResponse(
                member,
                index + 1,
                0,
                Array.Empty<int>(),
                "Loadout details are not projected to skill diagnostics yet."))
            .ToArray();

        var metrics = new[]
        {
            new AdminSkillMetricHttpResponse("CastCount", 0, "count", TraceNotConnected),
            new AdminSkillMetricHttpResponse("RejectCount", 0, "count", TraceNotConnected),
            new AdminSkillMetricHttpResponse("FailureCount", 0, "count", TraceNotConnected),
            new AdminSkillMetricHttpResponse("DamageTotal", 0, "value", TraceNotConnected),
            new AdminSkillMetricHttpResponse("BuffApplyCount", 0, "count", TraceNotConnected),
            new AdminSkillMetricHttpResponse("AvgPipelineMs", 0, "ms", TraceNotConnected)
        };

        return new AdminSkillDiagnosticsSummaryHttpResponse(
            runtimeState?.RoomId ?? roomId,
            runtimeState?.RoomType,
            resolvedBattleId,
            runtimeState?.WorldId ?? 0UL,
            runtimeState?.IsInBattle ?? false,
            currentFrame,
            members,
            TraceNotConnected,
            actorSummaries,
            metrics,
            warnings.ToArray(),
            DateTime.UtcNow.Ticks);
    }

    public static AdminSkillAnalysisModelHttpResponse GetAnalysisModel()
    {
        return GatewaySkillAnalysisModelProvider.GetModel();
    }

    public static Task<AdminSkillDiagnosticsEventsHttpResponse> GetEventsAsync(
        string? battleId,
        int? actorId,
        int? skillId,
        int limit)
    {
        var effectiveLimit = limit <= 0 ? 100 : Math.Min(limit, 500);
        var filters = new AdminSkillEventFilterHttpResponse(battleId, actorId, skillId, effectiveLimit);
        var warnings = new[]
        {
            "Skill event timeline is waiting for MOBA runtime trace sink integration."
        };

        var response = new AdminSkillDiagnosticsEventsHttpResponse(
            TraceNotConnected,
            filters,
            Array.Empty<AdminSkillEventHttpResponse>(),
            warnings,
            DateTime.UtcNow.Ticks);
        return Task.FromResult(response);
    }
}
