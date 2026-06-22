using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services.Buffs;
using AbilityKit.Demo.Moba.Services.Buffs.Presentation;
using AbilityKit.Demo.Moba.Services.Buffs.Runtime;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 作用域内的持续行为管理器，用于管理 Buff、技能管线、移动和其他运行时过程。
    /// </summary>
    [WorldService(typeof(IContinuousManager), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaContinuousManager), WorldLifetime.Scoped)]
    public sealed class MobaContinuousManager : DefaultContinuousManager, IWorldInitializable, System.IDisposable
    {
        private readonly List<IMobaContinuousIntervalHandler> _intervalHandlers = new List<IMobaContinuousIntervalHandler>();
        private MobaContinuousModifierProjectorRegistry _modifierProjectors;
        private MobaContinuousTickProcessor _tickProcessor;
        private MobaContinuousLifecycleBinder _lifecycleBinder;
        private MobaContinuousContextLifecycleBinder _contextLifecycleBinder;
        private BuffContinuousIntervalHandler _buffIntervalHandler;

        public void OnInit(IWorldResolver services)
        {
            services.TryResolve(out MobaConfigDatabase configs);
            services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            services.TryResolve(out MobaEffectExecutionService effects);
            services.TryResolve(out MobaTraceRegistry trace);
            services.TryResolve(out AbilityKit.Ability.Triggering.Runtime.ITriggerActionRunner actionRunner);
            _modifierProjectors = new MobaContinuousModifierProjectorRegistry();
            _modifierProjectors.OnInit(services);
            _lifecycleBinder = new MobaContinuousLifecycleBinder(_modifierProjectors);
            AddLifecycleBinder(_lifecycleBinder);
            _contextLifecycleBinder = new MobaContinuousContextLifecycleBinder(trace, actionRunner);
            AddLifecycleBinder(_contextLifecycleBinder);
            _buffIntervalHandler = new BuffContinuousIntervalHandler(configs, null, null, null);
            _intervalHandlers.Add(_buffIntervalHandler);
            _tickProcessor = new MobaContinuousTickProcessor(_intervalHandlers);
        }

        public void Reproject(IContinuous continuous)
        {
            _lifecycleBinder?.Reproject(continuous);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f) return;

            var active = GetAllActiveContinuous();
            for (var i = 0; i < active.Count; i++)
            {
                var continuous = active[i];
                if (continuous == null || continuous.IsTerminated || !continuous.IsActive || continuous.IsPaused) continue;

                if (continuous is IMobaTickableContinuous tickable)
                {
                    tickable.TickManaged(deltaTimeSeconds);
                }

                _tickProcessor?.Tick(continuous, deltaTimeSeconds);

                if (continuous is IMobaContinuousRuntimeStateSync stateSync)
                {
                    stateSync.SyncManagedState();
                }
            }
        }

        public void Dispose()
        {
            _intervalHandlers.Clear();
            _buffIntervalHandler = null;
            _tickProcessor = null;
            _contextLifecycleBinder = null;
            _lifecycleBinder = null;
            _modifierProjectors = null;
        }
    }
}
