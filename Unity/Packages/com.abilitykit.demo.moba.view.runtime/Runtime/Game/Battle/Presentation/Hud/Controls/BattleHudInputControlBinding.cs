using System;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;

namespace AbilityKit.Game.Battle.View
{
    internal sealed class BattleHudInputControlBinding
    {
        private readonly BattleHudSkillButtonBridgeSet _skillButtons;

        private JoystickAreaView _moveJoystick;
        private SkillButtonView _skill1;
        private SkillButtonView _skill2;
        private SkillButtonView _skill3;
        private bool _isBound;

        public BattleHudInputControlBinding(BattleHudSkillButtonBridgeSet skillButtons = null)
        {
            _skillButtons = skillButtons ?? new BattleHudSkillButtonBridgeSet();
        }

        public event Action<JoystickOutput> MoveChanged;

        public event Action<int> SkillClick
        {
            add => _skillButtons.Click += value;
            remove => _skillButtons.Click -= value;
        }

        public event Action<int> SkillLongPress
        {
            add => _skillButtons.LongPress += value;
            remove => _skillButtons.LongPress -= value;
        }

        public event Action<int, Vector2> SkillAimStart
        {
            add => _skillButtons.AimStart += value;
            remove => _skillButtons.AimStart -= value;
        }

        public event Action<int, Vector2> SkillAimUpdate
        {
            add => _skillButtons.AimUpdate += value;
            remove => _skillButtons.AimUpdate -= value;
        }

        public event Action<int, Vector2> SkillAimEnd
        {
            add => _skillButtons.AimEnd += value;
            remove => _skillButtons.AimEnd -= value;
        }

        public void Bind(
            JoystickAreaView moveJoystick,
            SkillButtonView skill1,
            SkillButtonView skill2,
            SkillButtonView skill3)
        {
            if (_isBound &&
                _moveJoystick == moveJoystick &&
                _skill1 == skill1 &&
                _skill2 == skill2 &&
                _skill3 == skill3)
            {
                return;
            }

            Unbind();

            _moveJoystick = moveJoystick;
            _skill1 = skill1;
            _skill2 = skill2;
            _skill3 = skill3;
            _isBound = true;

            if (_moveJoystick != null)
            {
                _moveJoystick.OnValueChanged += OnMoveChanged;
            }

            _skillButtons.Bind(_skill1, _skill2, _skill3);
        }

        public void Unbind()
        {
            if (!_isBound) return;

            if (_moveJoystick != null)
            {
                _moveJoystick.OnValueChanged -= OnMoveChanged;
            }

            _skillButtons.Unbind();

            _moveJoystick = null;
            _skill1 = null;
            _skill2 = null;
            _skill3 = null;
            _isBound = false;
        }

        private void OnMoveChanged(JoystickOutput output)
        {
            MoveChanged?.Invoke(output);
        }
    }
}
