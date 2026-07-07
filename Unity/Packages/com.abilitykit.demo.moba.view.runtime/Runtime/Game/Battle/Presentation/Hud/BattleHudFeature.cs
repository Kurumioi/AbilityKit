using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.World.ECS;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleHudFeature : IGamePhaseFeature
    {
        private BattleContext _ctx;
        private IBattleHudInputSink _hudInput;
        private Camera _camera;

        private EC.IEntity _hudNode;

        private BattleHudConfig _config;
        private BattleHudBinder _binder;
        private BattleHudCanvasController _canvasController;
        private BattleHudInputController _inputController;
        private BattleHudSnapshotController _snapshotController;
        private BattleHudEntityLifecycleBinding _entityLifecycle;
        private BattlePresentationSessionContext _presentation;
        private readonly BattlePresentationSessionResolver _presentationSessions = new BattlePresentationSessionResolver();
        private readonly BattleHudFeatureControllerFactory _controllers = new BattleHudFeatureControllerFactory();

        private BattleHudAimPreview _aimPreview;

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Features.TryGet(out _ctx);
            _hudInput = _ctx;

            _camera = Camera.main;
            _config = BattleHudConfig.Default;
            _presentation = _presentationSessions.Resolve(ctx);

            if (_ctx != null && _ctx.EntityNode.IsValid)
            {
                _hudNode = _ctx.EntityNode.AddChild("BattleHud");
            }

            _canvasController = _controllers.CreateCanvas();
            _canvasController.Create("BattleHudCanvas");

            _binder = _controllers.CreateBinder(_config, _canvasController.Root, _camera, _ctx);
            CreateInputController();
            SubscribeEntityLifecycle();
            SubscribeSnapshots();
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            _snapshotController?.Dispose();
            _snapshotController = null;

            _entityLifecycle?.Dispose();
            _entityLifecycle = null;

            _binder?.Clear();
            _binder = null;

            _inputController?.Dispose();
            _inputController = null;

            _aimPreview?.Clear();
            _aimPreview = null;

            _canvasController?.Dispose();
            _canvasController = null;
            _hudNode = default;

            _presentationSessions.Release(ctx, _presentation);
            _ctx = null;
            _hudInput = null;
            _camera = null;
            _presentation = null;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_binder == null) return;

            _binder.Tick(deltaTime);
            _aimPreview ??= _controllers.CreateAimPreview();
            _aimPreview.SetSkillSpecs(_inputController?.SkillSpecs);
            _aimPreview.Tick(_ctx);
        }

        private void CreateInputController()
        {
            var cameraTransform = _camera != null ? _camera.transform : null;
            _inputController = _controllers.CreateInput(
                _hudInput,
                _canvasController.Root,
                _canvasController.Canvas,
                cameraTransform,
                _presentation?.Resources);
            _inputController.Ensure();
        }

        private void SubscribeEntityLifecycle()
        {
            _entityLifecycle ??= _controllers.CreateEntityLifecycle();
            _entityLifecycle.Bind(_ctx, _binder);
        }

        private void SubscribeSnapshots()
        {
            _snapshotController = _controllers.CreateSnapshots();
            _snapshotController.Bind(_ctx, OnEnterGameSnapshot, OnDamageEventSnapshot);
        }

        private void OnEnterGameSnapshot(EnterMobaGameRes res)
        {
            if (_ctx == null) return;
            _inputController?.ApplySkillButtonTemplates(res, _ctx.Plan.World.PlayerId);
            _aimPreview?.SetSkillSpecs(_inputController?.SkillSpecs);
        }

        private void OnDamageEventSnapshot(MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            _binder?.OnDamageEvents(entries);
        }

    }

    internal sealed class BattleHudFeatureControllerFactory
    {
        public BattleHudCanvasController CreateCanvas()
        {
            return new BattleHudCanvasController();
        }

        public BattleHudBinder CreateBinder(
            BattleHudConfig config,
            RectTransform root,
            Camera camera,
            BattleContext ctx)
        {
            return new BattleHudBinder(config, root, camera, ctx);
        }

        public BattleHudInputController CreateInput(
            IBattleHudInputSink hudInput,
            RectTransform root,
            Canvas canvas,
            Transform cameraTransform,
            BattleViewResourceProvider resources)
        {
            return new BattleHudInputController(hudInput, root, canvas, cameraTransform, resources);
        }

        public BattleHudEntityLifecycleBinding CreateEntityLifecycle()
        {
            return new BattleHudEntityLifecycleBinding();
        }

        public BattleHudSnapshotController CreateSnapshots()
        {
            return new BattleHudSnapshotController();
        }

        public BattleHudAimPreview CreateAimPreview()
        {
            return new BattleHudAimPreview();
        }
    }
}
