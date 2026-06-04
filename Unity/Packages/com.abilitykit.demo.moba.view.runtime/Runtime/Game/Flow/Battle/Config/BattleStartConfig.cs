using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.IO;

namespace AbilityKit.Game.Flow
{
    [CreateAssetMenu(menuName = "AbilityKit/Game/Battle Start Config", fileName = "BattleStartConfig")]
    public sealed class BattleStartConfig : ScriptableObject
    {
        public enum BattleRunMode
        {
            Normal = 0,
            Record = 1,
            Replay = 2,
        }

        public enum BattleHostMode
        {
            Local = 0,
            GatewayRemote = 1,
        }

        [Header("Preset")]
        [LabelText("Preset(妯℃澘/瀹屽叏瑕嗙洊)")]
        public BattleStartPresetSO Preset;

        [LabelText("RuntimeOverrides(灏戦噺杩愯鏃惰鐩?")]
        public BattleStartRuntimeOverrides RuntimeOverrides;

        [Header("Formal Start Profile")]
        [LabelText("WorldId(涓栫晫ID/鎴块棿ID)")]
        public string WorldId = "room_1";

        [LabelText("WorldType(涓栫晫绫诲瀷)")]
        public string WorldType = "battle";

        [LabelText("ClientId(瀹㈡埛绔疘D)")]
        public string ClientId = "battle_client";

        [LabelText("HostMode(涓绘満妯″紡)")]
        public BattleHostMode HostMode = BattleHostMode.Local;

        [LabelText("AutoConnect(鑷姩杩炴帴)")]
        public bool AutoConnect = true;

        [LabelText("AutoCreateWorld(鑷姩鍒涘缓World)")]
        public bool AutoCreateWorld = true;

        [LabelText("AutoJoin(鑷姩鍔犲叆)")]
        public bool AutoJoin = true;

        [LabelText("AutoReady(鑷姩鍑嗗)")]
        public bool AutoReady = true;

        [LabelText("SyncMode(鍚屾妯″紡)")]
        public BattleSyncMode SyncMode = BattleSyncMode.Lockstep;

        [LabelText("ViewEventSourceMode(View浜嬩欢婧愭ā寮?")]
        public BattleViewEventSourceMode ViewEventSourceMode = BattleViewEventSourceMode.SnapshotOnly;

        [LabelText("EnabledSnapshotRegistryIds(Snapshot Registries)")]
        public string[] EnabledSnapshotRegistryIds;

        [LabelText("EnableClientPrediction(瀹㈡埛绔娴?")]
        public bool EnableClientPrediction = true;

        [LabelText("EnableConfirmedAuthorityWorld(鏉冨▉纭涓栫晫)")]
        public bool EnableConfirmedAuthorityWorld = false;

        [LabelText("GameplayId(鐜╂硶閰嶇疆ID)")]
        public int GameplayId = 1;

        [LabelText("EnterGame閰嶇疆(鍙鐢⊿O)")]
        public BattleEnterGameConfigSO EnterGameSO;

        [LabelText("鐜╁閰嶇疆(鍙鐢⊿O)")]
        public BattlePlayersConfigSO PlayersSO;

        [LabelText("UseRoomGameStartSpec(鐢ㄥ閮≧oomSpec鐢熸垚CreateWorldPayload)")]
        public bool UseRoomGameStartSpec;

        [LabelText("杩愯妯″紡閰嶇疆(鍙鐢⊿O)")]
        public BattleRunModeConfigSO RunModeSO;

        [LabelText("缃戝叧閰嶇疆(鍙鐢⊿O)")]
        public BattleGatewayConfigSO GatewaySO;

        [Header("Composition")]
        [LabelText("FeatureSet(鎴樻枟闃舵Feature缁勫悎)")]
        public BattleFeatureSetConfig FeatureSet;

        public BattleFeatureSetConfig EffectiveFeatureSet => Preset != null ? Preset.FeatureSet : FeatureSet;

