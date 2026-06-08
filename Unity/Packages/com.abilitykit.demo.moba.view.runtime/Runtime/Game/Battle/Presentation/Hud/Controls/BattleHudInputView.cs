using System;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;

namespace AbilityKit.Game.Battle.View
{
    public sealed class BattleHudInputView : MonoBehaviour
    {
        [SerializeField] private JoystickAreaView _moveJoystick;
        [SerializeField] private SkillButtonView _skill1;
        [SerializeField] private SkillButtonView _skill2;
        [SerializeField] private SkillButtonView _skill3;

        private readonly BattleHudSkillButtonBridgeSet _skillButtons = new BattleHudSkillButtonBridgeSet();

        private bool _isHooked;

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

        public void Initialize(
            JoystickAreaView moveJoystick,
            SkillButtonView skill1,
            SkillButtonView skill2,
            SkillButtonView skill3)
        {
            UnhookAll();

            _moveJoystick = moveJoystick;
            _skill1 = skill1;
            _skill2 = skill2;
            _skill3 = skill3;

            if (isActiveAndEnabled)
            {
                HookAll();
            }
        }

        private void OnEnable()
        {
            HookAll();
        }

        private void OnDisable()
        {
            UnhookAll();
        }

        private void OnDestroy()
        {
            UnhookAll();
        }

        private void HookAll()
        {
            if (_isHooked) return;

            _isHooked = true;

            if (_moveJoystick != null)
            {
                _moveJoystick.OnValueChanged += OnMoveChanged;
            }

            _skillButtons.Bind(_skill1, _skill2, _skill3);
        }

        private void UnhookAll()
        {
            if (!_isHooked) return;
            _isHooked = false;

            if (_moveJoystick != null)
            {
                _moveJoystick.OnValueChanged -= OnMoveChanged;
            }

            _skillButtons.Unbind();
        }

        private void OnMoveChanged(JoystickOutput output)
        {
            MoveChanged?.Invoke(output);
        }
    }
}
