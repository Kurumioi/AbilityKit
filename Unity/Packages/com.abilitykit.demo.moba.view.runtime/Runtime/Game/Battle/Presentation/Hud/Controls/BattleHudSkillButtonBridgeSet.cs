using System;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;

namespace AbilityKit.Game.Battle.View
{
    internal sealed class BattleHudSkillButtonBridgeSet
    {
        private readonly BattleHudSkillButtonEventBridge[] _bridges;

        public BattleHudSkillButtonBridgeSet(BattleHudSkillButtonBridgeSetFactory factory = null)
        {
            factory ??= new BattleHudSkillButtonBridgeSetFactory();
            _bridges = factory.CreateBridges();

            for (int i = 0; i < _bridges.Length; i++)
            {
                var bridge = _bridges[i];
                bridge.Click += OnClick;
                bridge.LongPress += OnLongPress;
                bridge.AimStart += OnAimStart;
                bridge.AimUpdate += OnAimUpdate;
                bridge.AimEnd += OnAimEnd;
            }
        }

        public event Action<int> Click;
        public event Action<int> LongPress;
        public event Action<int, Vector2> AimStart;
        public event Action<int, Vector2> AimUpdate;
        public event Action<int, Vector2> AimEnd;

        public void Bind(SkillButtonView skill1, SkillButtonView skill2, SkillButtonView skill3)
        {
            _bridges[0].Bind(skill1, 1);
            _bridges[1].Bind(skill2, 2);
            _bridges[2].Bind(skill3, 3);
        }

        public void Unbind()
        {
            for (int i = 0; i < _bridges.Length; i++)
            {
                _bridges[i].Unbind();
            }
        }

        private void OnClick(int slot)
        {
            Click?.Invoke(slot);
        }

        private void OnLongPress(int slot)
        {
            LongPress?.Invoke(slot);
        }

        private void OnAimStart(int slot, Vector2 aim)
        {
            AimStart?.Invoke(slot, aim);
        }

        private void OnAimUpdate(int slot, Vector2 aim)
        {
            AimUpdate?.Invoke(slot, aim);
        }

        private void OnAimEnd(int slot, Vector2 aim)
        {
            AimEnd?.Invoke(slot, aim);
        }
    }

    internal sealed class BattleHudSkillButtonBridgeSetFactory
    {
        public BattleHudSkillButtonEventBridge[] CreateBridges()
        {
            return new[]
            {
                new BattleHudSkillButtonEventBridge(),
                new BattleHudSkillButtonEventBridge(),
                new BattleHudSkillButtonEventBridge(),
            };
        }
    }
}