        public bool TryBuildCreateWorldPayload(out int opCode, out byte[] payload)
        {
            opCode = 0;
            payload = null;

            var enterGameSo = Preset != null ? Preset.EnterGameSO : EnterGameSO;
            if (enterGameSo == null) return false;

            opCode = enterGameSo.OpCode;
            var payloadBase64 = enterGameSo.PayloadBase64;

            if (string.IsNullOrEmpty(payloadBase64))
            {
                payload = null;
                return true;
            }

            try
            {
                payload = Convert.FromBase64String(payloadBase64);
                return true;
            }
            catch
            {
                payload = null;
                return false;
            }
        }

        public EnterMobaGameReq BuildEnterMobaGameReq()
        {
            return BuildLaunchSpec().ToEnterReq();
        }

        public MobaBattleLaunchSpec BuildLaunchSpec()
        {
            var playersSo = Preset != null ? Preset.PlayersSO : PlayersSO;
            var enterGameSo = Preset != null ? Preset.EnterGameSO : EnterGameSO;

            if (playersSo == null) throw new InvalidOperationException("PlayersSO is required.");
            if (enterGameSo == null) throw new InvalidOperationException("EnterGameSO is required.");

            var playerId = !string.IsNullOrEmpty(playersSo.LocalPlayerId) ? playersSo.LocalPlayerId : "p1";
            byte[] payload = null;
            TryBuildCreateWorldPayload(out _, out payload);

            return new MobaBattleLaunchSpec(
                battleId: GetEffectiveWorldId(),
                matchId: GetEffectiveWorldId(),
                worldId: GetEffectiveWorldId(),
                worldType: Preset != null ? Preset.WorldType : WorldType,
                clientId: GetEffectiveClientId(),
                localPlayerId: new PlayerId(playerId),
                mapId: enterGameSo.MapId,
                gameplayId: GetEffectiveGameplayId(),
                ruleSetId: 0,
                configVersion: 0,
                protocolVersion: 0,
                randomSeed: enterGameSo.RandomSeed,
                tickRate: enterGameSo.TickRate,
                inputDelayFrames: enterGameSo.InputDelayFrames,
                launchMode: MobaBattleLaunchMode.ViewFastEnter,
                syncMode: ToLaunchSyncMode(Preset != null ? Preset.SyncMode : SyncMode),
                authorityMode: ResolveAuthorityMode(),
                players: BuildPlayersLoadout(playersSo),
                enterGameOpCode: enterGameSo.OpCode,
                enterGamePayload: payload);
        }

        public MobaRoomGameStartSpec BuildRoomGameStartSpec()
        {
            var playersSo = Preset != null ? Preset.PlayersSO : PlayersSO;
            var enterGameSo = Preset != null ? Preset.EnterGameSO : EnterGameSO;

            if (playersSo == null) throw new InvalidOperationException("PlayersSO is required.");
            if (enterGameSo == null) throw new InvalidOperationException("EnterGameSO is required.");

            var slots = new List<MobaRoomPlayerSlot>(4);

            if (playersSo.Team1Players != null)
            {
                for (int i = 0; i < playersSo.Team1Players.Count; i++)
                {
                    var p = playersSo.Team1Players[i];
                    if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;

                    var ov = new MobaRoomLoadoutOverrides(p.Level, p.AttributeTemplateId, p.BasicAttackSkillId, p.SkillIds);
                    slots.Add(new MobaRoomPlayerSlot(new PlayerId(p.PlayerId), (int)p.TeamId, p.HeroId, p.SpawnIndex, in ov));
                }
            }

            if (playersSo.Team2Players != null)
            {
                for (int i = 0; i < playersSo.Team2Players.Count; i++)
                {
                    var p = playersSo.Team2Players[i];
                    if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;

                    var ov = new MobaRoomLoadoutOverrides(p.Level, p.AttributeTemplateId, p.BasicAttackSkillId, p.SkillIds);
                    slots.Add(new MobaRoomPlayerSlot(new PlayerId(p.PlayerId), (int)p.TeamId, p.HeroId, p.SpawnIndex, in ov));
                }
            }

            return new MobaRoomGameStartSpec(
                matchId: GetEffectiveWorldId(),
                mapId: enterGameSo.MapId,
                randomSeed: enterGameSo.RandomSeed,
                tickRate: enterGameSo.TickRate,
                inputDelayFrames: enterGameSo.InputDelayFrames,
                players: slots.Count == 0 ? null : slots.ToArray(),
                gameplayId: GetEffectiveGameplayId());
        }

