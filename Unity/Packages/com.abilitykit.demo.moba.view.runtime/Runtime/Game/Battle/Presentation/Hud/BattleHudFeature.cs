using System;
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
        private IDisposable _entityDestroyedSub;

        private BattleHudAimPreview _aimPreview;

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Root.TryGetRef(out _ctx);
            _hudInput = _ctx;

            _camera = Camera.main;
            _config = BattleHudConfig.Default;

            if (_ctx != null && _ctx.EntityNode.IsValid)
            {
                _hudNode = _ctx.EntityNode.AddChild("BattleHud");
            }

            _canvasController = new BattleHudCanvasController();
            _canvasController.Create("BattleHudCanvas");

            _binder = new BattleHudBinder(_config, _canvasController.Root, _camera, _ctx);
            CreateInputController();
            SubscribeEntityLifecycle();
            SubscribeSnapshots();
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            _snapshotController?.Dispose();
            _snapshotController = null;

            _entityDestroyedSub?.Dispose();
            _entityDestroyedSub = null;

            _binder?.Clear();
            _binder = null;

            _inputController?.Dispose();
            _inputController = null;

            _aimPreview?.Clear();
            _aimPreview = null;

            _canvasController?.Dispose();
            _canvasController = null;
            _hudNode = default;

            _ctx = null;
            _hudInput = null;
            _camera = null;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_binder == null) return;

            _binder.Tick(deltaTime);
            _aimPreview ??= new BattleHudAimPreview();
            _aimPreview.Tick(_ctx);
        }

        private void CreateInputController()
        {
            var cameraTransform = _camera != null ? _camera.transform : null;
            _inputController = new BattleHudInputController(
                _hudInput,
                _canvasController.Root,
                _canvasController.Canvas,
                cameraTransform);
            _inputController.Ensure();
        }

        private void SubscribeEntityLifecycle()
        {
            _entityDestroyedSub?.Dispose();
            if (_ctx?.EntityWorld != null)
            {
                _entityDestroyedSub = _ctx.EntityWorld.EntityDestroyed(OnEntityDestroyed);
            }
        }

        private void SubscribeSnapshots()
        {
            _snapshotController = new BattleHudSnapshotController();
            _snapshotController.Bind(_ctx, OnEnterGameSnapshot, OnDamageEventSnapshot);
        }

        private void OnEnterGameSnapshot(EnterMobaGameRes res)
        {
            if (_ctx == null) return;
            _inputController?.ApplySkillButtonTemplates(res, _ctx.Plan.PlayerId);
        }

        private void OnDamageEventSnapshot(MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            _binder?.OnDamageEvents(entries);
        }

        private void OnEntityDestroyed(EC.EntityDestroyed evt)
        {
            _binder?.OnEntityDestroyed(evt.EntityId);
        }
    }
}
