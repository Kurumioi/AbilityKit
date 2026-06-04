using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Event;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config.Plans;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Gameplay.Triggering
{
    [WorldService(typeof(MobaGameplayTriggerBindingService), WorldLifetime.Scoped)]
    public sealed class MobaGameplayTriggerBindingService : IService
    {
        [WorldInject(required: false)] private TriggerPlanJsonDatabase _triggerDb;
        [WorldInject(required: false)] private TriggerRunner<IWorldResolver> _runner;
        [WorldInject(required: false)] private MobaEventSubscriptionRegistry _eventRegistry;

        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        private int _boundGameplayId;

        public int BoundGameplayId => _boundGameplayId;

        public bool Bind(GameplayMO gameplay)
        {
            Unbind();

            if (gameplay == null)
            {
                Log.Warning("[MobaGameplayTriggerBindingService] bind skipped: gameplay is null");
                return false;
            }

            if (_triggerDb == null || _runner == null)
            {
                Log.Warning($"[MobaGameplayTriggerBindingService] bind skipped: missing trigger deps. gameplayId={gameplay.Id}");
                return false;
            }

            var ok = true;
            var triggerIds = gameplay.TriggerIds;
            if (triggerIds != null)
            {
                for (int i = 0; i < triggerIds.Count; i++)
                {
                    var triggerId = triggerIds[i];
                    if (triggerId <= 0)
                    {
                        continue;
                    }

                    if (!RegisterTrigger(triggerId, gameplay.Id))
                    {
                        ok = false;
                    }
                }
            }

            _boundGameplayId = gameplay.Id;
            Log.Info($"[MobaGameplayTriggerBindingService] gameplay triggers bound. gameplayId={gameplay.Id}, registrations={_registrations.Count}");
            return ok;
        }

        public void Unbind()
        {
            for (int i = _registrations.Count - 1; i >= 0; i--)
            {
                _registrations[i]?.Dispose();
            }

            _registrations.Clear();
            _boundGameplayId = 0;
        }

        public void Dispose()
        {
            Unbind();
        }

        private bool RegisterTrigger(int triggerId, int gameplayId)
        {
            if (!_triggerDb.TryGetRecordByTriggerId(triggerId, out var record))
            {
                Log.Error($"[MobaGameplayTriggerBindingService] gameplay trigger not found. gameplayId={gameplayId}, triggerId={triggerId}");
                return false;
            }

            if (record.EventId == 0 || record.Scope != TriggerPlanScope.Global)
            {
                Log.Error($"[MobaGameplayTriggerBindingService] invalid gameplay trigger record. gameplayId={gameplayId}, triggerId={triggerId}, eventId={record.EventId}, scope={record.Scope}");
                return false;
            }

            IDisposable registration;
            if (_eventRegistry != null
                && !string.IsNullOrEmpty(record.EventName)
                && _eventRegistry.TryGetArgsType(record.EventName, out var argsType)
                && argsType != null
                && argsType.IsClass)
            {
                registration = _runner.RegisterPlan(record.EventId, argsType, record.Plan);
            }
            else
            {
                var key = new EventKey<object>(record.EventId);
                registration = _runner.RegisterPlan<object, IWorldResolver>(key, record.Plan);
            }

            if (registration == null)
            {
                Log.Error($"[MobaGameplayTriggerBindingService] trigger registration returned null. gameplayId={gameplayId}, triggerId={triggerId}");
                return false;
            }

            _registrations.Add(registration);
            return true;
        }
    }
}