        public EnterMobaGameReq BuildEnterMobaGameReq(in MobaRoomGameStartSpec roomSpec)
        {
            return BuildLaunchSpec(in roomSpec).ToEnterReq();
        }

        public MobaBattleLaunchSpec BuildLaunchSpec(in MobaRoomGameStartSpec roomSpec)
        {
            var playersSo = Preset != null ? Preset.PlayersSO : PlayersSO;
            var enterGameSo = Preset != null ? Preset.EnterGameSO : EnterGameSO;

            if (playersSo == null) throw new InvalidOperationException("PlayersSO is required.");
            if (enterGameSo == null) throw new InvalidOperationException("EnterGameSO is required.");

            var playerId = !string.IsNullOrEmpty(playersSo.LocalPlayerId) ? playersSo.LocalPlayerId : "p1";
            byte[] payload = null;
            TryBuildCreateWorldPayload(out _, out payload);

            var startPlan = MobaBattleStartPlan.FromRoomSpec(new PlayerId(playerId), in roomSpec, enterGameSo.OpCode, payload);
            return MobaBattleLaunchSpecBuilder.FromStartPlan(
                in startPlan,
                worldId: GetEffectiveWorldId(),
                worldType: Preset != null ? Preset.WorldType : WorldType,
                clientId: GetEffectiveClientId(),
                launchMode: MobaBattleLaunchMode.RoomFlow,
                syncMode: ToLaunchSyncMode(Preset != null ? Preset.SyncMode : SyncMode),
                authorityMode: ResolveAuthorityMode());
        }

