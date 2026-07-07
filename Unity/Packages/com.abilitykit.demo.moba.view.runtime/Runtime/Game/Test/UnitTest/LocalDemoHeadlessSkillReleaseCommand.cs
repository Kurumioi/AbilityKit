using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.Search;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Game.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AbilityKit.Game.Test.UnitTest
{
    [InitializeOnLoad]
    public static class LocalDemoHeadlessSkillReleaseCommand
    {
        private const string DemoScenePath = "Assets/Scenes/MobaDemoScene.unity";
        private const string RunningKey = "AbilityKit.LocalDemoHeadlessSkillRelease.Running";
        private const string ResultPathKey = "AbilityKit.LocalDemoHeadlessSkillRelease.ResultPath";
        private const float FixedDeltaTime = 1f / 30f;
        private const int MaxTicks = 1500;
        private const float DamageEpsilon = 0.01f;
        private const float KnockupHeightEpsilon = 0.05f;
        private const float LandingHeightEpsilon = 0.03f;
        private const float TestHp = 5000f;

        private static readonly List<int> s_actorIds = new List<int>(16);
        private static readonly List<int> s_searchResults = new List<int>(16);
        private static readonly SkillScenario[] s_scenarios =
        {
            new SkillScenario("skill1.first", slot: 1, aimDx: 1f, aimDz: 0f, enemyOffsetX: 2.5f, enemyOffsetZ: 0f, requireCasterMove: true, requireDamage: true, requireKnockup: true, minObserveTicks: 8, maxObserveTicks: 180),
            new SkillScenario("skill1.second", slot: 1, aimDx: 1f, aimDz: 0f, enemyOffsetX: 2.5f, enemyOffsetZ: 0f, requireCasterMove: true, requireDamage: true, requireKnockup: true, minObserveTicks: 8, maxObserveTicks: 180),
            new SkillScenario("skill2", slot: 2, aimDx: 1f, aimDz: 0f, enemyOffsetX: 1.5f, enemyOffsetZ: 0f, requireCasterMove: false, requireDamage: true, requireKnockup: false, minObserveTicks: 20, maxObserveTicks: 210),
            new SkillScenario("skill3", slot: 3, aimDx: 1f, aimDz: 0f, enemyOffsetX: 1.5f, enemyOffsetZ: 0f, requireCasterMove: false, requireDamage: true, requireKnockup: true, minObserveTicks: 20, maxObserveTicks: 300),
        };

        private static bool _battleRequested;
        private static int _ticks;
        private static int _lastPeriodicDiagnosticTick;
        private static string _lastStage;
        private static Vector3 _startPosition;
        private static bool _hasStartPosition;
        private static int _enemyActorId;
        private static int _scenarioIndex;
        private static ScenarioPhase _scenarioPhase;
        private static int _scenarioStartTick;
        private static int _scenarioSubmitTick;
        private static float _enemyStartHp;
        private static Vector3 _enemyStartPosition;
        private static float _lastEnemyHp;
        private static Vector3 _lastEnemyPosition;
        private static float _scenarioStartCasterX;
        private static bool _movementObserved;
        private static float _observedMoveDistance;
        private static bool _knockupObserved;
        private static float _observedKnockupHeight;
        private static readonly List<string> s_scenarioResults = new List<string>(8);

        static LocalDemoHeadlessSkillReleaseCommand()
        {
            EditorApplication.update -= ContinueInPlayMode;
            EditorApplication.update += ContinueInPlayMode;
        }

        public static void Run()
        {
            var resultPath = GetArgValue("-headlessSkillResult");
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                resultPath = Path.GetFullPath("../UnityHeadlessSkillReleaseCommand.xml");
            }

            SessionState.SetBool(RunningKey, true);
            SessionState.SetString(ResultPathKey, resultPath);
            ResetState();

            try
            {
                var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new InvalidOperationException($"Demo scene should load from {DemoScenePath}.");

                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                Finish(false, "EXCEPTION: " + ex);
            }
        }

        private static void ContinueInPlayMode()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;

            try
            {
                var result = TickScenario();
                if (result != null)
                {
                    Finish(true, result);
                }
            }
            catch (Exception ex)
            {
                Finish(false, "EXCEPTION: " + ex);
            }
        }

        private static string TickScenario()
        {
            _ticks++;
            if (_ticks > MaxTicks) throw new TimeoutException("Headless local demo skill release validation timed out. " + BuildDiagnostic(null, null));

            var entry = Object.FindObjectOfType<GameEntry>();
            if (entry == null)
            {
                Stage("waitingForEntry", null, null);
                return null;
            }

            if (!GameEntry.IsInitialized)
            {
                Stage("waitingForGameEntryInitialized", null, null);
                return null;
            }

            var flow = entry.Get<GameFlowDomain>();
            if (flow == null)
            {
                Stage("waitingForFlow", null, null);
                return null;
            }

            FlowTick(flow, 1);

            if (!_battleRequested)
            {
                Stage("requestingBattle", flow, null);
                flow.EnterBattle(new TestBattleBootstrapper());
                _battleRequested = true;
                Stage("battleRequested", flow, null);
                return null;
            }

            if (flow.CurrentBattlePhase != MobaBattleState.InMatch)
            {
                Stage("waitingForInMatch", flow, null);
                return null;
            }

            if (!entry.TryGet(out BattleContext ctx))
            {
                Stage("waitingForBattleContext", flow, null);
                return null;
            }

            if (!EnsureLocalActorReady(ctx))
            {
                Stage("waitingForLocalActorReady", flow, ctx);
                return null;
            }

            if (!_hasStartPosition)
            {
                if (!TryGetLocalActorPosition(ctx, out _startPosition)) return null;
                _hasStartPosition = true;
                Stage("startPositionCaptured", flow, ctx);
            }

            if (_enemyActorId <= 0)
            {
                if (!TryFindEnemyActor(ctx, out _enemyActorId))
                {
                    throw new InvalidOperationException("No enemy actor with transform and attrs was found for headless Lian Po skill validation.");
                }
            }

            if (_scenarioIndex >= s_scenarios.Length)
            {
                return "PASS: Lian Po headless skill validation completed. " + string.Join(" | ", s_scenarioResults);
            }

            var scenario = s_scenarios[_scenarioIndex];
            switch (_scenarioPhase)
            {
                case ScenarioPhase.Prepare:
                    PrepareScenario(ctx, scenario);
                    _scenarioStartTick = _ticks;
                    _scenarioPhase = ScenarioPhase.Submit;
                    Stage("prepared." + scenario.Name, flow, ctx);
                    FlowTick(flow, 2);
                    return null;

                case ScenarioPhase.Submit:
                    Stage("submitting." + scenario.Name, flow, ctx);
                    ctx.SubmitHudSkillAim(scenario.Slot, scenario.AimDx, scenario.AimDz);
                    _scenarioSubmitTick = _ticks;
                    _scenarioPhase = ScenarioPhase.Observe;
                    Stage("submitted." + scenario.Name, flow, ctx);
                    FlowTick(flow, 2);
                    return null;

                case ScenarioPhase.Observe:
                    return ObserveScenario(flow, ctx, scenario);

                default:
                    throw new InvalidOperationException($"Unsupported scenario phase: {_scenarioPhase}");
            }
        }

        private static string ObserveScenario(GameFlowDomain flow, BattleContext ctx, SkillScenario scenario)
        {
            if (!TryGetLocalActorPosition(ctx, out var casterPosition))
            {
                throw new InvalidOperationException(DescribeContextWaitFailure(ctx));
            }

            var moved = Math.Abs(casterPosition.x - _scenarioStartCasterX);
            if (moved > 0.1f)
            {
                if (scenario.RequireCasterMove && scenario.AimDx > 0f && casterPosition.x - _scenarioStartCasterX <= 0.1f)
                {
                    throw new InvalidOperationException($"{scenario.Name} should follow +X aim direction. startX={_scenarioStartCasterX:F3}, current={casterPosition}, moved={moved:F3}");
                }

                _movementObserved = true;
                _observedMoveDistance = moved;
            }

            if (!TryGetEnemyRuntimeState(ctx, _enemyActorId, out var enemyHp, out var enemyPosition))
            {
                throw new InvalidOperationException($"Prepared enemy actor disappeared. enemyActorId={_enemyActorId}");
            }

            _lastEnemyHp = enemyHp;
            _lastEnemyPosition = enemyPosition;
            var damage = _enemyStartHp - enemyHp;
            var enemyPlanarMove = PlanarDistance(_enemyStartPosition, enemyPosition);
            var enemyHeightDelta = enemyPosition.y - _enemyStartPosition.y;
            if (enemyHeightDelta > _observedKnockupHeight) _observedKnockupHeight = enemyHeightDelta;
            if (enemyHeightDelta > KnockupHeightEpsilon) _knockupObserved = true;

            var observedEnoughTicks = _ticks - _scenarioSubmitTick >= scenario.MinObserveTicks;
            var landed = Math.Abs(enemyHeightDelta) <= LandingHeightEpsilon;
            var movementOk = !scenario.RequireCasterMove || _movementObserved;
            var damageOk = !scenario.RequireDamage || damage > DamageEpsilon;
            var knockupOk = !scenario.RequireKnockup || (_knockupObserved && landed);

            if (observedEnoughTicks && movementOk && damageOk && knockupOk)
            {
                var result = $"{scenario.Name}: damage={damage:F3}, casterMove={_observedMoveDistance:F3}, enemyPlanarMove={enemyPlanarMove:F3}, maxEnemyHeightDelta={_observedKnockupHeight:F3}, landedHeightDelta={enemyHeightDelta:F3}";
                s_scenarioResults.Add(result);
                _scenarioIndex++;
                _scenarioPhase = ScenarioPhase.Prepare;
                Stage("passed." + scenario.Name, flow, ctx);
                FlowTick(flow, 4);
                return _scenarioIndex >= s_scenarios.Length
                    ? "PASS: Lian Po headless skill validation completed. " + string.Join(" | ", s_scenarioResults)
                    : null;
            }

            if (_ticks - _scenarioStartTick > scenario.MaxObserveTicks)
            {
                throw new TimeoutException($"{scenario.Name} validation timed out. damage={damage:F3}, movementOk={movementOk}, damageOk={damageOk}, knockupOk={knockupOk}, landed={landed}, " + BuildDiagnostic(flow, ctx));
            }

            Stage("observing." + scenario.Name, flow, ctx);
            return null;
        }

        private static void PrepareScenario(BattleContext ctx, SkillScenario scenario)
        {
            if (!TryGetLocalActorPosition(ctx, out _))
            {
                throw new InvalidOperationException(DescribeContextWaitFailure(ctx));
            }

            var casterPosition = new Vector3(_startPosition.x, _startPosition.y, _startPosition.z);
            if (!TrySetActorPosition(ctx, ctx.LocalActorId, casterPosition))
            {
                throw new InvalidOperationException($"Failed to reset local actor position. actorId={ctx.LocalActorId}, position={casterPosition}");
            }

            var targetPosition = new Vector3(casterPosition.x + scenario.EnemyOffsetX, casterPosition.y, casterPosition.z + scenario.EnemyOffsetZ);
            if (!TrySetActorPosition(ctx, _enemyActorId, targetPosition))
            {
                throw new InvalidOperationException($"Failed to place enemy actor for {scenario.Name}. enemyActorId={_enemyActorId}, targetPosition={targetPosition}");
            }

            if (!TrySetActorHp(ctx, _enemyActorId, TestHp))
            {
                throw new InvalidOperationException($"Failed to reset enemy hp for {scenario.Name}. enemyActorId={_enemyActorId}");
            }

            ResetActiveSkillCooldowns(ctx, ctx.LocalActorId);

            if (!TryGetEnemyRuntimeState(ctx, _enemyActorId, out _enemyStartHp, out _enemyStartPosition))
            {
                throw new InvalidOperationException($"Failed to capture enemy baseline after placement. enemyActorId={_enemyActorId}");
            }

            ValidateScenarioSetup(ctx, scenario, casterPosition);

            _lastEnemyHp = _enemyStartHp;
            _lastEnemyPosition = _enemyStartPosition;
            _scenarioStartCasterX = casterPosition.x;
            _movementObserved = false;
            _observedMoveDistance = 0f;
            _knockupObserved = false;
            _observedKnockupHeight = 0f;
        }

        private static void FlowTick(GameFlowDomain flow, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                flow.Tick(FixedDeltaTime);
            }
        }

        private static bool EnsureLocalActorReady(BattleContext ctx)
        {
            if (ctx == null || ctx.Session == null || ctx.EntityQuery == null) return false;
            if (ctx.LocalActorId <= 0 && TryResolveLocalActorId(ctx, out var actorId))
            {
                ctx.LocalActorId = actorId;
                Stage("localActorResolved", null, ctx);
            }

            return ctx.LocalActorId > 0 && TryGetLocalActorPosition(ctx, out _);
        }

        private static bool TryResolveLocalActorId(BattleContext ctx, out int actorId)
        {
            actorId = 0;
            if (ctx?.Session == null) return false;
            if (!ctx.Session.TryGetWorld(out var world) || world?.Services == null) return false;
            if (!world.Services.TryResolve<MobaPlayerActorMapService>(out var playerActorMap) || playerActorMap == null) return false;

            var playerIdValue = ctx.Plan.World.PlayerId;
            var playerId = new PlayerId(string.IsNullOrEmpty(playerIdValue) ? "p1" : playerIdValue);
            if (!playerActorMap.TryGetActorId(playerId, out actorId) || actorId <= 0) return false;
            return ctx.EntityQuery.TryGetTransform(new BattleNetId(actorId), out var transform) && transform != null;
        }

        private static bool TryGetLocalActorPosition(BattleContext ctx, out Vector3 position)
        {
            position = default;
            if (ctx == null || ctx.EntityQuery == null || ctx.LocalActorId <= 0) return false;
            if (!ctx.EntityQuery.TryGetTransform(new BattleNetId(ctx.LocalActorId), out var transform) || transform == null) return false;

            position = transform.Position;
            return true;
        }

        private static bool TryFindEnemyActor(BattleContext ctx, out int enemyActorId)
        {
            enemyActorId = 0;
            if (!TryGetEntityManager(ctx, out var entities)) return false;
            if (!entities.TryGetActorEntity(ctx.LocalActorId, out var local) || local == null) return false;

            entities.GetRegisteredActorIds(s_actorIds);
            for (var i = 0; i < s_actorIds.Count; i++)
            {
                var actorId = s_actorIds[i];
                if (actorId <= 0 || actorId == ctx.LocalActorId) continue;
                if (!entities.TryGetActorEntity(actorId, out var candidate) || candidate == null) continue;
                if (!candidate.hasTransform || !candidate.hasAttributeGroup) continue;
                if (local.hasTeam && candidate.hasTeam && local.team.Value.Equals(candidate.team.Value)) continue;

                enemyActorId = actorId;
                return true;
            }

            return false;
        }

        private static bool TryGetEnemyRuntimeState(BattleContext ctx, int actorId, out float hp, out Vector3 position)
        {
            hp = 0f;
            position = default;
            if (!TryGetEntityManager(ctx, out var entities)) return false;
            if (!entities.TryGetActorEntity(actorId, out var entity) || entity == null || !entity.hasTransform || !entity.hasAttributeGroup) return false;

            hp = entity.GetMobaAttrs().Hp;
            position = ToUnityVector(entity.transform.Value.Position);
            return true;
        }

        private static bool TrySetActorPosition(BattleContext ctx, int actorId, Vector3 position)
        {
            if (!TryGetEntityManager(ctx, out var entities)) return false;
            if (!entities.TryGetActorEntity(actorId, out var entity) || entity == null || !entity.hasTransform) return false;

            var current = entity.transform.Value;
            var newPosition = ToVec3(position);
            entity.ReplaceTransform(new Transform3(in newPosition, in current.Rotation, in current.Scale));
            return true;
        }

        private static bool TrySetActorHp(BattleContext ctx, int actorId, float hp)
        {
            if (!TryGetEntityManager(ctx, out var entities)) return false;
            if (!entities.TryGetActorEntity(actorId, out var entity) || entity == null || !entity.hasAttributeGroup || !entity.hasResourceContainer) return false;

            var attrs = entity.GetMobaAttrs();
            attrs.SetBase(BattleAttributeType.MAX_HP, hp);
            attrs.Hp = hp;
            return true;
        }

        private static void ResetActiveSkillCooldowns(BattleContext ctx, int actorId)
        {
            if (!TryGetEntityManager(ctx, out var entities)) return;
            if (!entities.TryGetActorEntity(actorId, out var entity) || entity == null || !entity.hasSkillLoadout) return;

            var skills = entity.skillLoadout.ActiveSkills;
            if (skills != null)
            {
                for (var i = 0; i < skills.Length; i++)
                {
                    if (skills[i] != null) skills[i].CooldownEndTimeMs = 0L;
                }
            }

            if (ctx.Session != null && ctx.Session.TryGetWorld(out var world) && world?.Services != null && world.Services.TryResolve<SkillCastCoordinator>(out var coordinator) && coordinator != null)
            {
                coordinator.CancelAll(actorId);
            }
        }

        private static void ValidateScenarioSetup(BattleContext ctx, SkillScenario scenario, Vector3 casterPosition)
        {
            if (ctx == null || scenario.Slot <= 1) return;
            if (ctx.Session == null || !ctx.Session.TryGetWorld(out var world) || world?.Services == null) return;

            var expectedTriggerId = scenario.Slot == 2 ? 10010211 : 10010331;
            if (!world.Services.TryResolve<TriggerPlanJsonDatabase>(out var triggerDb) || triggerDb == null)
            {
                throw new InvalidOperationException($"{scenario.Name} validation requires TriggerPlanJsonDatabase.");
            }

            if (!triggerDb.TryGetPlanByTriggerId(expectedTriggerId, out _))
            {
                throw new InvalidOperationException($"{scenario.Name} trigger plan was not loaded. triggerId={expectedTriggerId}");
            }

            var queryId = scenario.Slot == 2 ? 50010201 : 50010301;
            if (!world.Services.TryResolve<SearchTargetService>(out var search) || search == null)
            {
                throw new InvalidOperationException($"{scenario.Name} validation requires SearchTargetService.");
            }

            s_searchResults.Clear();
            var aim = ToVec3(new Vector3(scenario.AimDx, 0f, scenario.AimDz));
            if (!search.TrySearchActorIds(queryId, ctx.LocalActorId, in aim, explicitTargetActorId: 0, s_searchResults) || !s_searchResults.Contains(_enemyActorId))
            {
                throw new InvalidOperationException($"{scenario.Name} target query did not find prepared enemy. queryId={queryId}, caster={ctx.LocalActorId}, enemy={_enemyActorId}, casterPos={casterPosition}, enemyPos={_enemyStartPosition}, resultCount={s_searchResults.Count}");
            }

            Debug.Log($"[LocalDemoHeadlessSkillReleaseCommand] setup validated. scenario={scenario.Name}, triggerId={expectedTriggerId}, queryId={queryId}, targets={string.Join(",", s_searchResults)}");
        }

        private static bool TryGetEntityManager(BattleContext ctx, out MobaEntityManager entities)
        {
            entities = null;
            if (ctx?.Session == null) return false;
            if (!ctx.Session.TryGetWorld(out var world) || world?.Services == null) return false;
            return world.Services.TryResolve(out entities) && entities != null;
        }

        private static Vector3 ToUnityVector(Vec3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static Vec3 ToVec3(Vector3 value)
        {
            return new Vec3(value.x, value.y, value.z);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var dx = b.x - a.x;
            var dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string DescribeContextWaitFailure(BattleContext ctx)
        {
            if (ctx == null) return "BattleContext was not available.";
            return $"Local actor was not ready. hasSession={ctx.Session != null}, localActorId={ctx.LocalActorId}, planPlayerId={ctx.Plan.World.PlayerId}, hasEntityQuery={ctx.EntityQuery != null}, lastFrame={ctx.LastFrame}, logicTime={ctx.LogicTimeSeconds:F3}";
        }

        private static void Stage(string stage, GameFlowDomain flow, BattleContext ctx)
        {
            var changed = !string.Equals(_lastStage, stage, StringComparison.Ordinal);
            var periodic = _ticks - _lastPeriodicDiagnosticTick >= 60;
            if (!changed && !periodic) return;

            _lastStage = stage;
            _lastPeriodicDiagnosticTick = _ticks;
            Debug.Log("[LocalDemoHeadlessSkillReleaseCommand] " + BuildDiagnostic(flow, ctx));
        }

        private static string BuildDiagnostic(GameFlowDomain flow, BattleContext ctx)
        {
            var currentPosition = "n/a";
            if (ctx != null && TryGetLocalActorPosition(ctx, out var position))
            {
                currentPosition = position.ToString();
            }

            var scenarioName = _scenarioIndex >= 0 && _scenarioIndex < s_scenarios.Length ? s_scenarios[_scenarioIndex].Name : "complete";
            var enemyText = _enemyActorId > 0
                ? $", enemyActorId={_enemyActorId}, enemyStartHp={_enemyStartHp:F3}, lastEnemyHp={_lastEnemyHp:F3}, enemyStart={_enemyStartPosition}, lastEnemy={_lastEnemyPosition}, enemyMove3d={Vector3.Distance(_enemyStartPosition, _lastEnemyPosition):F3}, enemyHeightDelta={(_lastEnemyPosition.y - _enemyStartPosition.y):F3}, observedKnockupHeight={_observedKnockupHeight:F3}, knockupObserved={_knockupObserved}, movementObserved={_movementObserved}, observedMove={_observedMoveDistance:F3}"
                : string.Empty;

            return $"stage={_lastStage ?? "notStarted"}, ticks={_ticks}, battleRequested={_battleRequested}, scenario={scenarioName}, scenarioIndex={_scenarioIndex}, scenarioPhase={_scenarioPhase}, hasStartPosition={_hasStartPosition}, battlePhase={(flow != null ? flow.CurrentBattlePhase.ToString() : "n/a")}, hasContext={ctx != null}, hasSession={(ctx != null && ctx.Session != null)}, localActorId={(ctx != null ? ctx.LocalActorId : 0)}, planPlayerId={(ctx != null ? ctx.Plan.World.PlayerId : "n/a")}, lastFrame={(ctx != null ? ctx.LastFrame : 0)}, logicTime={(ctx != null ? ctx.LogicTimeSeconds : 0f):F3}, start={(_hasStartPosition ? _startPosition.ToString() : "n/a")}, current={currentPosition}{enemyText}";
        }

        private static void Finish(bool success, string message)
        {
            var resultPath = SessionState.GetString(ResultPathKey, Path.GetFullPath("../UnityHeadlessSkillReleaseCommand.xml"));
            SessionState.EraseBool(RunningKey);
            SessionState.EraseString(ResultPathKey);
            ResetState();

            WriteResult(resultPath, success, message);
            Debug.Log("[LocalDemoHeadlessSkillReleaseCommand] " + message);

            EditorApplication.update -= ContinueInPlayMode;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            EditorApplication.Exit(success ? 0 : 1);
        }

        private static void ResetState()
        {
            _battleRequested = false;
            _ticks = 0;
            _lastPeriodicDiagnosticTick = 0;
            _lastStage = null;
            _startPosition = default;
            _hasStartPosition = false;
            _enemyActorId = 0;
            _scenarioIndex = 0;
            _scenarioPhase = ScenarioPhase.Prepare;
            _scenarioStartTick = 0;
            _scenarioSubmitTick = 0;
            _enemyStartHp = 0f;
            _enemyStartPosition = default;
            _lastEnemyHp = 0f;
            _lastEnemyPosition = default;
            _scenarioStartCasterX = 0f;
            _movementObserved = false;
            _observedMoveDistance = 0f;
            _knockupObserved = false;
            _observedKnockupHeight = 0f;
            s_scenarioResults.Clear();
            s_searchResults.Clear();
        }

        private static string GetArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }

            return null;
        }

        private static void WriteResult(string path, bool success, string message)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var encodedMessage = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message ?? string.Empty));
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                $"<headlessSkillRelease success=\"{success.ToString().ToLowerInvariant()}\">\n" +
                $"  <message encoding=\"base64\">{encodedMessage}</message>\n" +
                "</headlessSkillRelease>\n");
        }

        private enum ScenarioPhase
        {
            Prepare,
            Submit,
            Observe
        }

        private readonly struct SkillScenario
        {
            public SkillScenario(string name, int slot, float aimDx, float aimDz, float enemyOffsetX, float enemyOffsetZ, bool requireCasterMove, bool requireDamage, bool requireKnockup, int minObserveTicks, int maxObserveTicks)
            {
                Name = name;
                Slot = slot;
                AimDx = aimDx;
                AimDz = aimDz;
                EnemyOffsetX = enemyOffsetX;
                EnemyOffsetZ = enemyOffsetZ;
                RequireCasterMove = requireCasterMove;
                RequireDamage = requireDamage;
                RequireKnockup = requireKnockup;
                MinObserveTicks = minObserveTicks;
                MaxObserveTicks = maxObserveTicks;
            }

            public string Name { get; }
            public int Slot { get; }
            public float AimDx { get; }
            public float AimDz { get; }
            public float EnemyOffsetX { get; }
            public float EnemyOffsetZ { get; }
            public bool RequireCasterMove { get; }
            public bool RequireDamage { get; }
            public bool RequireKnockup { get; }
            public int MinObserveTicks { get; }
            public int MaxObserveTicks { get; }
        }
    }
}
