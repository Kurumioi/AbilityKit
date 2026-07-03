using AbilityKit.Orleans.Contracts.Battle;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal interface IBattleRuntimeAdapter
{
    string RoomType { get; }

    IBattleRuntimeSession CreateSession(string battleId);
}

internal readonly record struct BattleStateSyncObserverContext(
    string ObserverKey,
    string AccountId,
    string RoomId);

internal interface IBattleRuntimeSession : IDisposable
{
    BattleRuntimeStartResult Start(BattleInitParams initParams);

    BattlePlayerJoinResult JoinPlayer(BattlePlayerJoinRequest request, int currentFrame);

    int SubmitInputs(int frame, IReadOnlyList<BattleInputItem> inputs);

    BattleBotAiMountResult MountBotAi(BattleBotAiMountRequest request, int currentFrame);

    bool Tick(int frame, int tickRate, float deltaTime);

    BattleSnapshot? GetSnapshot(int frame);

    BattleWorldDiagnostics? GetWorldDiagnostics(ulong worldId, int frame);

    StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot);
}

internal interface IObserverAwareBattleRuntimeSession
{
    StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot, in BattleStateSyncObserverContext observerContext);
}

internal readonly record struct BattleRuntimeStartResult(bool Succeeded, string? Error)
{
    public static BattleRuntimeStartResult Success() => new(true, null);

    public static BattleRuntimeStartResult Fail(string error) => new(false, error);
}
