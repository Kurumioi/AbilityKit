using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Pooling;
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
        private static readonly MethodInfo s_registerTypedAsMethod = typeof(MobaTriggerPlanSubscriptionService)
            .GetMethod(nameof(RegisterTypedAs), BindingFlags.Instance | BindingFlags.NonPublic);

        [WorldInject] private TriggerPlanJsonDatabase _db = null;
        [WorldInject] private TriggerRunner<AbilityKit.Ability.World.DI.IWorldResolver> _runner = null;
        [WorldInject] private AbilityKit.Triggering.Eventing.IEventBus _planEventBus = null;
        [WorldInject(required: false)] private MobaEventSubscriptionRegistry _eventRegistry = null;
        [WorldInject(required: false)] private MobaOwnerBoundTriggerGateService _ownerBoundGates = null;
        [WorldInject(required: false)] private MobaEffectExecutionService _effects = null;

        private readonly Dictionary<int, TriggerPlanJsonDatabase.Record> _byTriggerId = new Dictionary<int, TriggerPlanJsonDatabase.Record>();
        private readonly Dictionary<int, Type> _argsTypeByTriggerId = new Dictionary<int, Type>();
        private static readonly ObjectPool<List<int>> s_intListPool = Pools.GetPool(
            createFunc: () => new List<int>(8),
            onRelease: list => list.Clear(),
            defaultCapacity: 8,
            maxSize: 64,
            collectionCheck: false);

        private readonly Dictionary<long, Dictionary<int, IDisposable>> _regsByOwnerKey = new Dictionary<long, Dictionary<int, IDisposable>>();

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

        public void ApplyTriggers(IReadOnlyList<int> triggerIds, long ownerKey)
        {
            if (ownerKey == 0) return;
            if (triggerIds == null || triggerIds.Count == 0)
            {
                Stop(ownerKey);
                return;
            }

            if (_db == null || _runner == null) return;

            if (!_regsByOwnerKey.TryGetValue(ownerKey, out var regs) || regs == null)
            {
                regs = new Dictionary<int, IDisposable>(triggerIds.Count);
                _regsByOwnerKey[ownerKey] = regs;
            }

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0 || regs.ContainsKey(triggerId)) continue;
                if (triggerId == 10020000)
                {
                    Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive register requested. ownerKey={ownerKey} triggerId={triggerId} hasDb={_db != null} hasRunner={_runner != null} hasGate={_ownerBoundGates != null} hasEffects={_effects != null}");
                }

                if (!TryRegister(ownerKey, triggerId, out var registration))
                {
                    if (triggerId == 10020000)
                    {
                        Log.Warning($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive register failed. ownerKey={ownerKey} triggerId={triggerId}");
                    }

                    continue;
                }

                regs[triggerId] = registration;
                if (triggerId == 10020000)
                {
                    Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive registered. ownerKey={ownerKey} triggerId={triggerId}");
                }
            }

            RemoveStaleRegistrations(ownerKey, regs, triggerIds);
            if (regs.Count == 0)
            {
                _regsByOwnerKey.Remove(ownerKey);
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
                if (record.Scope != TriggerPlanScope.OwnerBound) continue;
                if (string.IsNullOrEmpty(record.EventName)) continue;
                if (!_eventRegistry.TryGetArgsType(record.EventName, out var argsType) || argsType == null)
                {
                    throw new InvalidOperationException($"Owner-bound trigger event is not registered. triggerId={record.TriggerId} eventName={record.EventName}");
                }

                _argsTypeByTriggerId[record.TriggerId] = argsType.IsClass ? argsType : typeof(object);
            }
        }

        private bool TryRegister(long ownerKey, int triggerId, out IDisposable registration)
        {
            registration = null;
            if (!_byTriggerId.TryGetValue(triggerId, out var record))
            {
                Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId not found in plan db: {triggerId}");
                return false;
            }

            if (record.EventId == 0)
            {
                Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId has empty eventId: {triggerId}");
                return false;
            }

            if (record.Scope != TriggerPlanScope.OwnerBound)
            {
                Log.Warning($"[MobaTriggerPlanSubscriptionService] triggerId is not owner-bound. triggerId={triggerId} scope={record.Scope}");
                return false;
            }

            try
            {
                registration = RegisterTyped(ownerKey, record);
                return registration != null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaTriggerPlanSubscriptionService] register plan failed. triggerId={triggerId}");
                return false;
            }
        }

        private IDisposable RegisterTyped(long ownerKey, in TriggerPlanJsonDatabase.Record record)
        {
            if (!_argsTypeByTriggerId.TryGetValue(record.TriggerId, out var argsType) || argsType == null)
            {
                throw new InvalidOperationException($"Owner-bound trigger missing typed event args mapping. triggerId={record.TriggerId} eventName={record.EventName}");
            }

            if (s_registerTypedAsMethod == null)
            {
                throw new MissingMethodException(nameof(MobaTriggerPlanSubscriptionService), nameof(RegisterTypedAs));
            }

            try
            {
                if (record.TriggerId == 10020000)
                {
                    var typedKey = new EventKey<SkillCastContext>(record.EventId);
                    Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive typed registration. ownerKey={ownerKey} triggerId={record.TriggerId} eventName={record.EventName} eventId={record.EventId} argsType={argsType.Name} busHash={_planEventBus?.GetHashCode() ?? 0} hasTypedBefore={_planEventBus != null && _planEventBus.HasSubscribers(typedKey)}");
                }

                var method = s_registerTypedAsMethod.MakeGenericMethod(argsType);
                var registration = (IDisposable)method.Invoke(this, new object[] { ownerKey, record.EventId, record.Plan });
                if (registration == null)
                {
                    throw new InvalidOperationException($"Owner-bound trigger typed registration returned null. triggerId={record.TriggerId} eventName={record.EventName} eid={record.EventId}");
                }

                return registration;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private IDisposable RegisterTypedAs<TArgs>(long ownerKey, int eventId, TriggerPlan<object> plan)
            where TArgs : class
        {
            var typedPlan = plan.AsArgs<TArgs>();
            var inner = new PlannedTrigger<TArgs, IWorldResolver>(typedPlan);
            ITrigger<TArgs, IWorldResolver> trigger = inner;

            if (_ownerBoundGates != null)
            {
                trigger = new GatedOwnerBoundTrigger<TArgs>(ownerKey, inner, _ownerBoundGates, _effects);
            }

            var key = new EventKey<TArgs>(eventId);
            var registration = _runner.Register(key, trigger, typedPlan.Phase, typedPlan.Priority);
            if (trigger is ITriggerWithId withId && withId.TriggerId == 10020000)
            {
                Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive typed registered on runner. ownerKey={ownerKey} triggerId={withId.TriggerId} eventId={eventId} argsType={typeof(TArgs).Name} busHash={_planEventBus?.GetHashCode() ?? 0} hasTypedAfter={_planEventBus != null && _planEventBus.HasSubscribers(key)}");
            }

            return registration;
        }

        public void Stop(long ownerKey)
        {
            if (ownerKey == 0) return;
            if (!_regsByOwnerKey.TryGetValue(ownerKey, out var regs) || regs == null) return;

            _regsByOwnerKey.Remove(ownerKey);
            DisposeRegistrations(ownerKey, regs);
        }

        private void RemoveStaleRegistrations(long ownerKey, Dictionary<int, IDisposable> regs, IReadOnlyList<int> desiredTriggerIds)
        {
            if (regs == null || regs.Count == 0) return;

            var stale = s_intListPool.Get();
            try
            {
                foreach (var kv in regs)
                {
                    if (!ContainsTriggerId(desiredTriggerIds, kv.Key)) stale.Add(kv.Key);
                }

                for (int i = 0; i < stale.Count; i++)
                {
                    var triggerId = stale[i];
                    var registration = regs[triggerId];
                    regs.Remove(triggerId);
                    DisposeRegistration(ownerKey, registration);
                }
            }
            finally
            {
                s_intListPool.Release(stale);
            }
        }

        private static bool ContainsTriggerId(IReadOnlyList<int> triggerIds, int triggerId)
        {
            if (triggerIds == null || triggerIds.Count == 0) return false;
            for (int i = 0; i < triggerIds.Count; i++)
            {
                if (triggerIds[i] == triggerId) return true;
            }

            return false;
        }

        private static void DisposeRegistrations(long ownerKey, Dictionary<int, IDisposable> regs)
        {
            foreach (var kv in regs)
            {
                DisposeRegistration(ownerKey, kv.Value);
            }
        }

        private static void DisposeRegistration(long ownerKey, IDisposable registration)
        {
            try
            {
                registration?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaTriggerPlanSubscriptionService] dispose reg failed. ownerKey={ownerKey}");
            }
        }

        public void OnDeinit(IWorldResolver services)
        {
            var keys = new List<long>(_regsByOwnerKey.Keys);
            for (int i = 0; i < keys.Count; i++) Stop(keys[i]);
        }

        private sealed class GatedOwnerBoundTrigger<TArgs> : ITrigger<TArgs, IWorldResolver>, ITriggerWithId
            where TArgs : class
        {
            private readonly long _ownerKey;
            private readonly ITrigger<TArgs, IWorldResolver> _inner;
            private readonly MobaOwnerBoundTriggerGateService _gates;
            private readonly MobaEffectExecutionService _effects;
            private readonly int _triggerId;

            public GatedOwnerBoundTrigger(long ownerKey, ITrigger<TArgs, IWorldResolver> inner, MobaOwnerBoundTriggerGateService gates, MobaEffectExecutionService effects)
            {
                _ownerKey = ownerKey;
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _gates = gates;
                _effects = effects;
                _triggerId = inner is ITriggerWithId withId ? withId.TriggerId : 0;
            }

            public ITriggerCue Cue => _inner.Cue;
            public int TriggerId => _triggerId;

            public bool Evaluate(in TArgs args, in ExecCtx<IWorldResolver> ctx)
            {
                if (_gates != null && !_gates.CanExecute(_ownerKey, _triggerId))
                {
                    if (_triggerId == 10020000)
                    {
                        Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive evaluate gate rejected. ownerKey={_ownerKey} triggerId={_triggerId}");
                    }

                    return false;
                }

                var ok = _inner.Evaluate(in args, in ctx);
                if (_triggerId == 10020000)
                {
                    Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive evaluated. ownerKey={_ownerKey} triggerId={_triggerId} ok={ok}");
                }

                return ok;
            }

            public void Execute(in TArgs args, in ExecCtx<IWorldResolver> ctx)
            {
                if (_gates != null && !_gates.CanExecute(_ownerKey, _triggerId))
                {
                    if (_triggerId == 10020000)
                    {
                        Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive gate rejected. ownerKey={_ownerKey} triggerId={_triggerId}");
                    }

                    return;
                }

                if (_effects != null
                    && _gates != null
                    && _gates.TryGetExecutionSource(_ownerKey, _triggerId, out var source))
                {
                    if (_triggerId == 10020000)
                    {
                        Log.Info($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive executing owner-bound actions. ownerKey={_ownerKey} triggerId={_triggerId} sourceActor={source.SourceActorId}");
                    }

                    if (_effects.ExecuteOwnerBoundTriggerActions(_triggerId, args, in ctx, in source, _inner))
                    {
                        _gates.Complete(_ownerKey, _triggerId);
                    }
                    return;
                }

                if (_triggerId == 10020000)
                {
                    Log.Warning($"[MobaTriggerPlanSubscriptionService] XiaoQiao passive missing owner-bound execution source. ownerKey={_ownerKey} triggerId={_triggerId} hasEffects={_effects != null} hasGates={_gates != null}");
                }

                _inner.Execute(in args, in ctx);
                _gates?.Complete(_ownerKey, _triggerId);
            }
        }

        public void Dispose()
        {
            _byTriggerId.Clear();
            _argsTypeByTriggerId.Clear();
            _regsByOwnerKey.Clear();
        }
    }
}