        public BattleStartPlanOptions BuildPlanOptions(in EnterMobaGameReq req, byte[] createWorldPayload, int createWorldOpCode, MobaBattleLaunchSpec launchSpec = default)
        {
            var runModeSo = Preset != null ? Preset.RunModeSO : RunModeSO;
            var gatewaySo = Preset != null ? Preset.GatewaySO : GatewaySO;

            var runMode = runModeSo != null ? runModeSo.Mode : BattleRunMode.Normal;
            var enableInputRecording = runMode == BattleRunMode.Record;
            var enableInputReplay = runMode == BattleRunMode.Replay;

            var recordDirectory = runModeSo != null ? runModeSo.RecordOutputDirectory : "battle_records";
            if (string.IsNullOrEmpty(recordDirectory)) recordDirectory = "battle_records";

            var recordExt = (runModeSo != null && runModeSo.RecordFormat == BattleRunModeConfigSO.InputRecordFormat.Binary) ? "bin" : "json";
            var recordFileName = $"battle_record_{DateTime.Now:yyyyMMdd_HHmmss}.{recordExt}";
            var recordPath = Path.Combine(recordDirectory, recordFileName);
            if (!Path.IsPathRooted(recordPath)) recordPath = Path.Combine(Application.persistentDataPath, recordPath);

            var replayPath = runModeSo != null ? runModeSo.ReplayInputFilePath : string.Empty;

            if (RuntimeOverrides != null)
            {
                if (RuntimeOverrides.HasRecordOutputDirectory) recordDirectory = RuntimeOverrides.RecordOutputDirectory;
                if (RuntimeOverrides.HasReplayInputFilePath) replayPath = RuntimeOverrides.ReplayInputFilePath;
            }

            if (string.IsNullOrEmpty(recordDirectory)) recordDirectory = "battle_records";
            recordPath = Path.Combine(recordDirectory, recordFileName);
            if (!Path.IsPathRooted(recordPath)) recordPath = Path.Combine(Application.persistentDataPath, recordPath);

            var hostMode = Preset != null ? Preset.HostMode : HostMode;
            var gateway = gatewaySo;
            if (hostMode == BattleHostMode.GatewayRemote && gateway == null)
            {
                throw new InvalidOperationException("GatewaySO is required when HostMode is GatewayRemote.");
            }

            var numericRoomId = gateway != null ? gateway.NumericRoomId : 0;
            var joinRoomId = gateway != null ? gateway.JoinRoomId : string.Empty;
            if (RuntimeOverrides != null && RuntimeOverrides.HasNumericRoomId) numericRoomId = RuntimeOverrides.NumericRoomId;
            if (RuntimeOverrides != null && RuntimeOverrides.HasGatewayJoinRoomId) joinRoomId = RuntimeOverrides.GatewayJoinRoomId;

            var gatewayOptions = new BattleStartPlanGatewayOptions(
                useGatewayTransport: gateway != null && gateway.UseGatewayTransport,
                host: gateway != null ? gateway.Host : "127.0.0.1",
                port: gateway != null ? gateway.Port : 4000,
                numericRoomId: numericRoomId,
                sessionToken: gateway != null ? gateway.SessionToken : string.Empty,
                region: gateway != null ? gateway.Region : "dev",
                serverId: gateway != null ? gateway.ServerId : "local",
                autoCreateRoom: gateway != null && gateway.AutoCreateRoom,
                autoJoinRoom: gateway != null && gateway.AutoJoinRoom,
                joinRoomId: joinRoomId,
                createRoomOpCode: gateway != null ? gateway.CreateRoomOpCode : 110,
                joinRoomOpCode: gateway != null ? gateway.JoinRoomOpCode : 111);

            var autoConnect = Preset != null ? Preset.AutoConnect : AutoConnect;
            var autoCreateWorld = Preset != null ? Preset.AutoCreateWorld : AutoCreateWorld;
            var autoJoin = Preset != null ? Preset.AutoJoin : AutoJoin;
            var autoReady = Preset != null ? Preset.AutoReady : AutoReady;

            var autoOptions = new BattleStartPlanAutoOptions(autoConnect, autoCreateWorld, autoJoin, autoReady);

            var runModeOptions = new BattleStartPlanRunModeOptions(
                runMode: runMode,
                enableInputRecording: enableInputRecording,
                inputRecordOutputPath: recordPath,
                enableInputReplay: enableInputReplay,
                inputReplayPath: replayPath);

            var createWorldOptions = new BattleStartPlanCreateWorldOptions(createWorldOpCode, createWorldPayload);

            var timeSyncOptions = new BattleStartPlanTimeSyncOptions(
                opCode: gateway != null ? gateway.TimeSyncOpCode : 1300u,
                intervalMs: gateway != null ? gateway.TimeSyncIntervalMs : 1000,
                alpha: gateway != null ? gateway.TimeSyncAlpha : 0.20,
                timeoutMs: gateway != null ? gateway.TimeSyncTimeoutMs : 2000,
                idealFrameSafetyConstMarginFrames: gateway != null ? gateway.IdealFrameSafetyConstMarginFrames : 2,
                idealFrameSafetyRttFactor: gateway != null ? gateway.IdealFrameSafetyRttFactor : 1.0,
                idealFrameSafetyMinMarginFrames: gateway != null ? gateway.IdealFrameSafetyMinMarginFrames : 0,
                idealFrameSafetyMaxMarginFrames: gateway != null ? gateway.IdealFrameSafetyMaxMarginFrames : 30);

            return new BattleStartPlanOptions(
                worldId: GetEffectiveWorldId(),
                worldType: Preset != null ? Preset.WorldType : WorldType,
                clientId: GetEffectiveClientId(),
                playerId: req.PlayerId.Value,
                tickRate: req.TickRate,
                inputDelayFrames: req.InputDelayFrames,
                hostMode: hostMode,
                syncMode: Preset != null ? Preset.SyncMode : SyncMode,
                viewEventSourceMode: Preset != null ? Preset.ViewEventSourceMode : ViewEventSourceMode,
                enableClientPrediction: Preset != null ? Preset.EnableClientPrediction : EnableClientPrediction,
                enableConfirmedAuthorityWorld: Preset != null ? Preset.EnableConfirmedAuthorityWorld : EnableConfirmedAuthorityWorld,
                enabledSnapshotRegistryIds: Preset != null ? Preset.EnabledSnapshotRegistryIds : EnabledSnapshotRegistryIds,
                gateway: gatewayOptions,
                auto: autoOptions,
                runMode: runModeOptions,
                createWorld: createWorldOptions,
                timeSync: timeSyncOptions,
                launchSpec: launchSpec);
        }

