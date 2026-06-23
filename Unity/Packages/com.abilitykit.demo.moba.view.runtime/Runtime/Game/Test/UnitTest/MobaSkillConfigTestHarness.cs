using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Core.Mathematics;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Protocol.Moba;
using AbilityKit.Trace;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    /// <summary>
    /// Reusable EditMode fixture for config-driven MOBA skill tests.
    /// It owns a headless moba.view runtime world and exposes common config, input and trace assertions
    /// so new skill, condition and trigger-action tests can focus on ids and expected behavior.
    /// </summary>
    public sealed class MobaSkillConfigTestHarness : IDisposable
    {
        public const float DefaultFixedDelta = 1f / 30f;
        public const string DefaultWorldType = "battle";
        public const string DefaultPlayerId = "p1";

        private bool _disposed;
        private readonly Dictionary<string, int> _aliasToActorId = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerId> _aliasToPlayerId = new Dictionary<string, PlayerId>(StringComparer.Ordinal);
        private readonly MobaAcceptanceScenarioExpectation _scenario;
        private readonly MobaAcceptanceActorExpectation[] _scenarioActors;

        private MobaSkillConfigTestHarness(IWorld world, PlayerId playerId, float fixedDelta, int tickRate, MobaAcceptanceScenarioExpectation scenario = null, MobaAcceptanceActorExpectation[] scenarioActors = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            PlayerId = playerId;
            FixedDelta = fixedDelta;
            TickRate = tickRate;
            _scenario = scenario;
            _scenarioActors = scenarioActors ?? Array.Empty<MobaAcceptanceActorExpectation>();
            _aliasToPlayerId[playerId.Value] = playerId;
            CacheScenarioAliases(_scenarioActors);
        }

        public IWorld World { get; }
        public PlayerId PlayerId { get; }
        public float FixedDelta { get; }
        public int TickRate { get; }
        public MobaAcceptanceScenarioExpectation Scenario => _scenario;
        public MobaAcceptanceActorExpectation[] ScenarioActors => _scenarioActors;
        public IReadOnlyDictionary<string, int> ActorAliases => _aliasToActorId;
        public IReadOnlyDictionary<string, PlayerId> PlayerAliases => _aliasToPlayerId;

        public MobaConfigDatabase Config => World.Services.Resolve<MobaConfigDatabase>();
        public MobaTraceRegistry Trace => World.Services.Resolve<MobaTraceRegistry>();
        public TriggerPlanJsonDatabase TriggerPlans => World.Services.Resolve<TriggerPlanJsonDatabase>();
        public IFrameTime FrameTime => World.Services.Resolve<IFrameTime>();

        public static MobaSkillConfigTestHarness CreateForSinglePlayer(
            int[] skillIds,
            string worldId = "skill_config_test_world",
            string worldType = DefaultWorldType,
            string playerId = DefaultPlayerId,
            int attributeTemplateId = 1001,
            int heroId = 1,
            int tickRate = 30,
            int inputDelayFrames = 0,
            float fixedDelta = DefaultFixedDelta)
        {
            if (skillIds == null) throw new ArgumentNullException(nameof(skillIds));

            var typedPlayerId = new PlayerId(playerId);
            var world = CreateHeadlessMobaWorld(
                worldId: new WorldId(worldId),
                worldType: worldType,
                playerId: typedPlayerId,
                players: new[]
                {
                    new MobaPlayerLoadout(
                        typedPlayerId,
                        teamId: 1,
                        heroId: heroId,
                        attributeTemplateId: attributeTemplateId,
                        level: 1,
                        basicAttackSkillId: 1,
                        skillIds: skillIds,
                        spawnIndex: 0,
                        unitSubType: (int)UnitSubType.Hero,
                        mainType: (int)EntityMainType.Unit,
                        hasSpawnPosition: 1,
                        spawnX: 0f,
                        spawnY: 0f,
                        spawnZ: 0f)
                },
                tickRate: tickRate,
                inputDelayFrames: inputDelayFrames);

            return new MobaSkillConfigTestHarness(world, typedPlayerId, fixedDelta, tickRate);
        }

        public static MobaSkillConfigTestHarness CreateForScenario(
            MobaAcceptanceExpectation expectation,
            string worldId = null,
            string worldType = DefaultWorldType,
            string playerId = null,
            int tickRate = 0,
            int inputDelayFrames = 0,
            float fixedDelta = DefaultFixedDelta)
        {
            if (expectation == null) throw new ArgumentNullException(nameof(expectation));

            var scenario = expectation.scenario;
            var scenarioActors = GetScenarioActors(expectation);
            var resolvedWorldId = !string.IsNullOrEmpty(worldId)
                ? worldId
                : !string.IsNullOrEmpty(scenario?.worldId)
                    ? scenario.worldId
                    : !string.IsNullOrEmpty(expectation.worldId)
                        ? expectation.worldId
                        : (string.IsNullOrEmpty(expectation.caseId) ? "moba_acceptance_world" : expectation.caseId + "_world");
            var resolvedTickRate = tickRate > 0
                ? tickRate
                : scenario != null && scenario.tickRate > 0
                    ? scenario.tickRate
                    : expectation.tickRate > 0
                        ? expectation.tickRate
                        : 30;
            var resolvedPlayerId = !string.IsNullOrEmpty(playerId)
                ? playerId
                : ResolvePrimaryPlayerId(expectation, scenarioActors);
            var typedPlayerId = new PlayerId(resolvedPlayerId);
            var players = BuildPlayerLoadouts(expectation, scenarioActors, typedPlayerId, inputDelayFrames);
            var world = CreateHeadlessMobaWorld(
                worldId: new WorldId(resolvedWorldId),
                worldType: worldType,
                playerId: typedPlayerId,
                players: players,
                tickRate: resolvedTickRate,
                inputDelayFrames: inputDelayFrames);

            return new MobaSkillConfigTestHarness(world, typedPlayerId, fixedDelta, resolvedTickRate, scenario, scenarioActors);
        }

        public void EnterGameAndWarmup(int warmupTicks = 3, string reason = "editmode skill config test")
        {
            var phase = World.Services.Resolve<MobaLogicWorldRunGateService>();
            phase.SetInGame(reason);
            Tick(warmupTicks);
            RefreshScenarioActorAliases();
        }

        public bool TryGetActorId(string alias, out int actorId)
        {
            if (string.IsNullOrEmpty(alias))
            {
                actorId = 0;
                return false;
            }

            return _aliasToActorId.TryGetValue(alias, out actorId) && actorId > 0;
        }

        public bool TryGetPlayerId(string alias, out PlayerId playerId)
        {
            if (string.IsNullOrEmpty(alias))
            {
                playerId = default;
                return false;
            }

            return _aliasToPlayerId.TryGetValue(alias, out playerId) && !string.IsNullOrEmpty(playerId.Value);
        }

        public int AssertActorId(string alias, string message = null)
        {
            Assert.IsTrue(TryGetActorId(alias, out var actorId), message ?? $"Actor alias missing: {alias}");
            return actorId;
        }

        public void Tick(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                World.Tick(FixedDelta);
            }
        }

        public void TickSeconds(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Accelerated test delta time must be finite and non-negative.");
            }

            if (deltaTime <= 0f) return;
            World.Tick(deltaTime);
        }

        public void TickMilliseconds(int milliseconds)
        {
            if (milliseconds <= 0) return;
            TickSeconds(milliseconds / 1000f);
        }

        public int AssertPlayerActorBound(string message = null)
        {
            var playerActorMap = World.Services.Resolve<MobaPlayerActorMapService>();
            Assert.IsTrue(playerActorMap.TryGetActorId(PlayerId, out var actorId), message ?? $"Player actor binding missing: {PlayerId.Value}");
            return actorId;
        }

        public int AssertPlayerActorBound(PlayerId playerId, string message = null)
        {
            var playerActorMap = World.Services.Resolve<MobaPlayerActorMapService>();
            Assert.IsTrue(playerActorMap.TryGetActorId(playerId, out var actorId), message ?? $"Player actor binding missing: {playerId.Value}");
            return actorId;
        }

        public void AssertSlotSkill(int slot, int expectedSkillId)
        {
            var actorId = AssertPlayerActorBound();
            var loadout = World.Services.Resolve<MobaSkillLoadoutService>();
            Assert.IsTrue(loadout.TryGetSkillId(actorId, slot, out var slotSkillId), $"Slot {slot} skill missing for actor {actorId}.");
            Assert.AreEqual(expectedSkillId, slotSkillId);
        }

        public void AssertSkillUsesCastFlow(int skillId, int expectedCastFlowId)
        {
            Assert.IsTrue(Config.TryGetSkill(skillId, out var skill), $"Skill config missing: {skillId}");
            Assert.AreEqual(expectedCastFlowId, skill.CastFlowId);
        }

        public void AssertCastFlowContainsTimelineEffect(int castFlowId, int expectedEffectId)
        {
            Assert.IsTrue(Config.TryGetSkillFlow(castFlowId, out var flow), $"Skill flow config missing: {castFlowId}");
            Assert.IsTrue(ContainsTimelineEffect(flow.Phases, expectedEffectId), $"Skill flow {castFlowId} must contain timeline effect {expectedEffectId}.");
        }

        public void AssertProjectileConfigExists(int launcherId, int projectileId)
        {
            Assert.IsTrue(Config.TryGetProjectileLauncher(launcherId, out var launcher), $"Projectile launcher config missing: {launcherId}");
            Assert.IsNotNull(launcher);
            Assert.IsTrue(Config.TryGetProjectile(projectileId, out var projectile), $"Projectile config missing: {projectileId}");
            Assert.IsNotNull(projectile);
        }

        public TriggerPlan<object> AssertTriggerPlanContainsActions(int triggerId, params int[] actionIds)
        {
            Assert.IsTrue(TriggerPlans.TryGetPlanByTriggerId(triggerId, out var triggerPlan), $"Trigger plan missing for effect {triggerId}.");
            for (var i = 0; i < actionIds.Length; i++)
            {
                AssertPlanContainsAction(triggerPlan, actionIds[i], $"Effect {triggerId} must compile trigger action {actionIds[i]}.");
            }

            return triggerPlan;
        }

        public FrameIndex SubmitSkillPress(int slot)
        {
            return SubmitSkillInput(PlayerId, slot, SkillInputPhase.Press, targetActorId: 0, aimPos: default, aimDir: default);
        }

        public FrameIndex SubmitSkillInput(PlayerId playerId, int slot, SkillInputPhase phase, int targetActorId = 0, Vec3 aimPos = default, Vec3 aimDir = default)
        {
            var input = World.Services.Resolve<IWorldInputSink>();
            var castFrame = new FrameIndex(FrameTime.Frame.Value + 1);
            var skillInput = new SkillInputEvent(slot: slot, phase: phase, targetActorId: targetActorId, aimPos: in aimPos, aimDir: in aimDir);
            var command = new PlayerInputCommand(castFrame, playerId, MobaOpCodes.Input.SkillInput, SkillInputCodec.Serialize(in skillInput));
            input.Submit(castFrame, new[] { command });
            return castFrame;
        }

        public FrameIndex SubmitSkillInput(string actorAlias, int slot, string phase, string targetAlias = null, int targetActorId = 0, MobaAcceptanceVector3Expectation position = null, MobaAcceptanceVector3Expectation direction = null)
        {
            Assert.IsTrue(TryGetPlayerId(actorAlias, out var playerId), $"Actor alias {actorAlias} is not bound to a player input source.");
            var resolvedTargetActorId = targetActorId;
            if (resolvedTargetActorId <= 0 && !string.IsNullOrEmpty(targetAlias))
            {
                Assert.IsTrue(TryGetActorId(targetAlias, out resolvedTargetActorId), $"Target actor alias missing: {targetAlias}");
            }

            var aimPos = ToVec3(position);
            var aimDir = ToVec3(direction);
            return SubmitSkillInput(playerId, slot, ParseSkillInputPhase(phase), resolvedTargetActorId, aimPos, aimDir);
        }

        public void CastSkillSlotAndTick(int slot, int postCastTicks = 25)
        {
            SubmitSkillPress(slot);
            Tick(postCastTicks);
        }

        public TraceSnapshot<MobaTraceMetadata> CastSkillSlotAndTickUntilEffect(int slot, int skillId, int effectId, int safetyFrames = 5, int maxExtraFrames = 30)
        {
            var effectTimeMs = CalculateWaitMillisecondsForSkillEffect(skillId, effectId);
            var safetySeconds = Math.Max(0, safetyFrames) * FixedDelta;
            var maxTicks = CalculateWaitTicksForSkillEffect(skillId, effectId, safetyFrames) + maxExtraFrames;

            SubmitSkillPress(slot);

            // Keep input ordering frame-accurate: the command is submitted for Frame + 1,
            // so the first tick uses the normal fixed delta to let the cast enter runtime systems.
            Tick(1);
            if (TryFindTraceNode(MobaTraceKind.EffectExecution, effectId, out var existing)) return existing;

            // Unit tests do not need to spend N fixed frames to reach a delayed timeline point.
            // Convert the configured wait into elapsed seconds and advance the runtime once with
            // the equivalent delta. A small fixed-frame safety window remains as a fallback for
            // side effects that flush on the following frame.
            var acceleratedSeconds = Math.Max(0f, effectTimeMs / 1000f - FixedDelta) + safetySeconds;
            TickSeconds(acceleratedSeconds);
            if (TryFindTraceNode(MobaTraceKind.EffectExecution, effectId, out var accelerated)) return accelerated;

            return TickUntilTraceNode(MobaTraceKind.EffectExecution, effectId, maxExtraFrames, $"EffectExecution trace missing for effect {effectId} after casting skill {skillId} slot {slot} within accelerated {effectTimeMs} ms plus {maxExtraFrames} fallback ticks; fixed-frame equivalent timeout was {maxTicks} ticks.");
        }

        public TraceSnapshot<MobaTraceMetadata> TickUntilTraceNode(MobaTraceKind kind, int configId, int maxTicks, string message)
        {
            if (TryFindTraceNode(kind, configId, out var existing)) return existing;

            for (var i = 0; i < maxTicks; i++)
            {
                Tick(1);
                if (TryFindTraceNode(kind, configId, out var node)) return node;
            }

            Assert.Fail(message);
            return default;
        }

        public TraceSnapshot<MobaTraceMetadata> TickUntilTraceNodeInRoot(long rootId, MobaTraceKind kind, int configId, int maxTicks, string message)
        {
            if (TryFindTraceNodeInRoot(rootId, kind, configId, out var existing)) return existing;

            for (var i = 0; i < maxTicks; i++)
            {
                Tick(1);
                if (TryFindTraceNodeInRoot(rootId, kind, configId, out var node)) return node;
            }

            Assert.Fail(message);
            return default;
        }

        public int CalculateWaitTicksForSkillEffect(int skillId, int effectId, int safetyFrames = 5)
        {
            var effectTimeMs = CalculateWaitMillisecondsForSkillEffect(skillId, effectId);
            return MillisecondsToTicks(effectTimeMs) + Math.Max(0, safetyFrames);
        }

        public int CalculateWaitTicksForCastFlowEffect(int castFlowId, int effectId, int safetyFrames = 5)
        {
            var effectTimeMs = CalculateWaitMillisecondsForCastFlowEffect(castFlowId, effectId);
            return MillisecondsToTicks(effectTimeMs) + Math.Max(0, safetyFrames);
        }

        public int CalculateWaitMillisecondsForSkillEffect(int skillId, int effectId)
        {
            Assert.IsTrue(Config.TryGetSkill(skillId, out var skill), $"Skill config missing: {skillId}");
            return CalculateWaitMillisecondsForCastFlowEffect(skill.CastFlowId, effectId);
        }

        public int CalculateWaitMillisecondsForCastFlowEffect(int castFlowId, int effectId)
        {
            Assert.IsTrue(Config.TryGetSkillFlow(castFlowId, out var flow), $"Skill flow config missing: {castFlowId}");
            Assert.IsTrue(TryGetEarliestEffectTimeMs(flow.Phases, effectId, out var effectTimeMs), $"Skill flow {castFlowId} must contain timeline effect {effectId}.");
            return Math.Max(0, effectTimeMs);
        }

        public TraceSnapshot<MobaTraceMetadata> AssertSkillCastTrace(int skillId)
        {
            return AssertTraceNode(MobaTraceKind.SkillCast, skillId, $"SkillCast trace missing for skill {skillId}.");
        }

        public TraceSnapshot<MobaTraceMetadata> AssertEffectExecutionTrace(int effectId)
        {
            return AssertTraceNode(MobaTraceKind.EffectExecution, effectId, $"EffectExecution trace missing for configured effect {effectId}.");
        }

        public TraceSnapshot<MobaTraceMetadata> AssertActionExecutedUnderEffect(long effectRootId, int actionId, string actionName = null)
        {
            var displayName = string.IsNullOrEmpty(actionName) ? actionId.ToString() : $"{actionName}({actionId})";
            return AssertTraceNodeInRoot(effectRootId, MobaTraceKind.EffectAction, actionId, $"Trigger action {displayName} was not executed under effect root {effectRootId}.");
        }

        public TraceSnapshot<MobaTraceMetadata> AssertProjectileLaunchedUnderEffect(long effectRootId, int launcherId, int projectileId)
        {
            return AssertTraceNodeInRoot(effectRootId, MobaTraceKind.ProjectileLaunch, projectileId, $"shoot_projectile did not launch configured projectile {projectileId} from launcher {launcherId}.");
        }

        public TraceSnapshot<MobaTraceMetadata> AssertTraceNode(MobaTraceKind kind, int configId, string message)
        {
            if (TryFindTraceNode(kind, configId, out var node)) return node;

            Assert.Fail(message);
            return default;
        }

        public TraceSnapshot<MobaTraceMetadata> AssertTraceNodeInRoot(long rootId, MobaTraceKind kind, int configId, string message)
        {
            if (TryFindTraceNodeInRoot(rootId, kind, configId, out var node)) return node;

            Assert.Fail(message);
            return default;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            World.Dispose();
        }

        private static IWorld CreateHeadlessMobaWorld(
            WorldId worldId,
            string worldType,
            PlayerId playerId,
            MobaPlayerLoadout[] players,
            int tickRate,
            int inputDelayFrames)
        {
            var registry = new WorldTypeRegistry().RegisterEntitasWorld(worldType);
            var manager = new WorldManager(new RegistryWorldFactory(registry));
            var launchSpec = new MobaBattleLaunchSpec(
                battleId: worldId.Value,
                matchId: worldId.Value,
                worldId: worldId.Value,
                worldType: worldType,
                clientId: "editmode_skill_config_test",
                localPlayerId: playerId,
                mapId: 1,
                gameplayId: 0,
                ruleSetId: 0,
                configVersion: 0,
                protocolVersion: 0,
                randomSeed: 123,
                tickRate: tickRate,
                inputDelayFrames: inputDelayFrames,
                launchMode: MobaBattleLaunchMode.ViewFastEnter,
                syncMode: MobaBattleLaunchSyncMode.Hybrid,
                authorityMode: MobaBattleLaunchAuthorityMode.LocalAuthority,
                players: players,
                enterGamePayload: Array.Empty<byte>());

            var builder = WorldServiceContainerFactory.CreateWithAttributes(
                WorldServiceProfile.All,
                new[]
                {
                    typeof(WorldServiceContainerFactory).Assembly,
                    typeof(MobaWorldBootstrapModule).Assembly,
                    typeof(MobaSkillConfigTestHarness).Assembly
                },
                new[] { "AbilityKit" });

            builder.AddModule(new MobaConfigWorldModule());
            builder.RegisterInstance(launchSpec.ToWorldInitData(MobaWorldBootstrapModule.InitOpCode));
            builder.TryRegister<IFrameTime>(WorldLifetime.Singleton, _ => new FrameTime());

            var options = new WorldCreateOptions(worldId, worldType)
            {
                ServiceBuilder = builder,
            };
            options.SetEntitasContextsFactory(new MobaEntitasContextsFactory());
            return manager.Create(options);
        }

        private bool TryFindTraceNode(MobaTraceKind kind, int configId, out TraceSnapshot<MobaTraceMetadata> match)
        {
            foreach (var node in Trace.GetNodesByKind((int)kind))
            {
                if (node.Metadata != null && node.Metadata.ConfigId == configId)
                {
                    match = node;
                    return true;
                }
            }

            match = default;
            return false;
        }

        private bool TryFindTraceNodeInRoot(long rootId, MobaTraceKind kind, int configId, out TraceSnapshot<MobaTraceMetadata> match)
        {
            foreach (var node in Trace.GetNodesByRoot(rootId))
            {
                if (node.Kind == (int)kind && node.Metadata != null && node.Metadata.ConfigId == configId)
                {
                    match = node;
                    return true;
                }
            }

            match = default;
            return false;
        }

        private int MillisecondsToTicks(int milliseconds)
        {
            if (milliseconds <= 0) return 1;
            return Math.Max(1, (int)Math.Ceiling(milliseconds / 1000d * Math.Max(1, TickRate)));
        }

        private static bool ContainsTimelineEffect(System.Collections.Generic.IReadOnlyList<SkillPhaseDTO> phases, int effectId)
        {
            return TryGetEarliestEffectTimeMs(phases, effectId, out _);
        }

        private static bool TryGetEarliestEffectTimeMs(System.Collections.Generic.IReadOnlyList<SkillPhaseDTO> phases, int effectId, out int earliestMs)
        {
            return TryGetEarliestEffectTimeMs(phases, effectId, 0, out earliestMs, out _);
        }

        private static bool TryGetEarliestEffectTimeMs(System.Collections.Generic.IReadOnlyList<SkillPhaseDTO> phases, int effectId, int startMs, out int earliestMs, out int durationMs)
        {
            earliestMs = int.MaxValue;
            durationMs = 0;
            if (phases == null) return false;

            var found = false;
            var cursorMs = startMs;
            for (var i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                if (phase == null) continue;

                if (TryGetPhaseEffectTimeMs(phase, effectId, cursorMs, out var phaseEarliestMs, out var phaseDurationMs))
                {
                    found = true;
                    if (phaseEarliestMs < earliestMs) earliestMs = phaseEarliestMs;
                }

                cursorMs += phaseDurationMs;
                durationMs += phaseDurationMs;
            }

            return found;
        }

        private static bool TryGetPhaseEffectTimeMs(SkillPhaseDTO phase, int effectId, int startMs, out int earliestMs, out int durationMs)
        {
            earliestMs = int.MaxValue;
            durationMs = 0;
            if (phase == null) return false;

            switch ((SkillPhaseType)phase.Type)
            {
                case SkillPhaseType.Timeline:
                    return TryGetTimelineEffectTimeMs(phase.Timeline, effectId, startMs, out earliestMs, out durationMs);
                case SkillPhaseType.Delay:
                    durationMs = phase.Delay != null ? Math.Max(0, phase.Delay.DelayMs) : 0;
                    return false;
                case SkillPhaseType.Sequence:
                    return TryGetEarliestEffectTimeMs(phase.Children, effectId, startMs, out earliestMs, out durationMs);
                case SkillPhaseType.Parallel:
                    return TryGetParallelEffectTimeMs(phase.Children, effectId, startMs, out earliestMs, out durationMs);
                case SkillPhaseType.Repeat:
                    return TryGetRepeatEffectTimeMs(phase.Repeat, effectId, startMs, out earliestMs, out durationMs);
                case SkillPhaseType.RulePlan:
                    durationMs = 0;
                    return false;
                default:
                    durationMs = 0;
                    return false;
            }
        }

        private static bool TryGetTimelineEffectTimeMs(SkillTimelinePhaseDTO timeline, int effectId, int startMs, out int earliestMs, out int durationMs)
        {
            earliestMs = int.MaxValue;
            durationMs = timeline != null ? Math.Max(0, timeline.DurationMs) : 0;
            var found = false;
            var events = timeline != null ? timeline.Events : null;
            if (events == null) return false;

            for (var i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (e == null) continue;

                var atMs = Math.Max(0, e.AtMs);
                if (atMs > durationMs) durationMs = atMs;
                if (e.EffectId != effectId) continue;

                found = true;
                var absoluteMs = startMs + atMs;
                if (absoluteMs < earliestMs) earliestMs = absoluteMs;
            }

            return found;
        }

        private static bool TryGetParallelEffectTimeMs(System.Collections.Generic.IReadOnlyList<SkillPhaseDTO> children, int effectId, int startMs, out int earliestMs, out int durationMs)
        {
            earliestMs = int.MaxValue;
            durationMs = 0;
            if (children == null) return false;

            var found = false;
            for (var i = 0; i < children.Count; i++)
            {
                if (TryGetPhaseEffectTimeMs(children[i], effectId, startMs, out var childEarliestMs, out var childDurationMs))
                {
                    found = true;
                    if (childEarliestMs < earliestMs) earliestMs = childEarliestMs;
                }

                if (childDurationMs > durationMs) durationMs = childDurationMs;
            }

            return found;
        }

        private static bool TryGetRepeatEffectTimeMs(SkillRepeatPhaseDTO repeat, int effectId, int startMs, out int earliestMs, out int durationMs)
        {
            earliestMs = int.MaxValue;
            durationMs = 0;
            if (repeat == null || repeat.Phase == null || repeat.RepeatCount <= 0) return false;

            var intervalMs = Math.Max(0, repeat.IntervalMs);
            var found = false;
            var cursorMs = startMs;
            for (var i = 0; i < repeat.RepeatCount; i++)
            {
                if (TryGetPhaseEffectTimeMs(repeat.Phase, effectId, cursorMs, out var childEarliestMs, out var childDurationMs))
                {
                    found = true;
                    if (childEarliestMs < earliestMs) earliestMs = childEarliestMs;
                }

                cursorMs += childDurationMs;
                if (i < repeat.RepeatCount - 1) cursorMs += intervalMs;
                durationMs += childDurationMs;
                if (i < repeat.RepeatCount - 1) durationMs += intervalMs;
            }

            return found;
        }

        private void CacheScenarioAliases(MobaAcceptanceActorExpectation[] scenarioActors)
        {
            if (scenarioActors == null) return;

            for (var i = 0; i < scenarioActors.Length; i++)
            {
                var actor = scenarioActors[i];
                if (actor == null) continue;

                if (!string.IsNullOrEmpty(actor.alias))
                {
                    if (!string.IsNullOrEmpty(actor.playerId))
                    {
                        var typedPlayerId = new PlayerId(actor.playerId);
                        _aliasToPlayerId[actor.alias] = typedPlayerId;
                        _aliasToPlayerId[actor.playerId] = typedPlayerId;
                    }

                    if (TryParseActorId(actor.actorId, out var actorId))
                    {
                        _aliasToActorId[actor.alias] = actorId;
                    }
                }

                if (!string.IsNullOrEmpty(actor.playerId))
                {
                    var typedPlayerId = new PlayerId(actor.playerId);
                    _aliasToPlayerId[actor.playerId] = typedPlayerId;
                    if (TryParseActorId(actor.actorId, out var actorId))
                    {
                        _aliasToActorId[actor.playerId] = actorId;
                    }
                }
            }
        }

        public void RefreshScenarioActorAliases()
        {
            if (_scenarioActors == null || _scenarioActors.Length == 0) return;

            var playerActorMap = World.Services.Resolve<MobaPlayerActorMapService>();
            for (var i = 0; i < _scenarioActors.Length; i++)
            {
                var actor = _scenarioActors[i];
                if (actor == null || string.IsNullOrEmpty(actor.playerId)) continue;

                var typedPlayerId = new PlayerId(actor.playerId);
                if (!string.IsNullOrEmpty(actor.alias)) _aliasToPlayerId[actor.alias] = typedPlayerId;
                _aliasToPlayerId[actor.playerId] = typedPlayerId;
                if (playerActorMap.TryGetActorId(typedPlayerId, out var actorId) && actorId > 0)
                {
                    if (!string.IsNullOrEmpty(actor.alias)) _aliasToActorId[actor.alias] = actorId;
                    _aliasToActorId[actor.playerId] = actorId;
                }
            }
        }

        private static string ResolvePrimaryPlayerId(MobaAcceptanceExpectation expectation, MobaAcceptanceActorExpectation[] scenarioActors)
        {
            if (expectation != null && expectation.input != null && !string.IsNullOrEmpty(expectation.input.playerId)) return expectation.input.playerId;

            if (scenarioActors != null)
            {
                for (var i = 0; i < scenarioActors.Length; i++)
                {
                    var actor = scenarioActors[i];
                    if (actor != null && !string.IsNullOrEmpty(actor.playerId)) return actor.playerId;
                }
            }

            return DefaultPlayerId;
        }

        private static MobaPlayerLoadout[] BuildPlayerLoadouts(MobaAcceptanceExpectation expectation, MobaAcceptanceActorExpectation[] scenarioActors, PlayerId fallbackPlayerId, int inputDelayFrames)
        {
            var players = new List<MobaPlayerLoadout>();
            if (scenarioActors != null)
            {
                for (var i = 0; i < scenarioActors.Length; i++)
                {
                    var actor = scenarioActors[i];
                    if (actor == null || string.IsNullOrEmpty(actor.playerId)) continue;

                    var skillIds = ResolveActorSkillIds(expectation, actor);
                    var spawnPosition = actor.spawnPosition;
                    var hasSpawnPosition = actor.hasSpawnPosition || spawnPosition != null ? 1 : 0;
                    players.Add(new MobaPlayerLoadout(
                        new PlayerId(actor.playerId),
                        teamId: actor.teamId > 0 ? actor.teamId : 1,
                        heroId: actor.heroId > 0 ? actor.heroId : 1,
                        attributeTemplateId: actor.attributeTemplateId > 0 ? actor.attributeTemplateId : 1001,
                        level: actor.level > 0 ? actor.level : 1,
                        basicAttackSkillId: actor.basicAttackSkillId > 0 ? actor.basicAttackSkillId : 1,
                        skillIds: skillIds,
                        spawnIndex: actor.spawnIndex,
                        unitSubType: actor.unitSubType != 0 ? actor.unitSubType : (int)UnitSubType.Hero,
                        mainType: actor.mainType != 0 ? actor.mainType : (int)EntityMainType.Unit,
                        hasSpawnPosition: hasSpawnPosition,
                        spawnX: spawnPosition != null ? spawnPosition.x : 0f,
                        spawnY: spawnPosition != null ? spawnPosition.y : 0f,
                        spawnZ: spawnPosition != null ? spawnPosition.z : 0f));
                }
            }

            if (players.Count == 0)
            {
                var skillIds = expectation != null && expectation.config != null && expectation.config.skillId > 0
                    ? new[] { expectation.config.skillId }
                    : Array.Empty<int>();
                players.Add(new MobaPlayerLoadout(
                    fallbackPlayerId,
                    teamId: 1,
                    heroId: 1,
                    attributeTemplateId: 1001,
                    level: 1,
                    basicAttackSkillId: 1,
                    skillIds: skillIds,
                    spawnIndex: 0,
                    unitSubType: (int)UnitSubType.Hero,
                    mainType: (int)EntityMainType.Unit,
                    hasSpawnPosition: 1,
                    spawnX: 0f,
                    spawnY: 0f,
                    spawnZ: 0f));
            }

            return players.ToArray();
        }

        private static Vec3 ToVec3(MobaAcceptanceVector3Expectation value)
        {
            return value == null ? Vec3.Zero : new Vec3(value.x, value.y, value.z);
        }

        private static SkillInputPhase ParseSkillInputPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase)) return SkillInputPhase.Press;
            if (string.Equals(phase, "press", StringComparison.OrdinalIgnoreCase)) return SkillInputPhase.Press;
            if (string.Equals(phase, "hold", StringComparison.OrdinalIgnoreCase)) return SkillInputPhase.Hold;
            if (string.Equals(phase, "release", StringComparison.OrdinalIgnoreCase)) return SkillInputPhase.Release;
            if (string.Equals(phase, "cancel", StringComparison.OrdinalIgnoreCase)) return SkillInputPhase.Cancel;
            return (SkillInputPhase)Enum.Parse(typeof(SkillInputPhase), phase, ignoreCase: true);
        }

        private static int[] ResolveActorSkillIds(MobaAcceptanceExpectation expectation, MobaAcceptanceActorExpectation actor)
        {
            if (actor != null)
            {
                if (actor.skillIds != null && actor.skillIds.Length > 0) return actor.skillIds;
                if (actor.carriedSkillIds != null && actor.carriedSkillIds.Length > 0) return actor.carriedSkillIds;
            }

            if (expectation != null && expectation.config != null && expectation.config.skillId > 0)
            {
                return new[] { expectation.config.skillId };
            }

            return Array.Empty<int>();
        }

        private static MobaAcceptanceActorExpectation[] GetScenarioActors(MobaAcceptanceExpectation expectation)
        {
            if (expectation == null) return Array.Empty<MobaAcceptanceActorExpectation>();
            if (expectation.scenario != null && expectation.scenario.actors != null && expectation.scenario.actors.Length > 0) return expectation.scenario.actors;
            if (expectation.actors != null && expectation.actors.Length > 0) return expectation.actors;
            return Array.Empty<MobaAcceptanceActorExpectation>();
        }

        private static bool TryParseActorId(string actorId, out int value)
        {
            return int.TryParse(actorId, out value) && value > 0;
        }

        private static void AssertPlanContainsAction(TriggerPlan<object> plan, int actionId, string message)
        {
            var actions = plan.Actions;
            if (actions != null)
            {
                for (var i = 0; i < actions.Length; i++)
                {
                    if ((int)actions[i].Id.Value == actionId)
                    {
                        return;
                    }
                }
            }

            Assert.Fail(message);
        }
    }
}
