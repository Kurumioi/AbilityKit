using AbilityKit.Orleans.Contracts.Battle;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal interface IBattleRuntimeAdapter
{
    string RoomType { get; }

    IBattleRuntimeSession CreateSession(string battleId);
}

internal interface IBattleRuntimeSession : IDisposable
{
    BattleRuntimeStartResult Start(BattleInitParams initParams);

    int SubmitInputs(int frame, IReadOnlyList<BattleInputItem> inputs);

    bool Tick(int frame, int tickRate, float deltaTime);

    BattleSnapshot? GetSnapshot(int frame);

    StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot);
}

internal readonly record struct BattleRuntimeStartResult(bool Succeeded, string? Error)
{
    public static BattleRuntimeStartResult Success() => new(true, null);

    public static BattleRuntimeStartResult Fail(string error) => new(false, error);
}
