using System.Collections;
using System.Reflection;
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
            return ctx != null
                   && ctx.Session != null
                   && ctx.LocalActorId > 0
                   && ctx.EntityQuery != null
                   && TryGetLocalActorPosition(ctx, out _);
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
            return $"Local actor was not ready. hasSession={ctx.Session != null}, localActorId={ctx.LocalActorId}, hasEntityQuery={ctx.EntityQuery != null}, lastFrame={ctx.LastFrame}, logicTime={ctx.LogicTimeSeconds:F3}";
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

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method: {methodName}");
            method.Invoke(target, null);
        }
    }
}
