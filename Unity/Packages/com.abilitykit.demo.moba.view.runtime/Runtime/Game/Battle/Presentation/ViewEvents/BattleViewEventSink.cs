using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Ability.Triggering;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Effect;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    public sealed class BattleViewEventSink : IBattleViewEventSink
    {
        private readonly BattleAreaViewEventHandler _areaEvents;
        private readonly BattleDamageViewEventHandler _damageEvents;
        private readonly BattleProjectileViewEventHandler _projectileEvents;
        private readonly BattleViewDirtyEntityRefresher _dirtyViews;

        public BattleViewEventSink(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleViewBinder binder,
            BattleVfxManager vfx,
            EC.IEntity vfxNode,
            BattleFloatingTextSystem floatingTexts,
            BattleAreaViewSystem areaViews)
        {
            _areaEvents = new BattleAreaViewEventHandler(ctx, query, binder, areaViews);
            _damageEvents = new BattleDamageViewEventHandler(ctx, query, vfxNode, floatingTexts);
            _projectileEvents = new BattleProjectileViewEventHandler(ctx, query, vfx, vfxNode);
            _dirtyViews = new BattleViewDirtyEntityRefresher(ctx, query, binder);
        }

        public void OnTriggerEvent(in TriggerEvent evt)
        {
            if (evt.Id == null) return;

            if (evt.Id == DamagePipelineEvents.AfterApply)
            {
                if (evt.Payload is DamageResult result)
                {
                    _damageEvents.HandleDamageResult(result);
                }

                return;
            }

            if (evt.Id == ProjectileTriggering.Events.Hit)
            {
                _projectileEvents.HandleTriggerHit(evt);
            }
        }

        public void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res)
        {
            _dirtyViews.Refresh();
        }

        public void OnActorTransformSnapshot(ISnapshotEnvelope packet, MobaActorTransformSnapshotEntry[] entries)
        {
            _dirtyViews.Refresh();
        }

        public void OnProjectileEventSnapshot(ISnapshotEnvelope packet, MobaProjectileEventSnapshotEntry[] entries)
        {
            _projectileEvents.HandleSnapshot(entries);
        }

        public void OnAreaEventSnapshot(ISnapshotEnvelope packet, MobaAreaEventSnapshotEntry[] entries)
        {
            _areaEvents.HandleSnapshot(entries);
        }

        public void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotEntry[] entries)
        {
            _damageEvents.HandleSnapshot(entries);
        }
    }
}
