using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Demo.Moba.Util.Generator;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Protocol.Moba.CreateWorld;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaGameStartFailureCode
    {
        None = 0,
        AlreadyStarted = 1,
        InvalidProtocol = 2,
        MissingActorContext = 3,
        MissingActorEntityInitPipeline = 4,
        ActorBuildFailed = 5,
        InvalidActorBuildResult = 6,
        PublishEnterGameSnapshotFailed = 7,
        PublishSpawnSnapshotFailed = 8,
        MissingGameStartPort = 9,
        MissingGameplayService = 10,
        InvalidGameplayId = 11,
        GameplayStartFailed = 12,
        InvalidGamePhase = 13,
    }

    public readonly struct MobaGameStartResult
    {
        public static readonly MobaGameStartResult Success = new MobaGameStartResult(true, MobaGameStartFailureCode.None, null);

        public readonly bool Succeeded;
        public readonly MobaGameStartFailureCode FailureCode;
        public readonly string Message;

        public MobaGameStartResult(bool succeeded, MobaGameStartFailureCode failureCode, string message)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message;
        }

        public static MobaGameStartResult Fail(MobaGameStartFailureCode failureCode, string message)
        {
            return new MobaGameStartResult(false, failureCode, message);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : $"{FailureCode}: {Message}";
        }
    }

    public interface IMobaGameStartPort : IService
    {
        MobaGameStartResult TryStartGame(in MobaGameStartSpec spec);
    }

    [WorldService(typeof(IMobaGameStartPort))]
    [WorldService(typeof(MobaEnterGameFlowService))]
    public sealed class MobaEnterGameFlowService : IService, IMobaGameStartPort
    {
        [WorldInject] private MobaEnterGameSnapshotService _snapshot;
        [WorldInject] private IWorldContext _worldContext;
        [WorldInject] private global::Entitas.IContexts _contexts;
        [WorldInject] private ActorIdAllocator _actorIds;
        [WorldInject] private MobaActorRegistry _registry;
        [WorldInject] private MobaEntityManager _entities;
        [WorldInject] private MobaPlayerActorMapService _playerActorMap;
        [WorldInject] private MobaSkillLoadoutService _skills;
        [WorldInject(required: false)] private MobaConfigDatabase _config;
        [WorldInject(required: false)] private ActorEntityInitPipeline _generator;
        [WorldInject] private MobaActorSpawnSnapshotService _spawn;
        [WorldInject(required: false)] private MobaGamePhaseService _phase;
        [WorldInject(required: false)] private MobaGameplayService _gameplay;

        private bool _started;

        public MobaGameStartResult TryStartGame(in MobaGameStartSpec spec)
        {
            var actorContext = (_contexts as global::Contexts)?.actor;
            if (actorContext == null)
            {
                return Fail(MobaGameStartFailureCode.MissingActorContext, "ActorContext is null");
            }

            return TryApplyGameStartSpec(actorContext, in spec);
        }

        private MobaGameStartResult TryApplyGameStartSpec(ActorContext actorContext, in MobaGameStartSpec spec)
        {
            if (actorContext == null)
            {
                return Fail(MobaGameStartFailureCode.MissingActorContext, "ActorContext is null");
            }

            if (_started || (_phase != null && !_phase.CanStartGame))
            {
                return Fail(MobaGameStartFailureCode.AlreadyStarted, "game already started or not startable. phase=" + _phase);
            }

            var validation = MobaProtocolValidation.ValidateEnterGameReqEnvelope(in spec.EnterReq);
            if (!validation.IsValid)
            {
                return Fail(MobaGameStartFailureCode.InvalidProtocol, validation.ToString());
            }

            if (_generator == null)
            {
                return Fail(MobaGameStartFailureCode.MissingActorEntityInitPipeline, "ActorEntityInitPipeline not resolved; battle start is blocked to avoid partially initialized actors");
            }

            EnterMobaGameReq effectiveReq;
            try
            {
                effectiveReq = MobaGameStartSpecNormalizer.Normalize(_config, in spec.EnterReq);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEnterGameFlowService] Normalize enter game request failed");
                return Fail(MobaGameStartFailureCode.InvalidProtocol, ex.Message);
            }

            var effectiveValidation = MobaProtocolValidation.ValidateEnterGameReq(in effectiveReq);
            if (!effectiveValidation.IsValid)
            {
                return Fail(MobaGameStartFailureCode.InvalidProtocol, $"normalized request is invalid. {effectiveValidation}");
            }

            var initializing = TrySetPhase(MobaGamePhase.Initializing, "enter game actor build started");
            if (!initializing.Succeeded)
            {
                return initializing;
            }

            Log.Info($"[MobaEnterGameFlowService] TryStartGame: begin (players={(effectiveReq.Players != null ? effectiveReq.Players.Length : 0)}, playerId={effectiveReq.PlayerId.Value})");

            var spawnEntries = new List<MobaActorSpawnSnapshotEntry>(effectiveReq.Players != null ? effectiveReq.Players.Length : 4);
            BuildActorsResult built;

            try
            {
                built = ActorSpawnPipeline.BuildActorsFromEnterGameReqAndInitialize(
                    actorContext,
                    _actorIds,
                    _registry,
                    _entities,
                    effectiveReq,
                    initializer: (entity, loadout) =>
                    {
                        _generator.InitializeFromLoadout(entity, loadout);
                    },
                    onActorBuilt: (entity, loadout) =>
                    {
                        var actorId = entity != null && entity.hasActorId ? entity.actorId.Value : 0;
                        if (actorId <= 0)
                        {
                            throw new InvalidOperationException($"actor id is invalid after build. playerId={loadout.PlayerId.Value}, heroId={loadout.HeroId}");
                        }

                        spawnEntries.Add(new MobaActorSpawnSnapshotEntry
                        {
                            NetId = actorId,
                            Kind = (int)SpawnEntityKind.Character,
                            Code = loadout.HeroId,
                            OwnerNetId = 0,
                            X = loadout.SpawnX,
                            Y = loadout.SpawnY,
                            Z = loadout.SpawnZ
                        });
                    });
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEnterGameFlowService] BuildEnterGameActors failed");
                return FailAfterStart(MobaGameStartFailureCode.ActorBuildFailed, ex.Message);
            }

            var buildValidation = ValidateBuildResult(in built, effectiveReq.Players.Length);
            if (!buildValidation.Succeeded)
            {
                return FailAfterStart(buildValidation);
            }

            Log.Info($"[MobaEnterGameFlowService] TryStartGame: BuildEnterGameActors done (localActorId={built.LocalActorId})");

            var bindResult = BindPlayerActors(built.PlayerActors);
            if (!bindResult.Succeeded)
            {
                return FailAfterStart(bindResult);
            }

            var p = built.LocalActorTransform.Position;
            var payload = MobaEnterGamePayloadCodec.Serialize(in p);

            var res = new EnterMobaGameRes(
                worldId: _worldContext.Id,
                playerId: effectiveReq.PlayerId,
                localActorId: built.LocalActorId,
                randomSeed: effectiveReq.RandomSeed,
                tickRate: effectiveReq.TickRate,
                inputDelayFrames: effectiveReq.InputDelayFrames,
                players: built.Players,
                opCode: MobaEnterGamePayloadCodec.PayloadOpCode,
                payload: payload,
                playersLoadout: effectiveReq.Players
            );

            try
            {
                _snapshot.PublishEnterGameResPayload(EnterMobaGameCodec.SerializeRes(res));
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEnterGameFlowService] publish enter-game snapshot failed");
                return FailAfterStart(MobaGameStartFailureCode.PublishEnterGameSnapshotFailed, ex.Message);
            }

            try
            {
                var payload2 = MobaActorSpawnSnapshotCodec.Serialize(spawnEntries.ToArray());
                _spawn.PublishSpawnPayload(payload2);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEnterGameFlowService] publish spawn payload failed");
                return FailAfterStart(MobaGameStartFailureCode.PublishSpawnSnapshotFailed, ex.Message);
            }

            var ready = TrySetPhase(MobaGamePhase.Ready, "enter game data prepared");
            if (!ready.Succeeded)
            {
                return FailAfterStart(ready);
            }

            var gameplayStart = StartGameplay(effectiveReq.GameplayId);
            if (!gameplayStart.Succeeded)
            {
                return FailAfterStart(gameplayStart);
            }

            var inGame = TrySetPhase(MobaGamePhase.InGame, "gameplay started");
            if (!inGame.Succeeded)
            {
                return FailAfterStart(inGame);
            }

            _started = true;
            return MobaGameStartResult.Success;
        }

        private MobaGameStartResult FailAfterStart(MobaGameStartFailureCode failureCode, string message)
        {
            var result = Fail(failureCode, message);
            TrySetFaulted("game start failed. " + result);
            return result;
        }

        private MobaGameStartResult FailAfterStart(in MobaGameStartResult result)
        {
            TrySetFaulted("game start failed. " + result);
            return result;
        }

        private MobaGameStartResult TrySetPhase(MobaGamePhase phase, string reason)
        {
            if (_phase == null) return MobaGameStartResult.Success;

            var result = _phase.TryTransition(phase, reason);
            if (result.Succeeded) return MobaGameStartResult.Success;

            return Fail(MobaGameStartFailureCode.InvalidGamePhase, result.ToString());
        }

        private void TrySetFaulted(string reason)
        {
            if (_phase == null) return;

            var result = _phase.TryTransition(MobaGamePhase.Faulted, reason);
            if (!result.Succeeded)
            {
                MobaRuntimeLog.Warning(MobaRuntimeLogModule.Session, MobaRuntimeLogPurpose.Validation, nameof(MobaEnterGameFlowService), "Failed to mark game phase as faulted. " + result);
            }
        }

        private static MobaGameStartResult ValidateBuildResult(in BuildActorsResult built, int expectedPlayerCount)
        {
            if (built.LocalActorId <= 0)
            {
                return Fail(MobaGameStartFailureCode.InvalidActorBuildResult, $"local actor id is invalid, actual={built.LocalActorId}");
            }

            if (built.Players == null || built.Players.Length != expectedPlayerCount)
            {
                return Fail(MobaGameStartFailureCode.InvalidActorBuildResult, $"player entry count mismatch, expected={expectedPlayerCount}, actual={(built.Players != null ? built.Players.Length : 0)}");
            }

            if (built.PlayerActors == null || built.PlayerActors.Length != expectedPlayerCount)
            {
                return Fail(MobaGameStartFailureCode.InvalidActorBuildResult, $"player actor count mismatch, expected={expectedPlayerCount}, actual={(built.PlayerActors != null ? built.PlayerActors.Length : 0)}");
            }

            return MobaGameStartResult.Success;
        }

        private static MobaGameStartResult Fail(MobaGameStartFailureCode failureCode, string message)
        {
            var result = MobaGameStartResult.Fail(failureCode, message);
            Log.Error($"[MobaEnterGameFlowService] ApplyGameStartSpec failed. {result}");
            return result;
        }

        private MobaGameStartResult StartGameplay(int gameplayId)
        {
            if (_gameplay == null)
            {
                return Fail(MobaGameStartFailureCode.MissingGameplayService, "MobaGameplayService is required to start battle gameplay.");
            }

            if (gameplayId <= 0)
            {
                return Fail(MobaGameStartFailureCode.InvalidGameplayId, $"gameplay id must be positive for formal battle start. gameplayId={gameplayId}");
            }

            _gameplay.Start(gameplayId);
            if (!_gameplay.IsRunning || _gameplay.CurrentGameplayId != gameplayId)
            {
                return Fail(MobaGameStartFailureCode.GameplayStartFailed, $"gameplay start failed. gameplayId={gameplayId}, phase={_gameplay.Phase}, currentGameplayId={_gameplay.CurrentGameplayId}");
            }

            return MobaGameStartResult.Success;
        }

        private MobaGameStartResult BindPlayerActors(MobaPlayerActorEntry[] playerActors)
        {
            if (playerActors == null)
            {
                return Fail(MobaGameStartFailureCode.InvalidActorBuildResult, "player actor entries are null");
            }

            for (int i = 0; i < playerActors.Length; i++)
            {
                var entry = playerActors[i];
                if (entry.ActorId <= 0)
                {
                    return Fail(MobaGameStartFailureCode.InvalidActorBuildResult, $"player actor id is invalid. index={i}, playerId={entry.PlayerId.Value}, actorId={entry.ActorId}");
                }

                _playerActorMap.Bind(entry.PlayerId, entry.ActorId);
            }

            return MobaGameStartResult.Success;
        }

        public void Dispose()
        {
        }
    }
}
