using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Flow;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// 战斗实体 Feature
    /// 对齐 Unity BattleEntityFeature，管理 ECS 实体世界的创建和销毁
    /// </summary>
    public sealed class BattleEntityFeature : IGameModule<ConsoleBattleContext>
    {
        private EC.EntityWorld _world;
        private EC.IEntity _node;
        private BattleEntityLookup _lookup;
        private BattleEntityFactory _factory;

        public EC.IECWorld World => _world;
        public BattleEntityLookup Lookup => _lookup;
        public BattleEntityFactory Factory => _factory;

        /// <summary>
        /// OnAttach: 创建 ECS 世界和实体组件
        /// </summary>
        public void OnAttach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[BattleEntityFeature] OnAttach failed: context is null");
                return;
            }

            // 创建 ECS 世界
            _world = new EC.EntityWorld();
            _node = _world.Create("BattleEntities");

            // 创建实体组件
            _lookup = new BattleEntityLookup();
            _factory = new BattleEntityFactory(_world, _lookup, _node);

            // 注入到 Context
            context.EcsWorld = _world;
            context.EntityNode = _node;
            context.EntityLookup = _lookup;
            context.EntityFactory = _factory;

            Platform.Log.Entity("[BattleEntityFeature] ECS World created");
        }

        /// <summary>
        /// OnDetach: 销毁所有实体并清理
        /// </summary>
        public void OnDetach(ConsoleBattleContext context)
        {
            // 清空 Context 引用
            if (context != null)
            {
                context.EcsWorld = null;
                context.EntityNode = default;
                context.EntityLookup = null;
                context.EntityFactory = null;
            }

            // 销毁实体树
            if (_node.IsValid)
            {
                DestroyTree(_node);
            }

            _lookup?.Clear();
            _world = null;
            _lookup = null;
            _factory = null;
            _node = default;

            Platform.Log.Entity("[BattleEntityFeature] ECS World destroyed");
        }

        private static void DestroyTree(EC.IEntity root)
        {
            if (!root.IsValid) return;

            var list = new List<EC.IEntity>(16);
            var stack = new Stack<EC.IEntity>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var e = stack.Pop();
                if (!e.IsValid) continue;
                list.Add(e);

                var count = e.ChildCount;
                for (int i = 0; i < count; i++)
                {
                    stack.Push(e.GetChild(i));
                }
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.IsValid) e.Destroy();
            }
        }
    }
}
