using System.Collections;
using System.Reflection;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class LocalDemoHeadlessSkillReleaseTests
    {
        private const string DemoScenePath = "Assets/Scenes/MobaDemoScene.unity";
        private const float FixedDeltaTime = 1f / 30f;

        [TearDown]
        public void TearDown()
        {
            if (GameEntry.IsInitialized && GameEntry.Instance != null)
            {
                Object.DestroyImmediate(GameEntry.Instance.gameObject);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator MobaDemoScene_LocalBattle_ReleasesLianPoSkill1AndMovesLocalActor()
        {
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), $"Demo scene should load from {DemoScenePath}.");

            var entry = Object.FindObjectOfType<GameEntry>();
            Assert.IsNotNull(entry, "Demo scene must contain a GameEntry.");

            InvokePrivate(entry, "Awake");
            var flow = entry.Get<GameFlowDomain>();
            Assert.IsNotNull(flow, "GameEntry must expose GameFlowDomain after Awake.");

            flow.StartWithPersistentSettingsSync();
            FlowTick(flow, 2);

            flow.EnterBattle(new TestBattleBootstrapper());
            yield return TickUntil(flow, () => flow.CurrentBattlePhase == MobaBattleState.InMatch, 240, "Battle flow did not reach InMatch.");

            Assert.IsTrue(entry.TryGet(out BattleContext ctx), "BattleContext must be attached after entering battle.");
            yield return TickUntil(flow, () => IsLocalActorReady(ctx), 240, DescribeContextWaitFailure(ctx));

            Assert.IsTrue(TryGetLocalActorPosition(ctx, out var start), DescribeContextWaitFailure(ctx));

            ctx.SubmitHudSkillAim(slot: 1, aimDx: 1f, aimDz: 0f);
            FlowTick(flow, 2);

            yield return TickUntil(
                flow,
                () => TryGetLocalActorPosition(ctx, out var current) && PlanarDistance(start, current) > 0.1f,
                120,
                DescribeMovementFailure(ctx, start));

            Assert.IsTrue(TryGetLocalActorPosition(ctx, out var end), "Local actor position should still be readable after skill release.");
            var moved = PlanarDistance(start, end);
            Assert.Greater(moved, 0.1f, $"Lian Po skill 1 should move the local actor. start={start}, end={end}, moved={moved:F3}");
            Assert.Greater(end.x - start.x, 0.1f, $"Lian Po skill 1 should follow +X aim direction. start={start}, end={end}");
        }

        [UnityTest]
        public IEnumerator MobaDemoScene_LocalBattle_LianPoSkill3AimMovesAndJumpsToSelectedPoint()
        {
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), $"Demo scene should load from {DemoScenePath}.");

            var entry = Object.FindObjectOfType<GameEntry>();
            Assert.IsNotNull(entry, "Demo scene must contain a GameEntry.");

            InvokePrivate(entry, "Awake");
            var flow = entry.Get<GameFlowDomain>();
            Assert.IsNotNull(flow, "GameEntry must expose GameFlowDomain after Awake.");

            flow.StartWithPersistentSettingsSync();
            FlowTick(flow, 2);

            flow.EnterBattle(new TestBattleBootstrapper());
            yield return TickUntil(flow, () => flow.CurrentBattlePhase == MobaBattleState.InMatch, 240, "Battle flow did not reach InMatch.");

            Assert.IsTrue(entry.TryGet(out BattleContext ctx), "BattleContext must be attached after entering battle.");
            yield return TickUntil(flow, () => IsLocalActorReady(ctx), 240, DescribeContextWaitFailure(ctx));

            Assert.IsTrue(TryGetLocalActorPosition(ctx, out var skill1Start), DescribeContextWaitFailure(ctx));
            var skill1SubmitTime = ctx.LogicTimeSeconds;
            ctx.SubmitHudSkillAim(slot: 1, aimDx: 1f, aimDz: 0f);
            FlowTick(flow, 2);
            yield return TickUntil(
                flow,
                () => TryGetLocalActorPosition(ctx, out var current) && PlanarDistance(skill1Start, current) > 0.1f,
                120,
                DescribeMovementFailure(ctx, skill1Start));
            yield return TickUntil(
                flow,
                () => ctx.LogicTimeSeconds >= skill1SubmitTime + 0.8f,
                60,
                $"Lian Po skill 1 did not finish before skill 3 validation. lastFrame={ctx.LastFrame}, logicTime={ctx.LogicTimeSeconds:F3}");

            Assert.IsTrue(TryGetLocalActorPosition(ctx, out var skill3Start), DescribeContextWaitFailure(ctx));
            var maxPlanarMove = 0f;
            var maxHeightDelta = 0f;

            ctx.SubmitHudSkillAim(slot: 3, aimDx: 1f, aimDz: 0f);
            FlowTick(flow, 2);

            yield return TickUntil(
                flow,
                () =>
                {
                    if (!TryGetLocalActorPosition(ctx, out var current)) return false;
                    maxPlanarMove = Mathf.Max(maxPlanarMove, PlanarDistance(skill3Start, current));
                    maxHeightDelta = Mathf.Max(maxHeightDelta, current.y - skill3Start.y);
                    return maxPlanarMove > 0.1f && maxHeightDelta > 0.08f;
                },
                180,
                DescribeSkill3MovementFailure(ctx, skill3Start, maxPlanarMove, maxHeightDelta));

            Assert.IsTrue(TryGetLocalActorPosition(ctx, out var skill3End), "Local actor position should still be readable after skill 3 release.");
            Assert.Greater(maxPlanarMove, 0.1f, $"Lian Po skill 3 should move the caster toward the selected point. start={skill3Start}, end={skill3End}, maxMove={maxPlanarMove:F3}");
            Assert.Greater(maxHeightDelta, 0.08f, $"Lian Po skill 3 should keep jump as a vertical motion. start={skill3Start}, end={skill3End}, maxHeightDelta={maxHeightDelta:F3}");
            Assert.Greater(skill3End.x - skill3Start.x, 0.1f, $"Lian Po skill 3 should follow +X aim position. start={skill3Start}, end={skill3End}");
        }

        private static IEnumerator TickUntil(GameFlowDomain flow, System.Func<bool> predicate, int maxTicks, string failureMessage)
        {
            for (var i = 0; i < maxTicks; i++)
            {
                FlowTick(flow, 1);
                if (predicate()) yield break;
                yield return null;
            }

            Assert.Fail(failureMessage);
        }

        private static void FlowTick(GameFlowDomain flow, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                flow.Tick(FixedDeltaTime);
            }
        }

        private static bool IsLocalActorReady(BattleContext ctx)
        {
            if (ctx == null || ctx.Session == null || ctx.EntityQuery == null) return false;
            if (ctx.LocalActorId <= 0 && TryResolveLocalActorId(ctx, out var actorId))
            {
                ctx.LocalActorId = actorId;
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

        private static string DescribeMovementFailure(BattleContext ctx, Vector3 start)
        {
            if (!TryGetLocalActorPosition(ctx, out var current))
            {
                return DescribeContextWaitFailure(ctx);
            }

            var moved = PlanarDistance(start, current);
            return $"Lian Po skill 1 did not move the local actor after aim release. start={start}, current={current}, moved={moved:F3}, localActorId={ctx.LocalActorId}, lastFrame={ctx.LastFrame}, logicTime={ctx.LogicTimeSeconds:F3}";
        }

        private static string DescribeSkill3MovementFailure(BattleContext ctx, Vector3 start, float maxPlanarMove, float maxHeightDelta)
        {
            if (!TryGetLocalActorPosition(ctx, out var current))
            {
                return DescribeContextWaitFailure(ctx);
            }

            return $"Lian Po skill 3 did not combine selected-point horizontal movement and vertical jump. start={start}, current={current}, maxMove={maxPlanarMove:F3}, maxHeightDelta={maxHeightDelta:F3}, localActorId={ctx.LocalActorId}, lastFrame={ctx.LastFrame}, logicTime={ctx.LogicTimeSeconds:F3}";
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method: {methodName}");
            method.Invoke(target, null);
        }
    }
}
