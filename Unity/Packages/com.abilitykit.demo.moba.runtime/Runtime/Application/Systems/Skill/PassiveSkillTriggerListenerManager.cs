using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Systems
{
    internal sealed class PassiveSkillTriggerListenerManager
    {
        private readonly MobaConfigDatabase _configs;
        private readonly MobaTraceRegistry _trace;
        private readonly ITriggerActionRunner _actionRunner;

        internal readonly struct Registration
        {
            public readonly PassiveSkillMO PassiveSkill;
            public readonly PassiveSkillTriggerListenerRuntime Listener;

            public Registration(PassiveSkillMO passiveSkill, PassiveSkillTriggerListenerRuntime listener)
            {
                PassiveSkill = passiveSkill;
                Listener = listener;
            }
        }

        public PassiveSkillTriggerListenerManager(
            MobaConfigDatabase configs,
            MobaTraceRegistry trace,
            ITriggerActionRunner actionRunner)
        {
            _configs = configs;
            _trace = trace;
            _actionRunner = actionRunner;
        }

        public List<PassiveSkillTriggerListenerRuntime> EnsureListenerContainer(global::ActorEntity entity)
        {
            if (entity == null) return null;

            if (!entity.hasPassiveSkillTriggerListeners)
            {
                entity.AddPassiveSkillTriggerListeners(new List<PassiveSkillTriggerListenerRuntime>(4));
            }

            var listeners = entity.passiveSkillTriggerListeners.Active;
            if (listeners == null)
            {
                listeners = new List<PassiveSkillTriggerListenerRuntime>(4);
                entity.passiveSkillTriggerListeners.Active = listeners;
            }

            return listeners;
        }

        public void TryRegister(global::ActorEntity entity, int frame, List<Registration> outRegistrations)
        {
            if (entity == null) return;
            if (_configs == null) return;
            if (!entity.hasActorId || !entity.hasSkillLoadout) return;

            var passiveSkills = entity.skillLoadout.PassiveSkills;
            if (passiveSkills == null) passiveSkills = Array.Empty<PassiveSkillRuntime>();

            var listeners = EnsureListenerContainer(entity);
            if (listeners == null) return;

            var desired = BuildDesiredPassiveSkillIdSet(passiveSkills);
            RemoveObsoleteListeners(listeners, desired, frame);

            for (int i = 0; i < passiveSkills.Length; i++)
            {
                var rt = passiveSkills[i];
                if (rt == null) continue;

                var passiveSkillId = rt.PassiveSkillId;
                if (passiveSkillId <= 0) continue;

                if (!_configs.TryGetPassiveSkill(passiveSkillId, out var mo) || mo == null)
                {
                    Log.Warning($"[MobaPassiveSkillTriggerRegisterSystem] Passive skill config not found. actor={entity.actorId.Value} passiveSkillId={passiveSkillId} frame={frame}");
                    continue;
                }

                if (ContainsListener(listeners, passiveSkillId))
                {
                    continue;
                }

                var l = new PassiveSkillTriggerListenerRuntime
                {
                    PassiveSkillId = passiveSkillId,
                };

                EnsurePassiveSkillContext(entity, listeners, passiveSkillId, l, frame);
                if (l.SourceContextId == 0)
                {
                    Log.Warning($"[MobaPassiveSkillTriggerRegisterSystem] Passive skill listener registered without source context. actor={entity.actorId.Value} passiveSkillId={passiveSkillId} frame={frame} hasTrace={_trace != null}");
                }

                listeners.Add(l);
                outRegistrations?.Add(new Registration(mo, l));
            }
        }

        public void TryUnregister(global::ActorEntity entity, int frame)
        {
            if (entity == null) return;
            if (!entity.hasPassiveSkillTriggerListeners) return;

            var listeners = entity.passiveSkillTriggerListeners.Active;
            if (listeners == null || listeners.Count == 0) return;

            var ownerKeys = new HashSet<long>();

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var l = listeners[i];
                if (l == null) continue;

                if (l.SourceContextId != 0)
                {
                    ownerKeys.Add(l.SourceContextId);
                }

                listeners.RemoveAt(i);
            }

            EndOwnerKeys(ownerKeys, frame);
        }

        private HashSet<int> BuildDesiredPassiveSkillIdSet(PassiveSkillRuntime[] passiveSkills)
        {
            var desired = new HashSet<int>();
            if (passiveSkills == null || passiveSkills.Length == 0) return desired;

            for (int i = 0; i < passiveSkills.Length; i++)
            {
                var rt = passiveSkills[i];
                if (rt == null) continue;
                var passiveSkillId = rt.PassiveSkillId;
                if (passiveSkillId <= 0) continue;

                if (!_configs.TryGetPassiveSkill(passiveSkillId, out var mo) || mo == null) continue;
                desired.Add(passiveSkillId);
            }

            return desired;
        }

        private void RemoveObsoleteListeners(List<PassiveSkillTriggerListenerRuntime> listeners, HashSet<int> desired, int frame)
        {
            if (listeners == null || listeners.Count == 0) return;

            var ownerKeys = new HashSet<long>();

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var l = listeners[i];
                if (l == null) continue;

                if (desired.Contains(l.PassiveSkillId)) continue;

                if (l.SourceContextId != 0)
                {
                    ownerKeys.Add(l.SourceContextId);
                }

                listeners.RemoveAt(i);
            }

            EndOwnerKeys(ownerKeys, frame);
        }

        private void EndOwnerKeys(HashSet<long> ownerKeys, int frame)
        {
            if (ownerKeys == null || ownerKeys.Count == 0) return;

            foreach (var key in ownerKeys)
            {
                try
                {
                    _actionRunner?.CancelByOwnerKey(key);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaPassiveSkillTriggerRegisterSystem] CancelByOwnerKey failed (ownerKey={key})");
                }

                try
                {
                    _trace?.EndContext(key, TraceLifecycleReason.Cancelled);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaPassiveSkillTriggerRegisterSystem] Trace.EndContext failed (ownerKey={key}, frame={frame})");
                }
            }
        }

        private static bool ContainsListener(List<PassiveSkillTriggerListenerRuntime> list, int passiveSkillId)
        {
            if (list == null || list.Count == 0) return false;

            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null) continue;
                if (it.PassiveSkillId == passiveSkillId) return true;
            }

            return false;
        }

        private void EnsurePassiveSkillContext(global::ActorEntity entity, List<PassiveSkillTriggerListenerRuntime> listeners, int passiveSkillId, PassiveSkillTriggerListenerRuntime l, int frame)
        {
            if (entity == null) return;
            if (l == null) return;
            if (l.SourceContextId != 0) return;
            if (_trace == null)
            {
                Log.Warning($"[MobaPassiveSkillTriggerRegisterSystem] Cannot create passive skill source context because trace registry is missing. passiveSkillId={passiveSkillId} frame={frame}");
                return;
            }

            if (!entity.hasActorId) return;

            try
            {
                var actorId = entity.actorId.Value;
                l.SourceContextId = _trace.CreateRootContext(
                    MobaTraceKind.SkillEffect,
                    passiveSkillId,
                    actorId,
                    actorId,
                    TraceEndpoint.Config(MobaRuntimeKindNames.Skill, passiveSkillId),
                    TraceEndpoint.Actor(actorId));
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaPassiveSkillTriggerRegisterSystem] Trace.CreateRootContext failed (actor={entity.actorId.Value}, passiveSkillId={passiveSkillId}, frame={frame})");
                l.SourceContextId = 0;
            }
        }
    }
}
