using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.ECS;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// 战斗实体 Feature
    /// 对齐 Unity BattleEntityFeature，管理 ECS 实体世界的创建和销毁
    /// 实现 IFeature 接口，可被 FeatureHost 管理生命周期
    /// </summary>
    public sealed class BattleEntityFeature : IFeature
    {
        private EC.EntityWorld _world;
        private EC.IEntity _node;
        private BattleEntityLookup _lookup;
        private BattleEntityFactory _factory;
        private BattleEntityQuery _query;

        public static readonly string FeatureId = "BattleEntityFeature";

        public string Id => FeatureId;
        public string[] Dependencies => null;

        public EC.IECWorld World => _world;
        public BattleEntityLookup Lookup => _lookup;
        public BattleEntityFactory Factory => _factory;
        public BattleEntityQuery Query => _query;

        public void OnAttach(IFeatureContext ctx)
        {
            if (ctx is not FeatureContextAdapter adapter || adapter.Context == null)
            {
                Platform.Log.Error("[BattleEntityFeature] OnAttach failed: invalid context");
                return;
            }

            var context = adapter.Context;
            OnAttachToContext(context);
        }

        public void OnDetach(IFeatureContext ctx)
        {
            if (ctx is not FeatureContextAdapter adapter || adapter.Context == null)
            {
                return;
            }

            var context = adapter.Context;
            OnDetachFromContext(context);
        }

        /// <summary>
        /// 附加到指定 Context（内部方法）
        /// </summary>
        private void OnAttachToContext(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[BattleEntityFeature] OnAttachToContext failed: context is null");
                return;
            }

            // 创建 ECS 世界
            _world = new EC.EntityWorld();
            _node = _world.Create("BattleEntities");

            // 创建实体组件
            _lookup = new BattleEntityLookup();
            _factory = new BattleEntityFactory(_world, _lookup, _node);
            _query = new BattleEntityQuery(_world, _lookup);

            // 注入到 Context
            context.EcsWorld = _world;
            context.EntityNode = _node;
            context.EntityLookup = _lookup;
            context.EntityFactory = _factory;
            context.EntityQuery = _query;

            Platform.Log.Entity("[BattleEntityFeature] ECS World created, EntityFactory and EntityQuery injected");
        }

        /// <summary>
        /// 从指定 Context 分离（内部方法）
        /// </summary>
        private void OnDetachFromContext(ConsoleBattleContext context)
        {
            // 清空 Context 引用
            if (context != null)
            {
                context.EcsWorld = null;
                context.EntityNode = default;
                context.EntityLookup = null;
                context.EntityFactory = null;
                context.EntityQuery = null;
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
            _query = null;
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
