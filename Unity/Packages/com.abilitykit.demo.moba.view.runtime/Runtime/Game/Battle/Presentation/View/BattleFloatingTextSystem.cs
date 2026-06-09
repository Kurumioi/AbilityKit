using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.View
{
    public sealed class BattleFloatingTextSystem
    {
        private readonly BattleFloatingTextStore _floatingTexts;
        private readonly BattleWorldFloatingTextFactory _factory;

        public BattleFloatingTextSystem()
            : this(null, null)
        {
        }

        internal BattleFloatingTextSystem(
            BattleWorldFloatingTextFactory factory,
            BattleFloatingTextSystemFactory systemFactory = null)
        {
            systemFactory ??= new BattleFloatingTextSystemFactory();

            _floatingTexts = systemFactory.CreateStore();
            _factory = factory ?? systemFactory.CreateFloatingTextFactory();
        }

        public void Spawn(in EC.IEntity vfxNode, string text, in Vector3 worldPos, Color color)
        {
            if (!vfxNode.IsValid) return;

            var floatingText = _factory.Create(text, in worldPos, color);
            _floatingTexts.Add(floatingText);
        }

        public void Tick(float deltaTime)
        {
            _floatingTexts.Tick(deltaTime);
        }

        public void Clear()
        {
            _floatingTexts.Clear();
        }
    }

    internal sealed class BattleFloatingTextSystemFactory
    {
        public BattleFloatingTextStore CreateStore()
        {
            return new BattleFloatingTextStore();
        }

        public BattleWorldFloatingTextFactory CreateFloatingTextFactory()
        {
            return new BattleWorldFloatingTextFactory();
        }
    }
}
