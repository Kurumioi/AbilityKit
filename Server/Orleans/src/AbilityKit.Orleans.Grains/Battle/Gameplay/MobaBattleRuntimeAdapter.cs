using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Grains.Battle.Protocol;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;
using IWorld = AbilityKit.Ability.World.Abstractions.IWorld;
using IWorldStateSnapshotProvider = AbilityKit.Ability.Host.IWorldStateSnapshotProvider;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal sealed class MobaBattleRuntimeAdapter : IBattleRuntimeAdapter
{
    private readonly ServerMobaWorldManager _worldManager;
    private readonly IOrleansBattleProtocolMapper _protocolMapper;

    public MobaBattleRuntimeAdapter(ServerMobaWorldManager worldManager, IOrleansBattleProtocolMapper protocolMapper)
    {
        _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        _protocolMapper = protocolMapper ?? throw new ArgumentNullException(nameof(protocolMapper));
    }

    public string RoomType => MobaRoomGameplayAdapter.DefaultRoomType;

    public IBattleRuntimeSession CreateSession(string battleId)
    {
        return new MobaBattleRuntimeSession(battleId, _worldManager, _protocolMapper);
    }

    private sealed class MobaBattleRuntimeSession : IBattleRuntimeSession
    {
        private readonly string _battleId;
        private readonly ServerMobaWorldManager _worldManager;
        private readonly IOrleansBattleProtocolMapper _protocolMapper;
        private IWorld? _battleWorld;
        private IWorldStateSnapshotProvider? _snapshotProvider;
        private IMobaBattleRuntimePort? _runtimePort;
        private ulong _worldId;

        public MobaBattleRuntimeSession(string battleId, ServerMobaWorldManager worldManager, IOrleansBattleProtocolMapper protocolMapper)
        {
            _battleId = battleId;
            _worldManager = worldManager;
            _protocolMapper = protocolMapper;
        }

        public BattleRuntimeStartResult Start(BattleInitParams initParams)
        {
            if (initParams is null)
            {
                return BattleRuntimeStartResult.Fail("Battle init params are missing.");
            }

            _worldId = initParams.WorldId;
            _battleWorld = _worldManager.CreateBattleWorld(_battleId, initParams.TickRate);
            if (_battleWorld == null)
            {
                return BattleRuntimeStartResult.Fail("Battle world creation returned null.");
            }

            if (!_battleWorld.Services.TryResolve<IMobaBattleRuntimePort>(out _runtimePort) || _runtimePort == null)
            {
                return BattleRuntimeStartResult.Fail("IMobaBattleRuntimePort not resolved.");
            }

            if (!_runtimePort.Status.IsReadyForGameStart)
            {
                return BattleRuntimeStartResult.Fail(_runtimePort.Status.ToString());
            }

            var startSpec = _protocolMapper.CreateGameStartSpec(_battleId, initParams.TickRate, initParams);
            var startResult = _runtimePort.TryStartGame(in startSpec);
            if (!startResult.Succeeded)
            {
                return BattleRuntimeStartResult.Fail(startResult.ToString());
            }

            _snapshotProvider = _battleWorld.Services.Resolve<IWorldStateSnapshotProvider>();
            if (_snapshotProvider == null)
            {
                return BattleRuntimeStartResult.Fail("IWorldStateSnapshotProvider not resolved.");
            }

            return BattleRuntimeStartResult.Success();
        }

        public int SubmitInputs(int frame, IReadOnlyList<BattleInputItem> inputs)
        {
            if (inputs.Count == 0 || _runtimePort == null)
            {
                return 0;
            }

            var commands = _protocolMapper.CreatePlayerInputCommands(frame, inputs);
            if (commands.Count == 0)
            {
                return 0;
            }

            var result = _runtimePort.Submit(new FrameIndex(frame), commands);
            return result.Succeeded ? commands.Count : 0;
        }

        public bool Tick(int frame, int tickRate, float deltaTime)
        {
            if (_battleWorld == null)
            {
                return false;
            }

            _battleWorld.Tick(deltaTime);
            return true;
        }

        public BattleSnapshot? GetSnapshot(int frame)
        {
            var frameIndex = new FrameIndex(frame);
            if (_snapshotProvider != null && _snapshotProvider.TryGetSnapshot(frameIndex, out var snapshot))
            {
                return _protocolMapper.CreateBattleSnapshot(frame, snapshot, _runtimePort?.GetAllEntityStates());
            }

            return null;
        }

        public StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot)
        {
            var frameIndex = new FrameIndex(frame);
            WorldStateSnapshot snapshot = default;
            var hasSnapshot = _runtimePort?.TryGetSnapshot(frameIndex, out snapshot) == true;
            if (!hasSnapshot && _snapshotProvider != null)
            {
                hasSnapshot = _snapshotProvider.TryGetSnapshot(frameIndex, out snapshot);
            }

            return _protocolMapper.CreateStateSyncPush(
                worldId == 0 ? _worldId : worldId,
                frame,
                hasSnapshot ? snapshot : null,
                _runtimePort?.GetAllEntityStates(),
                isFullSnapshot);
        }

        public void Dispose()
        {
            _worldManager.DestroyBattleWorld(_battleId);
            _battleWorld = null;
            _snapshotProvider = null;
            _runtimePort = null;
        }
    }
}
