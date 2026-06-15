using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Config.Plans;

namespace AbilityKit.Demo.Moba.Services.Triggering
{
    [WorldService(typeof(MobaTriggerPlanSubscriptionService))]
    public sealed class MobaTriggerPlanSubscriptionService : IWorldInitializable, IWorldDeinitializable
    {
        [WorldInject] private TriggerPlanJsonDatabase _db;
        [WorldInject] private TriggerRunner<AbilityKit.Ability.World.DI.IWorldResolver> _runner;
        [WorldInject(required: false)] private MobaEventSubscriptionRegistry _eventRegistry;

        private readonly Dictionary<int, TriggerPlanJsonDatabase.Record> _byTriggerId = new Dictionary<int, TriggerPlanJsonDatabase.Record>();
        private readonly Dictionary<int, Type> _argsTypeByTriggerId = new Dictionary<int, Type>();
        private readonly Dictionary<long, List<IDisposable>> _regsByOwnerKey = new Dictionary<long, List<IDisposable>>();

        private static readonly MethodInfo RegisterTypedMethod = typeof(MobaTriggerPlanSubscriptionService).GetMethod(nameof(RegisterTyped), BindingFlags.NonPublic | BindingFlags.Static);

        public bool ContainsOwnerKey(long ownerKey)
        {
            return ownerKey != 0 && _regsByOwnerKey.ContainsKey(ownerKey);
        }

        public void CopyActiveOwnerKeys(List<long> dest)
        {
            if (dest == null) return;
            dest.Clear();
            foreach (var kv in _regsByOwnerKey) dest.Add(kv.Key);
        }

        public void OnInit(IWorldResolver services)
        {
            try
            {
                var records = _db?.Records;
                if (records != null)
                {
                    for (int i = 0; i < records.Count; i++)
                    {
                        var r = records[i];
                        if (r.TriggerId <= 0) continue;
                        _byTriggerId[r.TriggerId] = r;
                    }
                    BuildArgsTypeCache(records);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaTriggerPlanSubscriptionService] build triggerId map failed");
            }
        }

        public void StartTriggers(IReadOnlyList<int> triggerIds, long ownerKey)
        {
            if (ownerKey == 0) return;
            if (triggerIds == null || triggerIds.Count == 0) return;
            if (_db == null || _runner == null) return;

            if (_regsByOwnerKey.ContainsKey(ownerKey))
            {
                Stop(ownerKey);
            }

            var regs = new List<IDisposable>(triggerIds.Count);

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;
                if (!_byTriggerId.TryGetValue(triggerId, out var record))
                {
                    Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId not found in plan db: {triggerId}");
                    continue;
                }

                if (record.EventId == 0)
                {
                    Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId has empty eventId: {triggerId}");
                    continue;
                }

                if (record.Scope != TriggerPlanScope.OwnerBound)
                {
                    Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId is not owner-bound. triggerId={triggerId} scope={record.Scope}");
                    continue;
                }

                try
                {
                    regs.Add(RegisterTyped(record));
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaTriggerPlanSubscriptionService] register plan failed. triggerId={triggerId}");
                }
            }

