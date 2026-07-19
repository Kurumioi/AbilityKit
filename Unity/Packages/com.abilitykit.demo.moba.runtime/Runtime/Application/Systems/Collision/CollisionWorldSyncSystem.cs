using System;
using System.Collections.Generic;
using AbilityKit.Combat.Collision;
using AbilityKit.Core.Mathematics;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using Entitas;

namespace AbilityKit.Demo.Moba.Systems.Collision
{
    /// <summary>
    /// 碰撞世界同步系统
    /// 在每帧执行时同步 Entitas 实体与碰撞世界
    /// </summary>
    [WorldSystem(MobaSystemOrder.Base + WorldSystemOrder.Early, Phase = WorldSystemPhase.PreExecute)]
    public sealed class CollisionWorldSyncSystem : WorldSystemBase
    {
        private readonly ICollisionWorld _world;
        private readonly IGroup<global::ActorEntity> _withShape;
        private readonly IGroup<global::ActorEntity> _withCollisionId;

        private readonly HashSet<int> _validIds = new HashSet<int>();
        private readonly List<CollisionWorldDebugShape> _worldShapes = new List<CollisionWorldDebugShape>(2048);

        public CollisionWorldSyncSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
            if (!services.TryResolve<ICollisionService>(out var svc) || svc == null)
            {
                throw new InvalidOperationException("ICollisionService not registered");
            }

            _world = svc.World;
            var ctx = (global::Contexts)contexts;
            _withShape = ctx.actor.GetGroup(global::ActorMatcher.AllOf(
                global::ActorComponentsLookup.Transform,
                global::ActorComponentsLookup.Collider));
            _withCollisionId = ctx.actor.GetGroup(ActorMatcher.CollisionId);
        }

        protected override void OnExecute()
        {
            _validIds.Clear();

            // 添加或更新所有活跃碰撞体。
            var entities = _withShape.GetEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null) continue;
                if (!e.isEnabled) continue;
                if (!e.hasTransform || !e.hasCollider) continue;

                var t = e.transform.Value;
                var shape = e.collider.LocalShape;
                var layerMask = e.hasCollisionLayer ? e.collisionLayer.Mask : 0;
                var layerId = ResolveLayerId(layerMask);

                if (!e.hasCollisionId)
                {
                    var id = _world.Add(t, shape, layerId);
                    e.AddCollisionId(id);
                    _validIds.Add(id.Value);
                }
                else
                {
                    var id = e.collisionId.Value;
                    _world.Update(id, t, shape);
                    _world.UpdateLayer(id, layerId);
                    _validIds.Add(id.Value);
                }
            }

            // 移除已经失效的碰撞体（丢失 Transform/Collider）。
            var withIds = _withCollisionId.GetEntities();
            for (int i = 0; i < withIds.Length; i++)
            {
                var e = withIds[i];
                if (e == null) continue;
                if (!e.hasCollisionId) continue;

                if (!e.isEnabled || !e.hasTransform || !e.hasCollider)
                {
                    var id = e.collisionId.Value;
                    _world.Remove(id);
                    e.RemoveCollisionId();
                }
            }

            // 标记-清扫式清理：
            // 部分实体可能已销毁或禁用，不再出现在分组中，
            // 这会在碰撞世界中留下陈旧的碰撞体条目。
            // 这里保守移除所有未关联到当前活跃（Transform+Collider）实体的碰撞体 ID。
            if (_world is ICollisionWorldDebugView debugView)
            {
                debugView.CopyWorldShapes(_worldShapes);
                for (int i = 0; i < _worldShapes.Count; i++)
                {
                    var id = _worldShapes[i].Id;
                    if (_validIds.Contains(id.Value)) continue;
                    _world.Remove(id);
                }
            }
        }

        private static int ResolveLayerId(int layerMask)
        {
            if (layerMask == 0) return 0;
            if (layerMask < 0 || (layerMask & (layerMask - 1)) != 0)
            {
                throw new InvalidOperationException($"Actor collision layer must contain exactly one bit. mask=0x{layerMask:X8}");
            }

            var layerId = 0;
            while ((layerMask >>= 1) != 0)
            {
                layerId++;
            }
            return layerId;
        }
    }
}
