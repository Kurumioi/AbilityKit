using AbilityKit.Ability.World.Abstractions;
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
            ApplyLaunchSpecSkillTemplates();
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

            EnsureSnapshotSubscription();
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

        public void RefreshLocalControlSkillTemplates()
        {
            ApplyLaunchSpecSkillTemplates();
        }

        private void ApplyLaunchSpecSkillTemplates()
        {
            if (_ctx == null) return;
            var launchSpec = _ctx.Plan.LaunchSpec;
            if (launchSpec.Players == null || launchSpec.Players.Length == 0) return;

            var playerId = ResolveLocalPlayerId();
            var worldId = !string.IsNullOrEmpty(_ctx.Plan.World.WorldId)
                ? _ctx.Plan.World.WorldId
                : launchSpec.WorldId;

            var res = new EnterMobaGameRes(
                new WorldId(worldId),
                launchSpec.LocalPlayerId,
                _ctx.LocalActorId,
                launchSpec.RandomSeed,
                launchSpec.TickRate,
                launchSpec.InputDelayFrames,
                playersLoadout: launchSpec.Players);

            _inputController?.ApplySkillButtonTemplates(res, playerId);
            _aimPreview?.SetSkillSpecs(_inputController?.SkillSpecs);
        }

        private void SubscribeEntityLifecycle()
        {
            _entityLifecycle ??= _controllers.CreateEntityLifecycle();
            _entityLifecycle.Bind(_ctx, _binder);
        }

        private void SubscribeSnapshots()
        {
            _snapshotController = _controllers.CreateSnapshots();
            EnsureSnapshotSubscription();
        }

        private void EnsureSnapshotSubscription()
        {
            if (_snapshotController == null || _snapshotController.IsBound) return;

            _snapshotController.Bind(_ctx, OnEnterGameSnapshot, OnDamageEventSnapshot, OnSkillStateSnapshot);
        }

        private void OnEnterGameSnapshot(EnterMobaGameRes res)
        {
            if (_ctx == null) return;
            if (res.LocalActorId > 0 && _ctx.LocalActorId <= 0)
            {
                _ctx.LocalActorId = res.LocalActorId;
            }

            _inputController?.ApplySkillButtonTemplates(res, ResolveLocalPlayerId(res));
            _aimPreview?.SetSkillSpecs(_inputController?.SkillSpecs);
        }

        private void OnDamageEventSnapshot(MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            _binder?.OnDamageEvents(entries);
        }

        private void OnSkillStateSnapshot(MobaSkillStateSnapshotEntry[] entries)
        {
            if (_ctx == null || entries == null || entries.Length == 0) return;
            var localActorId = ResolveLocalActorId(entries);
            if (localActorId > 0 && _ctx.LocalActorId <= 0)
            {
                _ctx.LocalActorId = localActorId;
            }

            _inputController?.ApplySkillStates(entries, localActorId);
        }

        private string ResolveLocalPlayerId(EnterMobaGameRes res = default)
        {
            if (_ctx == null) return string.Empty;
            var controlledPlayerId = _ctx.ResolveLocalControlPlayerId();
            if (!string.IsNullOrEmpty(controlledPlayerId)) return controlledPlayerId;
            if (!string.IsNullOrEmpty(res.PlayerId.Value)) return res.PlayerId.Value;
            return _ctx.Plan.LaunchSpec.LocalPlayerId.Value;
        }

        private int ResolveLocalActorId(MobaSkillStateSnapshotEntry[] entries)
        {
            if (_ctx == null) return 0;
            if (_ctx.LocalActorId > 0) return _ctx.LocalActorId;
            return _inputController != null
                ? _inputController.ResolveActorIdFromSkillStates(entries)
                : 0;
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
