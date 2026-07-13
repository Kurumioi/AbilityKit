using System.Collections.Generic;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.View;
using AbilityKit.Game.Battle.View.Lib.Skill;
using AbilityKit.Game.Flow;
using AbilityKit.World.ECS;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class BattleHudInputEventBridgeTests
    {
        [Test]
        public void Dispatcher_ForwardsMoveAndSkillEventsToSink()
        {
            var sink = new RecordingHudInputSink();
            var dispatcher = new BattleHudInputEventDispatcher(sink);

            dispatcher.OnMoveBegin();
            dispatcher.OnMoveDxDzChanged(0.25f, -0.5f);
            dispatcher.OnMoveEnd();
            dispatcher.OnSkillClick(2);
            dispatcher.OnSkillAimStart(3, new Vector2(0.1f, 0.2f));
            dispatcher.OnSkillAimUpdate(3, new Vector2(0.3f, 0.4f));
            dispatcher.OnSkillAimEnd(3, new Vector2(0.5f, 0.6f));
            dispatcher.ResetHudAim();

            Assert.AreEqual(1, sink.BeginMoveCount);
            Assert.AreEqual(1, sink.EndMoveCount);
            Assert.AreEqual(0.25f, sink.MoveDx);
            Assert.AreEqual(-0.5f, sink.MoveDz);
            Assert.AreEqual(2, sink.ClickedSlot);
            Assert.AreEqual(3, sink.ActiveAimSlot);
            Assert.AreEqual(0.3f, sink.ActiveAimDx);
            Assert.AreEqual(0.4f, sink.ActiveAimDz);
            Assert.IsTrue(sink.ActiveAimAiming);
            Assert.AreEqual(3, sink.SubmittedAimSlot);
            Assert.AreEqual(0.5f, sink.SubmittedAimDx);
            Assert.AreEqual(0.6f, sink.SubmittedAimDz);
            Assert.AreEqual(0, sink.ResetAimSlot);
            Assert.IsFalse(sink.ResetAimAiming);
        }

        [Test]
        public void Dispatcher_ScalesTargetCircleAimBySkillRange()
        {
            var sink = new RecordingHudInputSink();
            var dispatcher = new BattleHudInputEventDispatcher(sink);
            dispatcher.SetSkillSpecs(new Dictionary<int, BattleHudSkillPresentationSpec>
            {
                [3] = new BattleHudSkillPresentationSpec(
                    10010301,
                    "TargetSkill",
                    BattleHudSkillPreviewShape.TargetCircle,
                    SkillAimIndicatorShape.TargetCircle,
                    10f,
                    6.8f,
                    3.4f,
                    Color.white)
            });

            dispatcher.OnSkillAimUpdate(3, new Vector2(0.5f, 0.25f));
            dispatcher.OnSkillAimEnd(3, new Vector2(1f, 0f));

            Assert.AreEqual(3, sink.ActiveAimSlot);
            Assert.AreEqual(5f, sink.ActiveAimDx, 0.0001f);
            Assert.AreEqual(2.5f, sink.ActiveAimDz, 0.0001f);
            Assert.AreEqual(3, sink.SubmittedAimSlot);
            Assert.AreEqual(10f, sink.SubmittedAimDx, 0.0001f);
            Assert.AreEqual(0f, sink.SubmittedAimDz, 0.0001f);
        }

        [Test]
        public void Dispatcher_KeepsDirectionLineAimNormalized()
        {
            var sink = new RecordingHudInputSink();
            var dispatcher = new BattleHudInputEventDispatcher(sink);
            dispatcher.SetSkillSpecs(new Dictionary<int, BattleHudSkillPresentationSpec>
            {
                [1] = new BattleHudSkillPresentationSpec(
                    10010101,
                    "DirectionSkill",
                    BattleHudSkillPreviewShape.DirectionLine,
                    SkillAimIndicatorShape.DirectionLine,
                    7f,
                    1.8f,
                    0f,
                    Color.white)
            });

            dispatcher.OnSkillAimEnd(1, new Vector2(1f, 0f));

            Assert.AreEqual(1, sink.SubmittedAimSlot);
            Assert.AreEqual(1f, sink.SubmittedAimDx, 0.0001f);
            Assert.AreEqual(0f, sink.SubmittedAimDz, 0.0001f);
        }

        [Test]
        public void Bridge_AimSkillPointerDownSetsPreviewAimAndReleaseKeepsSubmittedPreview()
        {
            using (var fixture = BattleHudInputEventBridgeFixture.Create())
            {
                fixture.Bridge.Bind(fixture.InputUi);

                fixture.PointerDown(fixture.Skill2, new Vector2(10f, 10f));

                Assert.AreEqual(2, fixture.Sink.ActiveAimSlot);
                Assert.IsTrue(fixture.Sink.ActiveAimAiming);

                fixture.PointerUp(fixture.Skill2, new Vector2(40f, 10f));

                Assert.AreEqual(2, fixture.Sink.SubmittedAimSlot);
                Assert.AreEqual(-1, fixture.Sink.ResetAimSlot);
                Assert.IsTrue(fixture.Sink.ResetAimAiming);
            }
        }

        [Test]
        public void Bridge_BindsSkillClickToSinkAndUnbindStopsForwarding()
        {
            using (var fixture = BattleHudInputEventBridgeFixture.Create())
            {
                fixture.Bridge.Bind(fixture.InputUi);

                fixture.Click(fixture.Skill1);
                fixture.Bridge.Unbind();
                fixture.Click(fixture.Skill3);

                Assert.AreEqual(1, fixture.Sink.ClickCount);
                Assert.AreEqual(1, fixture.Sink.ClickedSlot);
            }
        }

        [Test]
        public void Bridge_RebindsWithoutDuplicateSkillClickSubscriptions()
        {
            using (var fixture = BattleHudInputEventBridgeFixture.Create())
            {
                fixture.Bridge.Bind(fixture.InputUi);
                fixture.Bridge.Bind(fixture.InputUi);

                fixture.Click(fixture.Skill1);

                Assert.AreEqual(1, fixture.Sink.ClickCount);
                Assert.AreEqual(1, fixture.Sink.ClickedSlot);
            }
        }

        [Test]
        public void AimPreview_ContextStateKeepsMoziSkill2VisibleThroughSubmissionWindow()
        {
            var world = new EntityWorld();
            var lookup = new BattleEntityLookup();
            var actor = world.Create("mozi");
            actor.WithRef(new BattleTransformComponent
            {
                Position = new Vector3(2f, 0f, 3f),
                Forward = Vector3.right,
            });
            lookup.Bind(new BattleNetId(1004), actor);

            var ctx = BattleContext.Rent();
            var preview = new BattleHudAimPreview();
            preview.SetSkillSpecs(new Dictionary<int, BattleHudSkillPresentationSpec>
            {
                [2] = new BattleHudSkillPresentationSpec(
                    10040201,
                    "墨子-机关重炮",
                    BattleHudSkillPreviewShape.DirectionLine,
                    SkillAimIndicatorShape.DirectionLine,
                    12f,
                    1.4f,
                    0f,
                    new Color(0.18f, 0.95f, 0.72f, 0.34f))
            });
            try
            {
                ctx.LocalActorId = 1004;
                ctx.EntityQuery = new BattleEntityQuery(world, lookup);
                ctx.SetHudSkillAim(2, 1f, 0f, aiming: true);

                preview.Tick(ctx, 0.016f);

                Assert.IsNotNull(preview.PreviewRoot);
                Assert.IsTrue(preview.PreviewRoot.activeSelf);
                var casterRingPosition = preview.PreviewRoot.transform.Find("CasterRing").position;
                Assert.AreEqual(2f, casterRingPosition.x, 0.0001f);
                Assert.AreEqual(3f, casterRingPosition.z, 0.0001f);

                ctx.SubmitHudSkillAim(2, 1f, 0f);
                Assert.IsTrue(BattleHudInputSource.TryConsumeSkillAimSubmit(ctx, out var submitted));
                Assert.AreEqual(2, submitted.Slot);

                preview.Tick(ctx, 0.016f);
                Assert.IsTrue(preview.PreviewRoot.activeSelf);

                preview.Tick(ctx, 0.5f);
                Assert.IsFalse(preview.PreviewRoot.activeSelf);

                ctx.SetHudSkillAim(2, 0f, 1f, aiming: true);
                preview.Tick(ctx, 0.016f);
                Assert.IsTrue(preview.PreviewRoot.activeSelf);

                ctx.CancelHudSkillAim();
                preview.Tick(ctx, 0.016f);
                Assert.IsFalse(preview.PreviewRoot.activeSelf);
            }
            finally
            {
                preview.Clear();
                BattleContext.Return(ctx);
            }
        }

        [Test]
        public void AimPreview_MissingSkillSpecDoesNotInferShapeFromSlot()
        {
            var world = new EntityWorld();
            var lookup = new BattleEntityLookup();
            var actor = world.Create("xiao-qiao");
            actor.WithRef(new BattleTransformComponent
            {
                Position = new Vector3(2f, 0f, 3f),
                Forward = Vector3.right,
            });
            lookup.Bind(new BattleNetId(1002), actor);

            var ctx = BattleContext.Rent();
            var preview = new BattleHudAimPreview();
            try
            {
                ctx.LocalActorId = 1002;
                ctx.EntityQuery = new BattleEntityQuery(world, lookup);
                ctx.SetHudSkillAim(2, 4f, 0f, aiming: true);

                preview.Tick(ctx, 0.016f);

                Assert.IsNull(preview.PreviewRoot);
            }
            finally
            {
                preview.Clear();
                BattleContext.Return(ctx);
            }
        }

        [Test]
        public void AimPreviewObject_TargetCircleShowsXiaoQiaoSkill2AtSelectedPoint()
        {
            var factory = new BattleHudAimPreviewObjectFactory();
            var preview = factory.Create();
            try
            {
                var spec = new BattleHudSkillPresentationSpec(
                    10020201,
                    "小乔-甜蜜恋风",
                    BattleHudSkillPreviewShape.TargetCircle,
                    SkillAimIndicatorShape.TargetCircle,
                    8f,
                    5.6f,
                    2.8f,
                    new Color(0.95f, 0.68f, 0.18f, 0.3f));
                var state = new BattleHudAimPreviewState(
                    2,
                    new Vector3(2f, 0f, 3f),
                    Vector3.right,
                    4f);

                preview.Apply(state, spec);

                var line = preview.Root.transform.Find("Line");
                var circle = preview.Root.transform.Find("Circle");
                var dot = preview.Root.transform.Find("Dot");
                var edgeRing = preview.Root.transform.Find("EdgeRing");

                Assert.IsTrue(preview.Root.activeSelf);
                Assert.IsFalse(line.gameObject.activeSelf);
                Assert.IsTrue(circle.gameObject.activeSelf);
                Assert.IsTrue(dot.gameObject.activeSelf);
                Assert.IsTrue(edgeRing.gameObject.activeSelf);
                Assert.AreEqual(6f, circle.position.x, 0.0001f);
                Assert.AreEqual(3f, circle.position.z, 0.0001f);
                Assert.AreEqual(circle.position.x, dot.position.x, 0.0001f);
                Assert.AreEqual(circle.position.z, dot.position.z, 0.0001f);
                Assert.AreEqual(circle.position.x, edgeRing.position.x, 0.0001f);
                Assert.AreEqual(circle.position.z, edgeRing.position.z, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(preview.Root);
            }
        }

        [Test]
        public void AimPreviewObject_DirectionLineShowsMoziSkill2WorldArrow()
        {
            var factory = new BattleHudAimPreviewObjectFactory();
            var preview = factory.Create();
            try
            {
                var spec = new BattleHudSkillPresentationSpec(
                    10040201,
                    "墨子-机关重炮",
                    BattleHudSkillPreviewShape.DirectionLine,
                    SkillAimIndicatorShape.DirectionLine,
                    12f,
                    1.4f,
                    0f,
                    new Color(0.18f, 0.95f, 0.72f, 0.34f));
                var state = new BattleHudAimPreviewState(2, new Vector3(2f, 0f, 3f), Vector3.right, 1f);

                preview.Apply(state, spec);

                var line = preview.Root.transform.Find("Line");
                var dot = preview.Root.transform.Find("Dot");
                var casterRing = preview.Root.transform.Find("CasterRing");
                var edgeRing = preview.Root.transform.Find("EdgeRing");

                Assert.IsTrue(preview.Root.activeSelf);
                Assert.IsNotNull(line);
                Assert.IsNotNull(dot);
                Assert.IsNotNull(casterRing);
                Assert.IsNotNull(edgeRing);
                Assert.IsTrue(line.gameObject.activeSelf);
                Assert.IsTrue(dot.gameObject.activeSelf);
                Assert.IsTrue(casterRing.gameObject.activeSelf);
                Assert.IsTrue(edgeRing.gameObject.activeSelf);
                Assert.AreEqual(new Vector3(1.4f, 0.035f, 12f), line.localScale);
                Assert.AreEqual(new Vector3(8f, 0.12f, 3f), line.position);
                Assert.AreEqual(new Vector3(14f, 0.155f, 3f), dot.position);

                var renderer = line.GetComponent<Renderer>();
                Assert.IsNotNull(renderer);
                Assert.IsNotNull(renderer.sharedMaterial);
                Assert.AreEqual((int)UnityEngine.Rendering.RenderQueue.Overlay, renderer.sharedMaterial.renderQueue);
            }
            finally
            {
                Object.DestroyImmediate(preview.Root);
            }
        }

        private sealed class BattleHudInputEventBridgeFixture : System.IDisposable
        {
            private readonly GameObject _root;

            private BattleHudInputEventBridgeFixture(
                GameObject root,
                BattleHudInputEventBridge bridge,
                BattleHudInputUi inputUi,
                RecordingHudInputSink sink,
                SkillButtonView skill1,
                SkillButtonView skill2,
                SkillButtonView skill3)
            {
                _root = root;
                Bridge = bridge;
                InputUi = inputUi;
                Sink = sink;
                Skill1 = skill1;
                Skill2 = skill2;
                Skill3 = skill3;
            }

            public BattleHudInputEventBridge Bridge { get; }
            public BattleHudInputUi InputUi { get; }
            public RecordingHudInputSink Sink { get; }
            public SkillButtonView Skill1 { get; }
            public SkillButtonView Skill2 { get; }
            public SkillButtonView Skill3 { get; }

            public static BattleHudInputEventBridgeFixture Create()
            {
                var root = new GameObject("BattleHudInputEventBridgeTests.Root", typeof(RectTransform));
                var inputView = root.AddComponent<BattleHudInputView>();
                var skill1 = CreateSkill(root.transform, "Skill1");
                var skill2 = CreateSkill(root.transform, "Skill2");
                var skill3 = CreateSkill(root.transform, "Skill3");
                inputView.Initialize(null, skill1, skill2, skill3);

                var skillAimMapper = new BattleHudSkillAimInputMapper();
                skillAimMapper.Initialize(inputView, null);
                var inputUi = new BattleHudInputUi(root, inputView, null, null, skillAimMapper, new[] { skill1, skill2, skill3 }, null);
                var sink = new RecordingHudInputSink();
                var bridge = new BattleHudInputEventBridge(sink);

                return new BattleHudInputEventBridgeFixture(root, bridge, inputUi, sink, skill1, skill2, skill3);
            }

            public void Click(SkillButtonView skill)
            {
                PointerDown(skill, new Vector2(10f, 10f));
                PointerUp(skill, new Vector2(10f, 10f));
            }

            public void PointerDown(SkillButtonView skill, Vector2 position)
            {
                skill.OnPointerDown(Pointer(1, position));
            }

            public void PointerUp(SkillButtonView skill, Vector2 position)
            {
                skill.OnPointerUp(Pointer(1, position));
            }

            public void Dispose()
            {
                Bridge.Dispose();
                Object.DestroyImmediate(_root);
            }

            private static PointerEventData Pointer(int pointerId, Vector2 position)
            {
                return new PointerEventData(EventSystem.current)
                {
                    pointerId = pointerId,
                    position = position
                };
            }

            private static SkillButtonView CreateSkill(Transform parent, string name)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                var skill = go.AddComponent<SkillButtonView>();
                var config = SkillButtonConfig.Default;
                config.EnableAim = name == "Skill2";
                config.IndicatorShape = config.EnableAim ? SkillAimIndicatorShape.DirectionLine : SkillAimIndicatorShape.Hidden;
                skill.Initialize(rect, parent as RectTransform, null, config);
                return skill;
            }
        }

        private sealed class RecordingHudInputSink : IBattleHudInputSink
        {
            public int BeginMoveCount { get; private set; }
            public int EndMoveCount { get; private set; }
            public float MoveDx { get; private set; }
            public float MoveDz { get; private set; }
            public int ClickCount { get; private set; }
            public int ClickedSlot { get; private set; }
            public int ActiveAimSlot { get; private set; }
            public float ActiveAimDx { get; private set; }
            public float ActiveAimDz { get; private set; }
            public bool ActiveAimAiming { get; private set; }
            public int SubmittedAimSlot { get; private set; }
            public float SubmittedAimDx { get; private set; }
            public float SubmittedAimDz { get; private set; }
            public int ResetAimSlot { get; private set; } = -1;
            public bool ResetAimAiming { get; private set; } = true;

            public void BeginHudMove()
            {
                BeginMoveCount++;
            }

            public void EndHudMove()
            {
                EndMoveCount++;
            }

            public void SetHudMove(float dx, float dz)
            {
                MoveDx = dx;
                MoveDz = dz;
            }

            public void SubmitHudSkillClick(int slot)
            {
                ClickCount++;
                ClickedSlot = slot;
            }

            public void SetHudSkillAim(int slot, float dx, float dz, bool aiming)
            {
                if (slot == 0)
                {
                    ResetAimSlot = slot;
                    ResetAimAiming = aiming;
                    return;
                }

                ActiveAimSlot = slot;
                ActiveAimDx = dx;
                ActiveAimDz = dz;
                ActiveAimAiming = aiming;
            }

            public void CancelHudSkillAim()
            {
                ResetAimSlot = 0;
                ResetAimAiming = false;
            }

            public void SubmitHudSkillAim(int slot, float aimDx, float aimDz)
            {
                SubmittedAimSlot = slot;
                SubmittedAimDx = aimDx;
                SubmittedAimDz = aimDz;
            }
        }
    }
}
