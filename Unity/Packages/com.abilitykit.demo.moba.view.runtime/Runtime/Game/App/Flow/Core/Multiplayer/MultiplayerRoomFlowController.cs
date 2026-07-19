#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 多人房间流程状态，供 UI 和 Flow 观察。
    /// </summary>
    public enum MultiplayerRoomFlowState
    {
        /// <summary>未开始。</summary>
        Idle = 0,
        /// <summary>正在登录。</summary>
        LoggingIn = 1,
        /// <summary>正在创建房间。</summary>
        CreatingRoom = 2,
        /// <summary>正在加入房间。</summary>
        JoiningRoom = 3,
        /// <summary>在大厅，等待选英雄/Ready。</summary>
        InLobby = 4,
        /// <summary>正在加载资源。</summary>
        LoadingAssets = 5,
        /// <summary>等待服务端开战。</summary>
        WaitingForBattle = 6,
        /// <summary>已进入战斗。</summary>
        InBattle = 7,
        /// <summary>失败。</summary>
        Failed = 8
    }

    /// <summary>
    /// 多人房间流程的快照视图（纯 C#，零 Unity 依赖）。
    /// 由 <see cref="IRoomSnapshotProvider"/> 投影，供控制器与 UI 共享。
    /// </summary>
    public sealed class MultiplayerRoomSnapshot
    {
        public string RoomId { get; set; } = string.Empty;
        public ulong NumericRoomId { get; set; }
        public MultiplayerRoomPhase Phase { get; set; }
        public bool CanStart { get; set; }
        public string BattleId { get; set; } = string.Empty;
        public ulong WorldId { get; set; }
        public long LaunchGeneration { get; set; }
        public int LaunchManifestVersion { get; set; }
        public string LaunchManifestHash { get; set; } = string.Empty;
        public long RoomRevision { get; set; }
    }

    /// <summary>
    /// 多人房间阶段（与服务端 RoomPhase 对齐的纯 C# 镜像）。
    /// </summary>
    public enum MultiplayerRoomPhase
    {
        Lobby = 0,
        Loading = 1,
        Starting = 2,
        InBattle = 3,
        Closing = 4,
        Closed = 5,
        Expired = 6
    }

    /// <summary>
    /// 选英雄/配置出战的参数。
    /// </summary>
    public readonly struct MultiplayerLoadoutSpec
    {
        public readonly int HeroId;
        public readonly int TeamId;
        public readonly int SpawnPointId;
        public readonly int Level;
        public readonly int AttributeTemplateId;
        public readonly int BasicAttackSkillId;
        public readonly int[] SkillIds;

        public MultiplayerLoadoutSpec(
            int heroId,
            int teamId,
            int spawnPointId,
            int level,
            int attributeTemplateId,
            int basicAttackSkillId,
            int[]? skillIds)
        {
            HeroId = heroId;
            TeamId = teamId;
            SpawnPointId = spawnPointId;
            Level = level;
            AttributeTemplateId = attributeTemplateId;
            BasicAttackSkillId = basicAttackSkillId;
            SkillIds = skillIds ?? Array.Empty<int>();
        }
    }

    /// <summary>
    /// 创建/加入房间的启动参数（对应 RoomGatewayLaunchSpec 的纯 C# 子集）。
    /// </summary>
    public sealed class MultiplayerRoomLaunchSpec
    {
        public string SessionToken { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string RoomType { get; set; } = "default";
        public string RoomTitle { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 2;
    }

    /// <summary>
    /// 抽象 RoomGatewaySessionFlow 的分阶段 API，使控制器可测试（零 host.extension 依赖）。
    /// </summary>
    public interface IMultiplayerRoomSession
    {
        /// <summary>阶段 1：创建房间，返回 roomId。</summary>
        Task<string> CreateRoomAsync(MultiplayerRoomLaunchSpec spec, CancellationToken cancellationToken);

        /// <summary>阶段 2：加入房间。</summary>
        Task JoinRoomAsync(MultiplayerRoomLaunchSpec spec, string roomId, CancellationToken cancellationToken);

        /// <summary>阶段 3：配置出战（PickHero）。</summary>
        Task ConfigureLoadoutAsync(string roomId, MultiplayerLoadoutSpec loadout, CancellationToken cancellationToken);

        /// <summary>阶段 4：设置准备状态。</summary>
        Task SetReadyAsync(string roomId, bool ready, CancellationToken cancellationToken);

        /// <summary>阶段 5：Owner 发起资源加载阶段。</summary>
        Task BeginLoadingAsync(string roomId, CancellationToken cancellationToken);

        /// <summary>阶段 6：成员上报资源加载完成。</summary>
        Task ReportAssetsLoadedAsync(string roomId, CancellationToken cancellationToken);

        /// <summary>阶段 7：等待战斗开始（轮询直到 Phase 进入 Starting/InBattle 或超时）。</summary>
        Task WaitForBattleStartAsync(string roomId, CancellationToken cancellationToken);
    }

    /// <summary>
    /// 抽象 ClientRoomStore 的快照订阅能力，使控制器可测试。
    /// </summary>
    public interface IRoomSnapshotProvider
    {
        /// <summary>当前快照（或 null）。</summary>
        MultiplayerRoomSnapshot? Current { get; }

        /// <summary>快照变更事件。</summary>
        event Action<MultiplayerRoomSnapshot>? OnSnapshotChanged;
    }

    /// <summary>
    /// 多人房间流程控制器：编排 登录→建房/入房→选英雄→Ready→BeginLoading→ReportAssets→WaitForBattle。
    /// <para>
    /// 纯 C#，零 Unity 依赖。通过 <see cref="IMultiplayerRoomSession"/> 与 <see cref="IRoomSnapshotProvider"/>
    /// 抽象与外部（RoomGatewaySessionFlow / ClientRoomStore）交互，使其可在无 Unity/host.extension 的测试项目中测试。
    /// </para>
    /// </summary>
    internal sealed class MultiplayerRoomFlowController : IDisposable
    {
        private readonly IMultiplayerRoomSession _session;
        private readonly IRoomSnapshotProvider _snapshotProvider;
        private bool _disposed;

        /// <summary>状态变更回调。每次 <see cref="CurrentState"/> 变化时触发。</summary>
        public event Action<MultiplayerRoomFlowState>? StateChanged;

        /// <summary>当前状态。</summary>
        public MultiplayerRoomFlowState CurrentState { get; private set; }

        /// <summary>当前房间快照（从 IRoomSnapshotProvider 投影）。</summary>
        public MultiplayerRoomSnapshot? CurrentSnapshot { get; private set; }

        /// <summary>最近一次错误信息（进入 Failed 状态时设置）。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>当前房间 Id（创建/加入成功后设置）。</summary>
        public string CurrentRoomId { get; private set; } = string.Empty;

        public MultiplayerRoomFlowController(IMultiplayerRoomSession session, IRoomSnapshotProvider snapshotProvider)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _snapshotProvider.OnSnapshotChanged += HandleSnapshotChanged;
            CurrentSnapshot = _snapshotProvider.Current;
        }

        /// <summary>
        /// 启动创建房间流程：Idle → LoggingIn → CreatingRoom → InLobby。
        /// </summary>
        public async Task StartCreateRoomAsync(MultiplayerRoomLaunchSpec spec, CancellationToken cancellationToken = default)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            await RunAsync(
                async ct =>
                {
                    Transition(MultiplayerRoomFlowState.LoggingIn);
                    Transition(MultiplayerRoomFlowState.CreatingRoom);
                    var roomId = await _session.CreateRoomAsync(spec, ct).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(roomId))
                    {
                        throw new InvalidOperationException("创建房间成功但未返回 roomId。");
                    }

                    CurrentRoomId = roomId;
                    await _session.JoinRoomAsync(spec, roomId, ct).ConfigureAwait(false);
                    Transition(MultiplayerRoomFlowState.InLobby);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 启动加入房间流程：Idle → LoggingIn → JoiningRoom → InLobby。
        /// </summary>
        public async Task StartJoinRoomAsync(MultiplayerRoomLaunchSpec spec, string roomId, CancellationToken cancellationToken = default)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId 不能为空。", nameof(roomId));
            await RunAsync(
                async ct =>
                {
                    Transition(MultiplayerRoomFlowState.LoggingIn);
                    Transition(MultiplayerRoomFlowState.JoiningRoom);
                    await _session.JoinRoomAsync(spec, roomId, ct).ConfigureAwait(false);
                    CurrentRoomId = roomId;
                    Transition(MultiplayerRoomFlowState.InLobby);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 选英雄/配置出战。仅在 InLobby 状态可用。
        /// </summary>
        public Task PickHeroAsync(MultiplayerLoadoutSpec loadout, CancellationToken cancellationToken = default)
        {
            return RunAsync(
                async ct =>
                {
                    EnsureState(MultiplayerRoomFlowState.InLobby);
                    await _session.ConfigureLoadoutAsync(CurrentRoomId, loadout, ct).ConfigureAwait(false);
                },
                cancellationToken);
        }

        /// <summary>
        /// 设置准备状态。仅在 InLobby 状态可用。
        /// </summary>
        public Task SetReadyAsync(bool ready, CancellationToken cancellationToken = default)
        {
            return RunAsync(
                async ct =>
                {
                    EnsureState(MultiplayerRoomFlowState.InLobby);
                    await _session.SetReadyAsync(CurrentRoomId, ready, ct).ConfigureAwait(false);
                },
                cancellationToken);
        }

        /// <summary>
        /// Owner 发起资源加载：InLobby → LoadingAssets。
        /// </summary>
        public Task BeginLoadingAsync(CancellationToken cancellationToken = default)
        {
            return RunAsync(
                async ct =>
                {
                    EnsureState(MultiplayerRoomFlowState.InLobby);
                    await _session.BeginLoadingAsync(CurrentRoomId, ct).ConfigureAwait(false);
                    Transition(MultiplayerRoomFlowState.LoadingAssets);
                },
                cancellationToken);
        }

        /// <summary>
        /// 成员上报资源加载完成：LoadingAssets → WaitingForBattle。
        /// </summary>
        public Task ReportAssetsLoadedAsync(CancellationToken cancellationToken = default)
        {
            return RunAsync(
                async ct =>
                {
                    EnsureState(MultiplayerRoomFlowState.LoadingAssets);
                    await _session.ReportAssetsLoadedAsync(CurrentRoomId, ct).ConfigureAwait(false);
                    Transition(MultiplayerRoomFlowState.WaitingForBattle);
                },
                cancellationToken);
        }

        /// <summary>
        /// 等待服务端开战：WaitingForBattle → InBattle。
        /// </summary>
        public Task WaitForBattleStartAsync(CancellationToken cancellationToken = default)
        {
            return RunAsync(
                async ct =>
                {
                    EnsureState(MultiplayerRoomFlowState.WaitingForBattle);
                    await _session.WaitForBattleStartAsync(CurrentRoomId, ct).ConfigureAwait(false);
                    Transition(MultiplayerRoomFlowState.InBattle);
                },
                cancellationToken);
        }

        /// <summary>
        /// 取消当前流程，回到 Idle。
        /// </summary>
        public void Cancel()
        {
            CurrentRoomId = string.Empty;
            LastError = string.Empty;
            Transition(MultiplayerRoomFlowState.Idle);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _snapshotProvider.OnSnapshotChanged -= HandleSnapshotChanged;
        }

        /// <summary>
        /// 从快照恢复：根据当前快照 Phase 推断控制器状态。
        /// </summary>
        public void RestoreFromSnapshot()
        {
            var snapshot = _snapshotProvider.Current;
            if (snapshot == null)
            {
                Cancel();
                return;
            }

            CurrentSnapshot = snapshot;
            CurrentRoomId = snapshot.RoomId;
            Transition(MapPhaseToState(snapshot.Phase));
        }

        private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                throw;
            }
        }

        private void HandleSnapshotChanged(MultiplayerRoomSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            if (!string.IsNullOrEmpty(snapshot.RoomId) && string.IsNullOrEmpty(CurrentRoomId))
            {
                CurrentRoomId = snapshot.RoomId;
            }

            // 仅在活跃流程中根据服务端 Phase 同步状态，避免覆盖用户驱动的中间态（LoggingIn/CreatingRoom 等）。
            if (IsActiveFlowState(CurrentState))
            {
                var mapped = MapPhaseToState(snapshot.Phase);
                if (mapped != CurrentState)
                {
                    Transition(mapped);
                }
            }
        }

        private void Transition(MultiplayerRoomFlowState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            StateChanged?.Invoke(next);
        }

        private void Fail(string message)
        {
            LastError = message ?? string.Empty;
            Transition(MultiplayerRoomFlowState.Failed);
        }

        private void EnsureState(MultiplayerRoomFlowState expected)
        {
            if (CurrentState != expected)
            {
                throw new InvalidOperationException(
                    $"当前状态不支持该操作：期望 {expected}，实际 {CurrentState}。");
            }
        }

        private static bool IsActiveFlowState(MultiplayerRoomFlowState state)
        {
            return state == MultiplayerRoomFlowState.InLobby ||
                   state == MultiplayerRoomFlowState.LoadingAssets ||
                   state == MultiplayerRoomFlowState.WaitingForBattle ||
                   state == MultiplayerRoomFlowState.InBattle;
        }

        private static MultiplayerRoomFlowState MapPhaseToState(MultiplayerRoomPhase phase)
        {
            switch (phase)
            {
                case MultiplayerRoomPhase.Lobby:
                    return MultiplayerRoomFlowState.InLobby;
                case MultiplayerRoomPhase.Loading:
                    return MultiplayerRoomFlowState.LoadingAssets;
                case MultiplayerRoomPhase.Starting:
                    return MultiplayerRoomFlowState.WaitingForBattle;
                case MultiplayerRoomPhase.InBattle:
                    return MultiplayerRoomFlowState.InBattle;
                case MultiplayerRoomPhase.Closed:
                case MultiplayerRoomPhase.Expired:
                case MultiplayerRoomPhase.Closing:
                    return MultiplayerRoomFlowState.Failed;
                default:
                    return MultiplayerRoomFlowState.Idle;
            }
        }
    }
}
