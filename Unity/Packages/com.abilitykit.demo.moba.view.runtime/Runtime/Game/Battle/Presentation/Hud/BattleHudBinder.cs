using AbilityKit.Game.Battle.Entity;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudBinder : IBattleHudActorPositionResolver
    {
        private readonly BattleContext _ctx;
        private readonly BattleHudHpBarController _hpBars;
        private readonly BattleHudFloatingTextController _floatingTexts;

        public BattleHudBinder(BattleHudConfig cfg, RectTransform root, Camera camera, BattleContext ctx)
        {
            _ctx = ctx;

            var projector = new BattleHudCanvasProjector(root, camera);
            _hpBars = new BattleHudHpBarController(cfg, root, projector, this);
            _floatingTexts = new BattleHudFloatingTextController(cfg, root, projector, this);
        }

        public void OnDamageEvents(MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null) return;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.TargetActorId <= 0) continue;

                var absValue = Mathf.Abs(entry.Value);
                if (absValue <= 0.0001f) continue;

                _hpBars.Ensure(entry.TargetActorId);
                _hpBars.UpdateHp(entry.TargetActorId, entry.TargetHp, entry.TargetMaxHp);

                var isHeal = entry.Kind == (int)DamageEventKind.Heal;
                var sign = isHeal ? "+" : "-";
                var text = sign + Mathf.RoundToInt(absValue).ToString();
                _floatingTexts.Spawn(entry.TargetActorId, text, isHeal);
            }
        }

        public void Tick(float deltaTime)
        {
            _hpBars.Tick();
            _floatingTexts.Tick(deltaTime);
        }

        public void OnEntityDestroyed(EC.IEntityId id)
        {
            if (!TryResolveActorId(id, out var actorId)) return;

            _hpBars.RemoveActor(actorId);
            _floatingTexts.RemoveActor(actorId);
        }

        public void Clear()
        {
            _hpBars.Clear();
            _floatingTexts.Clear();
        }

        public bool TryGetActorWorldPos(int actorId, out Vector3 pos)
        {
            pos = default;
            if (_ctx?.EntityQuery == null) return false;
            if (!_ctx.EntityQuery.TryResolve(new BattleNetId(actorId), out var entity)) return false;
            if (!entity.TryGetRef(out AbilityKit.Game.Battle.Component.BattleTransformComponent transform) || transform == null) return false;

            pos = transform.Position;
            return true;
        }

        private bool TryResolveActorId(EC.IEntityId id, out int actorId)
        {
            actorId = 0;
            if (_ctx?.EntityQuery == null) return false;
            if (!_ctx.EntityQuery.World.IsAlive(id)) return false;

            var entity = _ctx.EntityQuery.World.Wrap(id);
            if (!entity.TryGetRef(out BattleNetIdComponent netIdComp) || netIdComp == null) return false;

            actorId = netIdComp.NetId.Value;
            return actorId > 0;
        }
    }
}
