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

        private readonly BattleHudInputControlBinding _binding = new BattleHudInputControlBinding();

        public event Action<JoystickOutput> MoveChanged
        {
            add => _binding.MoveChanged += value;
            remove => _binding.MoveChanged -= value;
        }

        public event Action<int> SkillClick
        {
            add => _binding.SkillClick += value;
            remove => _binding.SkillClick -= value;
        }

        public event Action<int> SkillLongPress
        {
            add => _binding.SkillLongPress += value;
            remove => _binding.SkillLongPress -= value;
        }

        public event Action<int, Vector2> SkillAimStart
        {
            add => _binding.SkillAimStart += value;
            remove => _binding.SkillAimStart -= value;
        }

        public event Action<int, Vector2> SkillAimUpdate
        {
            add => _binding.SkillAimUpdate += value;
            remove => _binding.SkillAimUpdate -= value;
        }

        public event Action<int, Vector2> SkillAimEnd
        {
            add => _binding.SkillAimEnd += value;
            remove => _binding.SkillAimEnd -= value;
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
            _binding.Bind(_moveJoystick, _skill1, _skill2, _skill3);
        }

        private void UnhookAll()
        {
            _binding.Unbind();
        }
    }
}
