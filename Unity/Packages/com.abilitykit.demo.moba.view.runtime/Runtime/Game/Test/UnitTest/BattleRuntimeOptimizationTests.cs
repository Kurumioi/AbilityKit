using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using NUnit.Framework;
using UnityEngine;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class BattleRuntimeOptimizationTests
    {
        [Test]
        public void DefaultBattleDebugFacade_InvokesInjectedSessionProvider()
        {
            var invoked = false;
            var facade = new DefaultBattleDebugFacade(() =>
            {
                invoked = true;
                return null;
            });

            var resolved = facade.TryGetSession(out var session);

            Assert.IsTrue(invoked);
            Assert.IsFalse(resolved);
            Assert.IsNull(session);
        }

        [Test]
        public void BattleDamageTextFormatter_FormatsDamageAndHealWithoutUnityAdapterState()
        {
            var formatter = new BattleDamageTextFormatter();

            var damageFormatted = formatter.TryFormat(-12.4f, false, out var damage);
            var healFormatted = formatter.TryFormat(0.4f, true, out var heal);
            var zeroFormatted = formatter.TryFormat(0f, false, out _);

            Assert.IsTrue(damageFormatted);
            Assert.AreEqual("-12", damage.Text);
            Assert.IsFalse(damage.IsHeal);
            Assert.IsTrue(healFormatted);
            Assert.AreEqual("+0.4", heal.Text);
            Assert.IsTrue(heal.IsHeal);
            Assert.IsFalse(zeroFormatted);
        }

        [Test]
        public void BattlePresentationCueResolver_CreatesPureSpawnRequestWithoutUnityState()
        {
            var resolver = new BattlePresentationCueResolver();
            var cue = CreatePresentationCue(
                PresentationCueStage.Started,
                requestKey: "skill-hit",
                vfxId: 3001,
                templateId: 0,
                sourceActorId: 11,
                targetActorId: 22,
                targets: new[] { 33 },
                positions: new[] { new SnapshotVec3(1f, 2f, 3f) },
                offsetX: 0.5f,
                offsetY: 1.5f,
                offsetZ: -0.25f);

            var decision = resolver.Resolve(in cue);

            Assert.AreEqual(BattlePresentationCueDecisionKind.Play, decision.Kind);
            Assert.IsFalse(decision.IsNone);
            Assert.AreEqual(3001, decision.SpawnRequest.VfxId);
            Assert.AreEqual(11, decision.SpawnRequest.SourceActorId);
            Assert.AreEqual(22, decision.SpawnRequest.TargetActorId);
            Assert.AreEqual(33, decision.SpawnRequest.FirstTargetActorId);
            Assert.IsTrue(decision.SpawnRequest.HasExplicitPosition);
            Assert.AreEqual(1f, decision.SpawnRequest.ExplicitPosition.X);
            Assert.AreEqual(2f, decision.SpawnRequest.ExplicitPosition.Y);
            Assert.AreEqual(3f, decision.SpawnRequest.ExplicitPosition.Z);
            Assert.AreEqual(0.5f, decision.SpawnRequest.Offset.X);
            Assert.AreEqual(1.5f, decision.SpawnRequest.Offset.Y);
            Assert.AreEqual(-0.25f, decision.SpawnRequest.Offset.Z);
        }

        [Test]
        public void BattlePresentationCueResolver_StopsByStableRequestKeyAndIgnoresMissingVfx()
        {
            var resolver = new BattlePresentationCueResolver();
            var startWithoutVfx = CreatePresentationCue(PresentationCueStage.Started, requestKey: "buff-loop", vfxId: 0, templateId: 0);
            var stop = CreatePresentationCue(PresentationCueStage.Removed, requestKey: "buff-loop", vfxId: 0, templateId: 0);

            var ignoredStart = resolver.Resolve(in startWithoutVfx);
            var stopDecision = resolver.Resolve(in stop);

            Assert.IsTrue(ignoredStart.IsNone);
            Assert.AreEqual(BattlePresentationCueDecisionKind.Stop, stopDecision.Kind);
            Assert.IsFalse(stopDecision.IsNone);
            Assert.IsTrue(stopDecision.SpawnRequest.IsEmpty);
            Assert.AreEqual(BattlePresentationCueRequestKey.From(in startWithoutVfx), stopDecision.RequestKey);
        }

        [Test]
        public void BattleWorldFloatingTextFactory_ReusesReleasedInstance()
        {
            var factory = new BattleWorldFloatingTextFactory();
            BattleWorldFloatingText first = null;
            BattleWorldFloatingText second = null;

            try
            {
                first = factory.Create("100", Vector3.zero, Color.red);
                var firstGameObject = first.GameObject;

                factory.Release(first);
                second = factory.Create("200", Vector3.one, Color.green);

                Assert.AreSame(first, second);
                Assert.AreSame(firstGameObject, second.GameObject);
                Assert.IsTrue(second.GameObject.activeSelf);
                Assert.AreEqual("200", second.Text.text);
                Assert.AreEqual(Vector3.one, second.GameObject.transform.position);
                Assert.AreEqual(Color.green, second.BaseColor);
                Assert.AreEqual(0f, second.Age);
            }
            finally
            {
                DestroyIfAlive(second);
                if (!ReferenceEquals(first, second)) DestroyIfAlive(first);
                factory.ClearPool();
            }
        }

        [Test]
        public void BattleFloatingTextStore_ReleasesExpiredTextInsteadOfDestroying()
        {
            var releaseCount = 0;
            BattleWorldFloatingText released = null;
            var store = new BattleFloatingTextStore(text =>
            {
                releaseCount++;
                released = text;
                text.Deactivate();
            });

            var floatingText = CreateFloatingText(lifetime: 0.1f);

            try
            {
                store.Add(floatingText);
                store.Tick(0.2f);

                Assert.AreEqual(1, releaseCount);
                Assert.AreSame(floatingText, released);
                Assert.IsNotNull(floatingText.GameObject);
                Assert.IsFalse(floatingText.GameObject.activeSelf);
            }
            finally
            {
                DestroyIfAlive(floatingText);
            }
        }

        private static PresentationCueData CreatePresentationCue(
            PresentationCueStage stage,
            string requestKey,
            int vfxId,
            int templateId,
            int sourceActorId = 0,
            int targetActorId = 0,
            int[] targets = null,
            SnapshotVec3[] positions = null,
            float offsetX = 0f,
            float offsetY = 0f,
            float offsetZ = 0f)
        {
            return new PresentationCueData(
                stage,
                cueKind: null,
                cueVfxId: null,
                cueSfxId: null,
                templateId,
                vfxId,
                sfxId: 0,
                requestKey,
                sourceActorId,
                targetActorId,
                triggerEventId: 0,
                triggerEventName: null,
                triggerId: 0,
                phase: 0,
                priority: 0,
                order: 0,
                actionIndex: 0,
                interruptReason: 0,
                interruptSourceName: null,
                interruptTriggerId: 0,
                interruptConditionPassed: false,
                targets ?? Array.Empty<int>(),
                positions ?? Array.Empty<SnapshotVec3>(),
                offsetX,
                offsetY,
                offsetZ,
                durationMsOverride: 0,
                scale: 0f,
                colorR: 0f,
                colorG: 0f,
                colorB: 0f,
                colorA: 0f);
        }

        private static BattleWorldFloatingText CreateFloatingText(float lifetime)
        {
            var go = new GameObject("FloatingTextTest");
            var textMesh = go.AddComponent<TextMesh>();
            return new BattleWorldFloatingText
            {
                GameObject = go,
                Text = textMesh,
                Lifetime = lifetime,
                Velocity = Vector3.zero,
                BaseColor = Color.white,
            };
        }

        private static void DestroyIfAlive(BattleWorldFloatingText floatingText)
        {
            if (floatingText?.GameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(floatingText.GameObject);
                floatingText.GameObject = null;
                floatingText.Text = null;
            }
        }
    }
}