        private int GetEffectiveGameplayId()
        {
            return Preset != null ? Preset.GameplayId : GameplayId;
        }

        private string GetEffectiveWorldId()
        {
            if (RuntimeOverrides != null && RuntimeOverrides.HasWorldId) return RuntimeOverrides.WorldId;
            return Preset != null ? Preset.WorldId : WorldId;
        }

        private string GetEffectiveClientId()
        {
            if (RuntimeOverrides != null && RuntimeOverrides.HasClientId) return RuntimeOverrides.ClientId;
            return Preset != null ? Preset.ClientId : ClientId;
        }

        private MobaBattleLaunchAuthorityMode ResolveAuthorityMode()
        {
            var hostMode = Preset != null ? Preset.HostMode : HostMode;
            var enableClientPrediction = Preset != null ? Preset.EnableClientPrediction : EnableClientPrediction;
            if (hostMode == BattleHostMode.GatewayRemote) return MobaBattleLaunchAuthorityMode.ServerAuthority;
            return enableClientPrediction ? MobaBattleLaunchAuthorityMode.ClientPrediction : MobaBattleLaunchAuthorityMode.LocalAuthority;
        }

        private static MobaBattleLaunchSyncMode ToLaunchSyncMode(BattleSyncMode syncMode)
        {
            return syncMode switch
            {
                BattleSyncMode.Lockstep => MobaBattleLaunchSyncMode.FrameSync,
                BattleSyncMode.SnapshotAuthority => MobaBattleLaunchSyncMode.StateSync,
                BattleSyncMode.HybridPredictReconcile => MobaBattleLaunchSyncMode.Hybrid,
                _ => MobaBattleLaunchSyncMode.Unspecified,
            };
        }

        private static MobaPlayerLoadout[] BuildPlayersLoadout(BattlePlayersConfigSO cfg)
        {
            if (cfg == null) return null;

            var list = new List<MobaPlayerLoadout>(4);

            if (cfg.Team1Players != null)
            {
                for (int i = 0; i < cfg.Team1Players.Count; i++)
                {
                    var p = cfg.Team1Players[i];
                    if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;
                    list.Add(new MobaPlayerLoadout(
                        new PlayerId(p.PlayerId),
                        (int)p.TeamId,
                        p.HeroId,
                        p.AttributeTemplateId,
                        p.Level,
                        p.BasicAttackSkillId,
                        p.SkillIds,
                        p.SpawnIndex,
                        (int)p.UnitSubType,
                        (int)p.MainType,
                        hasSpawnPosition: 1,
                        spawnX: p.SpawnPosition.x,
                        spawnY: p.SpawnPosition.y,
                        spawnZ: p.SpawnPosition.z));
                }
            }

            if (cfg.Team2Players != null)
            {
                for (int i = 0; i < cfg.Team2Players.Count; i++)
                {
                    var p = cfg.Team2Players[i];
                    if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;
                    list.Add(new MobaPlayerLoadout(
                        new PlayerId(p.PlayerId),
                        (int)p.TeamId,
                        p.HeroId,
                        p.AttributeTemplateId,
                        p.Level,
                        p.BasicAttackSkillId,
                        p.SkillIds,
                        p.SpawnIndex,
                        (int)p.UnitSubType,
                        (int)p.MainType,
                        hasSpawnPosition: 1,
                        spawnX: p.SpawnPosition.x,
                        spawnY: p.SpawnPosition.y,
                        spawnZ: p.SpawnPosition.z));
                }
            }

            return list.Count == 0 ? null : list.ToArray();
        }
    }
}

