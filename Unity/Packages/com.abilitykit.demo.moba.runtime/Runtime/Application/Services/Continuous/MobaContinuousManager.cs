using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA-scoped continuous behavior manager for buffs, skill pipelines, movement, and other runtime processes.
    /// </summary>
    [WorldService(typeof(IContinuousManager), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaContinuousManager), WorldLifetime.Scoped)]
    public sealed class MobaContinuousManager : DefaultContinuousManager, IWorldInitializable
    {
        private readonly List<IMobaContinuousIntervalHandler> _intervalHandlers = new List<IMobaContinuousIntervalHandler>();
        private MobaContinuousModifierProjectorRegistry _modifierProjectors;
        private MobaContinuousLifecycleBinder _lifecycleBinder;
        private IMobaEffectiveTagQueryService _effectiveTags;
        private IMobaContinuousTagRuleService _tagRules;
        private IWorldResolver _services;

        public void OnInit(IWorldResolver services)
        {
            if (services == null) return;

            _services = services;
            RegisterDefaultModifierProjectors(services);
            RegisterDefaultIntervalHandlers(services);
        }

        public void RegisterIntervalHandler(IMobaContinuousIntervalHandler handler)
        {
            if (handler == null) return;
            if (_intervalHandlers.Contains(handler)) return;
 
            _intervalHandlers.Add(handler);
        }

        public void Reproject(IContinuous continuous)
        {
            if (continuous == null || continuous.IsTerminated) return;
            SyncManagedState(continuous);
            ReprojectModifiers(continuous);
            MarkEffectiveTagsDirty(continuous);
            ResolveTagRules()?.ReconcileOwnerFor(continuous);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f) return;

            var active = GetAllActiveContinuous();
            if (active == null || active.Count == 0) return;

            for (int i = 0; i < active.Count; i++)
            {
                var continuous = active[i];
                if (continuous == null || !continuous.IsActive || continuous.IsTerminated) continue;

                if (continuous is IMobaTickableContinuous tickable)
                {
                    tickable.TickManaged(deltaTimeSeconds);
                }

                if (continuous.IsTerminated) continue;
                TickInterval(continuous, deltaTimeSeconds);
            }
        }

        private void TickInterval(IContinuous continuous, float deltaTimeSeconds)
        {
            if (continuous == null) return;
            if (!(continuous.Config is IMobaContinuousPeriodicConfig periodic)) return;
            if (!(continuous is IMobaContinuousIntervalState state)) return;

            var intervalSeconds = periodic.IntervalSeconds;
            var triggerIds = periodic.IntervalEffectIds;
            if (intervalSeconds <= 0f || triggerIds == null || triggerIds.Count == 0)
            {
                state.IntervalRemainingSeconds = 0f;
                SyncManagedState(continuous);
                return;
            }

            if (state.IntervalRemainingSeconds <= 0f || state.IntervalRemainingSeconds > intervalSeconds)
            {
                state.IntervalRemainingSeconds = intervalSeconds;
            }

            state.IntervalRemainingSeconds -= deltaTimeSeconds;
            var guard = 0;
            while (state.IntervalRemainingSeconds <= 0f && guard++ < 16)
            {
                if (continuous.IsTerminated) break;

                state.IntervalRemainingSeconds += intervalSeconds;
                SyncManagedState(continuous);
                DispatchInterval(continuous, periodic);
            }

            SyncManagedState(continuous);
        }

        private void DispatchInterval(IContinuous continuous, IMobaContinuousPeriodicConfig periodic)
        {
            for (int i = 0; i < _intervalHandlers.Count; i++)
            {
                var handler = _intervalHandlers[i];
                if (handler == null || !handler.CanHandle(continuous)) continue;

                handler.OnInterval(continuous, periodic);
            }
        }

        private void RegisterDefaultIntervalHandlers(IWorldResolver services)
        {
            services.TryResolve(out MobaConfigDatabase configs);
            services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            services.TryResolve(out MobaEffectExecutionService effects);

            var buffEvents = new BuffEventPublisher(eventBus);
            var stageEffects = new BuffStageEffectExecutor(effects);
            RegisterIntervalHandler(new BuffContinuousIntervalHandler(configs, buffEvents, stageEffects));
        }

        private void RegisterDefaultModifierProjectors(IWorldResolver services)
        {
            _modifierProjectors = new MobaContinuousModifierProjectorRegistry();
            _modifierProjectors.Register(new MobaAttributeContinuousModifierProjector());
            _modifierProjectors.OnInit(services);

            _lifecycleBinder = new MobaContinuousLifecycleBinder(_modifierProjectors);
            AddLifecycleBinder(_lifecycleBinder);
        }

        private void ReprojectModifiers(IContinuous continuous)
        {
            if (_lifecycleBinder == null || continuous == null) return;
            _lifecycleBinder.Reproject(continuous);
        }

        private static void SyncManagedState(IContinuous continuous)
        {
            if (continuous is IMobaContinuousRuntimeStateSync sync)
            {
                sync.SyncManagedState();
            }
        }

        private void MarkEffectiveTagsDirty(IContinuous continuous)
        {
            var ownerId = continuous?.Config?.OwnerId ?? 0L;
            if (ownerId <= 0 || ownerId > int.MaxValue) return;

            ResolveEffectiveTags()?.MarkDirty((int)ownerId);
        }

        /// <summary>
        /// Releases all registered continuous behaviors when the MOBA world scope is disposed.
        /// </summary>
        public void Dispose()
        {
            if (_lifecycleBinder != null)
            {
                RemoveLifecycleBinder(_lifecycleBinder);
                _lifecycleBinder = null;
            }

            _intervalHandlers.Clear();
            _modifierProjectors = null;
            _effectiveTags = null;
            _tagRules = null;
            _services = null;
            Clear();
        }

        private IMobaEffectiveTagQueryService ResolveEffectiveTags()
        {
            if (_effectiveTags == null)
            {
                _services?.TryResolve(out _effectiveTags);
            }

            return _effectiveTags;
        }

        private IMobaContinuousTagRuleService ResolveTagRules()
        {
            if (_tagRules == null)
            {
                _services?.TryResolve(out _tagRules);
            }

            return _tagRules;
        }
    }
}
