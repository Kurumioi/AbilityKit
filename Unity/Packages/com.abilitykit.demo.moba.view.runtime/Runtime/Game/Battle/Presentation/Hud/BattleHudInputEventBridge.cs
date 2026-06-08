using System;
using AbilityKit.Game.Battle.View;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputEventBridge : IDisposable
    {
        private readonly IBattleHudInputSink _hudInput;
        private BattleHudInputUi _ui;

        public BattleHudInputEventBridge(IBattleHudInputSink hudInput)
        {
            _hudInput = hudInput;
        }

        public void Bind(BattleHudInputUi ui)
        {
            Unbind();
            _ui = ui;
            if (_ui == null) return;

            if (_ui.MoveJoystick != null)
            {
                _ui.MoveJoystick.OnBegin += OnMoveBegin;
                _ui.MoveJoystick.OnEnd += OnMoveEnd;
            }

            if (_ui.MoveMapper != null)
            {
                _ui.MoveMapper.MoveDxDzChanged += OnMoveDxDzChanged;
            }

            if (_ui.SkillAimMapper != null)
            {
                _ui.SkillAimMapper.SkillAimStart += OnSkillAimStart;
                _ui.SkillAimMapper.SkillAimUpdate += OnSkillAimUpdate;
                _ui.SkillAimMapper.SkillAimEnd += OnSkillAimEnd;
            }

            if (_ui.InputView != null)
            {
                _ui.InputView.SkillClick += OnSkillClick;
            }
        }

        public void Unbind()
        {
            if (_ui == null) return;

            if (_ui.MoveJoystick != null)
            {
                _ui.MoveJoystick.OnBegin -= OnMoveBegin;
                _ui.MoveJoystick.OnEnd -= OnMoveEnd;
            }

            if (_ui.MoveMapper != null)
            {
                _ui.MoveMapper.MoveDxDzChanged -= OnMoveDxDzChanged;
            }

            if (_ui.SkillAimMapper != null)
            {
                _ui.SkillAimMapper.SkillAimStart -= OnSkillAimStart;
                _ui.SkillAimMapper.SkillAimUpdate -= OnSkillAimUpdate;
                _ui.SkillAimMapper.SkillAimEnd -= OnSkillAimEnd;
            }

            if (_ui.InputView != null)
            {
                _ui.InputView.SkillClick -= OnSkillClick;
            }

            _ui = null;
        }

        public void ResetHudAim()
        {
            _hudInput?.SetHudSkillAim(0, 0f, 0f, aiming: false);
        }

        public void Dispose()
        {
            ResetHudAim();
            Unbind();
        }

        private void OnMoveBegin()
        {
            _hudInput?.BeginHudMove();
        }

        private void OnMoveEnd()
        {
            _hudInput?.EndHudMove();
        }

        private void OnMoveDxDzChanged(float dx, float dz)
        {
            _hudInput?.SetHudMove(dx, dz);
        }

        private void OnSkillClick(int slot)
        {
            _hudInput?.SubmitHudSkillClick(slot);
        }

        private void OnSkillAimStart(int slot, Vector2 aim)
        {
            _hudInput?.SetHudSkillAim(slot, aim.x, aim.y, aiming: true);
        }

        private void OnSkillAimUpdate(int slot, Vector2 aim)
        {
            _hudInput?.SetHudSkillAim(slot, aim.x, aim.y, aiming: true);
        }

        private void OnSkillAimEnd(int slot, Vector2 aim)
        {
            _hudInput?.SubmitHudSkillAim(slot, aim.x, aim.y);
        }
    }
}