            if (regs.Count > 0)
            {
                _regsByOwnerKey[ownerKey] = regs;
            }
        }

        private void BuildArgsTypeCache(IReadOnlyList<TriggerPlanJsonDatabase.Record> records)
        {
            if (records == null || records.Count == 0) return;
            if (_eventRegistry == null)
            {
                throw new InvalidOperationException("MobaTriggerPlanSubscriptionService requires MobaEventSubscriptionRegistry for owner-bound typed trigger registration.");
            }

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.TriggerId <= 0) continue;
                if (string.IsNullOrEmpty(record.EventName)) continue;
                if (!_eventRegistry.TryGetArgsType(record.EventName, out var argsType) || argsType == null)
                {
                    throw new InvalidOperationException($"Owner-bound trigger event is not registered. triggerId={record.TriggerId} eventName={record.EventName}");
                }

                if (!argsType.IsClass)
                {
                    throw new InvalidOperationException($"Owner-bound trigger event args type must be a class. triggerId={record.TriggerId} eventName={record.EventName} argsType={argsType.FullName}");
                }

                _argsTypeByTriggerId[record.TriggerId] = argsType;
            }
        }

        private IDisposable RegisterTyped(in TriggerPlanJsonDatabase.Record record)
        {
            if (!_argsTypeByTriggerId.TryGetValue(record.TriggerId, out var argsType) || argsType == null)
            {
                throw new InvalidOperationException($"Owner-bound trigger missing typed event args mapping. triggerId={record.TriggerId} eventName={record.EventName}");
            }

            if (RegisterTypedMethod == null)
            {
                throw new MissingMethodException(nameof(MobaTriggerPlanSubscriptionService), nameof(RegisterTyped));
            }

            var mi = RegisterTypedMethod.MakeGenericMethod(argsType);
            var registration = (IDisposable)mi.Invoke(null, new object[] { _runner, record.EventId, record.Plan });
            if (registration == null)
            {
                throw new InvalidOperationException($"Owner-bound trigger typed registration returned null. triggerId={record.TriggerId} eventName={record.EventName} eid={record.EventId}");
            }

            return registration;
        }

        private static IDisposable RegisterTyped<TArgs>(TriggerRunner<IWorldResolver> runner, int eid, TriggerPlan<object> planObj) where TArgs : class
        {
            if (runner == null) return null;
            if (eid == 0) return null;

            var plan = ConvertPlan<TArgs>(in planObj);
            var key = new EventKey<TArgs>(eid);
            return runner.RegisterPlan<TArgs, IWorldResolver>(key, in plan);
        }

        private static TriggerPlan<TArgs> ConvertPlan<TArgs>(in TriggerPlan<object> src)
        {
            var actions = src.Actions;

            if (!src.HasPredicate || src.PredicateKind == EPredicateKind.None)
            {
                return new TriggerPlan<TArgs>(src.Phase, src.Priority, src.TriggerId, actions, src.InterruptPriority, src.Cue, src.Schedule, src.ExecutionControl);
            }

            if (src.PredicateKind == EPredicateKind.Expr)
            {
                return new TriggerPlan<TArgs>(src.Phase, src.Priority, src.TriggerId, src.PredicateExpr, actions, src.InterruptPriority, src.Cue, src.Schedule, src.ExecutionControl);
            }

            switch (src.PredicateArity)
            {
                case 0:
                    return new TriggerPlan<TArgs>(phase: src.Phase, priority: src.Priority, triggerId: src.TriggerId, predicateId: src.PredicateId, predicateArgs: null, actions: actions, interruptPriority: src.InterruptPriority, cue: src.Cue, schedule: src.Schedule, executionControl: src.ExecutionControl);
                case 1:
                    return new TriggerPlan<TArgs>(phase: src.Phase, priority: src.Priority, triggerId: src.TriggerId, predicateId: src.PredicateId, predicateArgs: new[] { src.PredicateArg0 }, actions: actions, interruptPriority: src.InterruptPriority, cue: src.Cue, schedule: src.Schedule, executionControl: src.ExecutionControl);
                case 2:
                    return new TriggerPlan<TArgs>(phase: src.Phase, priority: src.Priority, triggerId: src.TriggerId, predicateId: src.PredicateId, predicateArgs: new[] { src.PredicateArg0, src.PredicateArg1 }, actions: actions, interruptPriority: src.InterruptPriority, cue: src.Cue, schedule: src.Schedule, executionControl: src.ExecutionControl);
                default:
                    return new TriggerPlan<TArgs>(phase: src.Phase, priority: src.Priority, triggerId: src.TriggerId, actions: actions, interruptPriority: src.InterruptPriority, cue: src.Cue, schedule: src.Schedule, executionControl: src.ExecutionControl);
            }
        }

        public void Stop(long ownerKey)
        {
            if (ownerKey == 0) return;
            if (!_regsByOwnerKey.TryGetValue(ownerKey, out var regs) || regs == null) return;

            _regsByOwnerKey.Remove(ownerKey);

            for (int i = 0; i < regs.Count; i++)
            {
                try
                {
                    regs[i]?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaTriggerPlanSubscriptionService] dispose reg failed. ownerKey={ownerKey}");
                }
            }
        }

        public void OnDeinit(IWorldResolver services)
        {
            var keys = new List<long>(_regsByOwnerKey.Keys);
            for (int i = 0; i < keys.Count; i++) Stop(keys[i]);
        }

        public void Dispose()
        {
            _byTriggerId.Clear();
            _argsTypeByTriggerId.Clear();
            _regsByOwnerKey.Clear();
        }
    }
}
