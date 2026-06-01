using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Systems.ContinuousPeriodic
{
    [WorldSystem(order: MobaSystemOrder.ContinuousPeriodicTick, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaContinuousPeriodicTickSystem : WorldSystemBase
    {
        private IWorldClock _clock;
        private MobaPeriodicEffectService _periodic;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaContinuousPeriodicTickSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _clock);
            Services.TryResolve(out _periodic);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.MobaContinuousPeriodic));
        }

        protected override void OnExecute()
        {
            if (_clock == null) return;
            var dt = _clock.DeltaTime;
            if (dt <= 0f) return;

            if (_periodic == null) return;

            var addMs = (int)System.Math.Round(dt * 1000f);
            if (addMs <= 0) return;

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasMobaContinuousPeriodic) continue;

                var list = e.mobaContinuousPeriodic.Active;
                if (list == null || list.Count == 0) continue;

                for (int j = list.Count - 1; j >= 0; j--)
                {
                    var rt = list[j];
                    if (rt == null)
                    {
                        list.RemoveAt(j);
                        continue;
                    }

                    if (rt.IsStopped)
                    {
                        list.RemoveAt(j);
                        _periodic.NotifyRuntimeRemoved(rt);
                        continue;
                    }

                    if (!rt.Started)
                    {
                        _periodic.ExecuteRuntimePhase(rt, MobaContinuousPeriodicPhase.Start);
                        rt.Started = true;
                    }

                    rt.ElapsedMs += addMs;

                    if (rt.RemainingMs > 0)
                    {
                        rt.RemainingMs -= addMs;
                        if (rt.RemainingMs <= 0)
                        {
                            _periodic.ExecuteRuntimePhase(rt, MobaContinuousPeriodicPhase.Stop);

                            list.RemoveAt(j);
                            _periodic.NotifyRuntimeRemoved(rt);
                            continue;
                        }
                    }

                    var periodMs = rt.EffectivePeriodMs;
                    if (periodMs > 0 && HasTickAction(rt))
                    {
                        rt.NextTickMs -= addMs;
                        while (rt.NextTickMs <= 0)
                        {
                            _periodic.ExecuteRuntimePhase(rt, MobaContinuousPeriodicPhase.Tick);

                            periodMs = rt.EffectivePeriodMs;
                            if (periodMs <= 0)
                            {
                                rt.NextTickMs = 0;
                                break;
                            }

                            rt.NextTickMs += periodMs;
                        }
                    }
                }
            }
        }

        private static bool HasTickAction(MobaContinuousPeriodicRuntime runtime)
        {
            if (runtime == null) return false;
            if (runtime.OnTickTriggerId > 0) return true;
            if (runtime.OnTickTriggerIds != null && runtime.OnTickTriggerIds.Count > 0) return true;
            return runtime.OnTickEffectIds != null && runtime.OnTickEffectIds.Count > 0;
        }
    }
}
