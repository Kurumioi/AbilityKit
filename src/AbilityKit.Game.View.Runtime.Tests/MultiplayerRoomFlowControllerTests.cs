using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// MultiplayerRoomFlowController 的状态转换逻辑测试。
    /// 使用 stub 实现的 IMultiplayerRoomSession / IRoomSnapshotProvider，零 Unity/host.extension 依赖。
    /// </summary>
    public sealed class MultiplayerRoomFlowControllerTests
    {
        [Fact]
        public async Task StartCreateRoomAsync_Success_Idle_To_InLobby()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            var visited = new List<MultiplayerRoomFlowState>();
            controller.StateChanged += s => visited.Add(s);

            await controller.StartCreateRoomAsync(NewSpec());

            Assert.Equal(MultiplayerRoomFlowState.InLobby, controller.CurrentState);
            Assert.Equal(StubSession.CreatedRoomId, controller.CurrentRoomId);
            // Idle → LoggingIn → CreatingRoom → InLobby（状态转换路径）
            Assert.Equal(
                new[]
                {
                    MultiplayerRoomFlowState.LoggingIn,
                    MultiplayerRoomFlowState.CreatingRoom,
                    MultiplayerRoomFlowState.InLobby
                },
                visited);
        }

        [Fact]
        public async Task StartJoinRoomAsync_Success_Idle_To_InLobby()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            var visited = new List<MultiplayerRoomFlowState>();
            controller.StateChanged += s => visited.Add(s);

            await controller.StartJoinRoomAsync(NewSpec(), "room-xyz");

            Assert.Equal(MultiplayerRoomFlowState.InLobby, controller.CurrentState);
            Assert.Equal("room-xyz", controller.CurrentRoomId);
            // Idle → LoggingIn → JoiningRoom → InLobby（状态转换路径）
            Assert.Equal(
                new[]
                {
                    MultiplayerRoomFlowState.LoggingIn,
                    MultiplayerRoomFlowState.JoiningRoom,
                    MultiplayerRoomFlowState.InLobby
                },
                visited);
        }

        [Fact]
        public async Task PickHeroAsync_InLobby_CallsSession()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            var loadout = new MultiplayerLoadoutSpec(
                heroId: 2002, teamId: 1, spawnPointId: 0, level: 1,
                attributeTemplateId: 0, basicAttackSkillId: 0, skillIds: new[] { 1, 2 });

            await controller.PickHeroAsync(loadout);

            Assert.True(session.ConfigureLoadoutCalled);
            Assert.Equal(2002, session.LastLoadout.HeroId);
            Assert.Equal(StubSession.CreatedRoomId, session.LastLoadoutRoomId);
        }

        [Fact]
        public async Task SetReadyAsync_InLobby_CallsSession()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            await controller.SetReadyAsync(true);

            Assert.True(session.SetReadyCalled);
            Assert.True(session.LastReadyValue);
        }

        [Fact]
        public async Task BeginLoadingAsync_InLobby_Transitions_To_LoadingAssets()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            await controller.BeginLoadingAsync();

            Assert.Equal(MultiplayerRoomFlowState.LoadingAssets, controller.CurrentState);
            Assert.True(session.BeginLoadingCalled);
        }

        [Fact]
        public async Task ReportAssetsLoadedAsync_AfterLoading_Transitions_To_WaitingForBattle()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());
            await controller.BeginLoadingAsync();

            await controller.ReportAssetsLoadedAsync();

            Assert.Equal(MultiplayerRoomFlowState.WaitingForBattle, controller.CurrentState);
            Assert.True(session.ReportAssetsLoadedCalled);
        }

        [Fact]
        public async Task WaitForBattleStartAsync_Transitions_To_InBattle()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());
            await controller.BeginLoadingAsync();
            await controller.ReportAssetsLoadedAsync();

            await controller.WaitForBattleStartAsync();

            Assert.Equal(MultiplayerRoomFlowState.InBattle, controller.CurrentState);
            Assert.True(session.WaitForBattleStartCalled);
        }

        [Fact]
        public async Task CreateRoom_Failure_Transitions_To_Failed_With_LastError()
        {
            var session = new StubSession
            {
                CreateRoomException = new InvalidOperationException("boom-create")
            };
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.StartCreateRoomAsync(NewSpec()));

            Assert.Equal(MultiplayerRoomFlowState.Failed, controller.CurrentState);
            Assert.Contains("boom-create", controller.LastError);
        }

        [Fact]
        public async Task JoinRoom_Failure_Transitions_To_Failed_With_LastError()
        {
            var session = new StubSession
            {
                JoinRoomException = new InvalidOperationException("boom-join")
            };
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.StartJoinRoomAsync(NewSpec(), "room-1"));

            Assert.Equal(MultiplayerRoomFlowState.Failed, controller.CurrentState);
            Assert.Contains("boom-join", controller.LastError);
        }

        [Fact]
        public async Task BeginLoading_Failure_Transitions_To_Failed()
        {
            var session = new StubSession
            {
                BeginLoadingException = new InvalidOperationException("boom-load")
            };
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.BeginLoadingAsync());

            Assert.Equal(MultiplayerRoomFlowState.Failed, controller.CurrentState);
            Assert.Contains("boom-load", controller.LastError);
        }

        [Fact]
        public async Task StateChanged_Fires_On_Every_Transition()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            var fired = new List<MultiplayerRoomFlowState>();
            controller.StateChanged += s => fired.Add(s);

            await controller.StartCreateRoomAsync(NewSpec());
            await controller.BeginLoadingAsync();

            // 依次触发：LoggingIn, CreatingRoom, InLobby, LoadingAssets
            Assert.Equal(4, fired.Count);
            Assert.Equal(MultiplayerRoomFlowState.LoggingIn, fired[0]);
            Assert.Equal(MultiplayerRoomFlowState.CreatingRoom, fired[1]);
            Assert.Equal(MultiplayerRoomFlowState.InLobby, fired[2]);
            Assert.Equal(MultiplayerRoomFlowState.LoadingAssets, fired[3]);
        }

        [Fact]
        public async Task PickHero_Outside_InLobby_Throws_InvalidOperation()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.PickHeroAsync(default));
        }

        [Fact]
        public async Task SnapshotChanged_In_ActiveFlow_Syncs_State()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            // 服务端推送 Loading 阶段快照，控制器应同步到 LoadingAssets。
            provider.Emit(new MultiplayerRoomSnapshot
            {
                RoomId = StubSession.CreatedRoomId,
                Phase = MultiplayerRoomPhase.Loading
            });

            Assert.Equal(MultiplayerRoomFlowState.LoadingAssets, controller.CurrentState);
        }

        [Fact]
        public async Task Cancel_Resets_To_Idle()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);
            await controller.StartCreateRoomAsync(NewSpec());

            controller.Cancel();

            Assert.Equal(MultiplayerRoomFlowState.Idle, controller.CurrentState);
            Assert.Equal(string.Empty, controller.CurrentRoomId);
        }

        [Fact]
        public void RestoreFromSnapshot_NullSnapshot_Goes_Idle()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            controller.RestoreFromSnapshot();

            Assert.Equal(MultiplayerRoomFlowState.Idle, controller.CurrentState);
        }

        [Fact]
        public void RestoreFromSnapshot_LobbySnapshot_Goes_InLobby()
        {
            var session = new StubSession();
            var provider = new StubSnapshotProvider();
            var controller = new MultiplayerRoomFlowController(session, provider);

            controller.RestoreFromSnapshot();
            provider.Emit(new MultiplayerRoomSnapshot
            {
                RoomId = "restored-room",
                Phase = MultiplayerRoomPhase.Lobby
            });
            controller.RestoreFromSnapshot();

            Assert.Equal(MultiplayerRoomFlowState.InLobby, controller.CurrentState);
            Assert.Equal("restored-room", controller.CurrentRoomId);
        }

        private static MultiplayerRoomLaunchSpec NewSpec()
        {
            return new MultiplayerRoomLaunchSpec
            {
                SessionToken = "token",
                Region = "r",
                ServerId = "s",
                RoomType = "default",
                RoomTitle = "T",
                MaxPlayers = 2
            };
        }

        private sealed class StubSession : IMultiplayerRoomSession
        {
            public const string CreatedRoomId = "room-created";

            public bool ConfigureLoadoutCalled;
            public MultiplayerLoadoutSpec LastLoadout;
            public string LastLoadoutRoomId;

            public bool SetReadyCalled;
            public bool LastReadyValue;

            public bool BeginLoadingCalled;
            public bool ReportAssetsLoadedCalled;
            public bool WaitForBattleStartCalled;

            public Exception CreateRoomException;
            public Exception JoinRoomException;
            public Exception BeginLoadingException;

            public Task<string> CreateRoomAsync(MultiplayerRoomLaunchSpec spec, CancellationToken cancellationToken)
            {
                if (CreateRoomException != null) throw CreateRoomException;
                return Task.FromResult(CreatedRoomId);
            }

            public Task JoinRoomAsync(MultiplayerRoomLaunchSpec spec, string roomId, CancellationToken cancellationToken)
            {
                if (JoinRoomException != null) throw JoinRoomException;
                return Task.CompletedTask;
            }

            public Task ConfigureLoadoutAsync(string roomId, MultiplayerLoadoutSpec loadout, CancellationToken cancellationToken)
            {
                ConfigureLoadoutCalled = true;
                LastLoadout = loadout;
                LastLoadoutRoomId = roomId;
                return Task.CompletedTask;
            }

            public Task SetReadyAsync(string roomId, bool ready, CancellationToken cancellationToken)
            {
                SetReadyCalled = true;
                LastReadyValue = ready;
                return Task.CompletedTask;
            }

            public Task BeginLoadingAsync(string roomId, CancellationToken cancellationToken)
            {
                if (BeginLoadingException != null) throw BeginLoadingException;
                BeginLoadingCalled = true;
                return Task.CompletedTask;
            }

            public Task ReportAssetsLoadedAsync(string roomId, CancellationToken cancellationToken)
            {
                ReportAssetsLoadedCalled = true;
                return Task.CompletedTask;
            }

            public Task WaitForBattleStartAsync(string roomId, CancellationToken cancellationToken)
            {
                WaitForBattleStartCalled = true;
                return Task.CompletedTask;
            }
        }

        private sealed class StubSnapshotProvider : IRoomSnapshotProvider
        {
            private MultiplayerRoomSnapshot _current;

            public MultiplayerRoomSnapshot Current => _current;

            public event Action<MultiplayerRoomSnapshot> OnSnapshotChanged;

            public void Emit(MultiplayerRoomSnapshot snapshot)
            {
                _current = snapshot;
                OnSnapshotChanged?.Invoke(snapshot);
            }
        }
    }
}
