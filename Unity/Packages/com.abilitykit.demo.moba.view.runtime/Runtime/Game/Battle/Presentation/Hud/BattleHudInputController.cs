using System;
using AbilityKit.Protocol.Moba;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputController : IDisposable
    {
        private readonly IBattleHudInputSink _hudInput;
        private readonly RectTransform _root;
        private readonly Canvas _canvas;
        private readonly Transform _cameraTransform;
        private readonly BattleHudInputEventBridge _inputEvents;
        private readonly BattleHudSkillButtonTemplateBinder _templateBinder;
        private readonly BattleHudInputUiFactory _uiFactory;

        private BattleHudInputUi _inputUi;

        public BattleHudInputController(
            IBattleHudInputSink hudInput,
            RectTransform root,
            Canvas canvas,
            Transform cameraTransform,
            BattleViewResourceProvider resources = null,
            BattleHudInputUiFactory uiFactory = null,
            BattleHudInputControllerFactory controllers = null)
        {
            controllers ??= new BattleHudInputControllerFactory();

            _hudInput = hudInput;
            _root = root;
            _canvas = canvas;
            _cameraTransform = cameraTransform;
            _inputEvents = controllers.CreateInputEvents(hudInput);
            _templateBinder = controllers.CreateTemplateBinder(resources);
            _uiFactory = uiFactory ?? new BattleHudInputUiFactory();
        }

        public void Ensure()
        {
            if (_root == null) return;
            if (_hudInput == null) return;
            if (_inputUi != null) return;

            _inputUi = _uiFactory.Create(_root, _canvas, _cameraTransform, OnInfoClick);
            _inputEvents.Bind(_inputUi);
        }

        public void ApplySkillButtonTemplates(EnterMobaGameRes res, string playerId)
        {
            _templateBinder.TryApply(
                res,
                playerId,
                _inputUi?.SkillViews);
        }

        public void Dispose()
        {
            _inputEvents.ResetHudAim();
            DestroyInputUi();
        }

        private void DestroyInputUi()
        {
            if (_inputUi == null) return;

            _inputEvents.Unbind();
            _inputUi.Destroy();
            _inputUi = null;
        }

        private static void OnInfoClick()
        {
        }
    }

    internal sealed class BattleHudInputControllerFactory
    {
        public BattleHudInputEventBridge CreateInputEvents(IBattleHudInputSink hudInput)
        {
            return new BattleHudInputEventBridge(hudInput);
        }

        public BattleHudSkillButtonTemplateBinder CreateTemplateBinder(BattleViewResourceProvider resources)
        {
            return new BattleHudSkillButtonTemplateBinder(resources);
        }
    }
}
