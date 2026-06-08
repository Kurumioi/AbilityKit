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

        private BattleHudInputUi _inputUi;

        public BattleHudInputController(
            IBattleHudInputSink hudInput,
            RectTransform root,
            Canvas canvas,
            Transform cameraTransform)
        {
            _hudInput = hudInput;
            _root = root;
            _canvas = canvas;
            _cameraTransform = cameraTransform;
            _inputEvents = new BattleHudInputEventBridge(hudInput);
        }

        public void Ensure()
        {
            if (_root == null) return;
            if (_hudInput == null) return;
            if (_inputUi != null) return;

            _inputUi = BattleHudInputUiFactory.Create(_root, _canvas, _cameraTransform, OnInfoClick);
            _inputEvents.Bind(_inputUi);
        }

        public void ApplySkillButtonTemplates(EnterMobaGameRes res, string playerId)
        {
            BattleHudSkillButtonTemplateBinder.TryApply(
                res,
                playerId,
                _inputUi?.Skill1View,
                _inputUi?.Skill2View,
                _inputUi?.Skill3View);
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
}
