using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Systems
{
    [WorldService(typeof(MobaEventSubscriptionRegistry))]
    public sealed class MobaEventSubscriptionRegistry : IService
    {
        private readonly Dictionary<string, Type> _exact = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly List<PrefixEntry> _prefixes = new List<PrefixEntry>(8);

        public MobaEventSubscriptionRegistry()
        {
            DiscoverAndRegister();
        }

        private readonly struct PrefixEntry
        {
            public readonly string Prefix;
            public readonly Type ArgsType;

            public PrefixEntry(string prefix, Type argsType)
            {
                Prefix = prefix;
                ArgsType = argsType;
            }
        }

        public void RegisterExact<TArgs>(string eventId)
        {
            RegisterExact(eventId, typeof(TArgs));
        }

        public void RegisterPrefix<TArgs>(string prefix)
        {
            RegisterPrefix(prefix, typeof(TArgs));
        }

        public void RegisterExact(string eventId, Type argsType)
        {
            if (string.IsNullOrEmpty(eventId)) throw new ArgumentException(nameof(eventId));
            if (argsType == null) throw new ArgumentNullException(nameof(argsType));
            _exact[eventId] = argsType;
        }

        public void RegisterPrefix(string prefix, Type argsType)
        {
            if (string.IsNullOrEmpty(prefix)) throw new ArgumentException(nameof(prefix));
            if (argsType == null) throw new ArgumentNullException(nameof(argsType));

            for (int i = 0; i < _prefixes.Count; i++)
            {
                var existing = _prefixes[i];
                if (string.Equals(existing.Prefix, prefix, StringComparison.Ordinal))
                {
                    _prefixes[i] = new PrefixEntry(prefix, argsType);
                    return;
                }
            }

            _prefixes.Add(new PrefixEntry(prefix, argsType));
        }

        public void DiscoverAndRegister(Assembly assembly = null)
        {
            if (assembly != null)
            {
                RegisterAssemblyMappings(assembly);
                return;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                RegisterAssemblyMappings(assemblies[i]);
            }
        }
        private void RegisterAssemblyMappings(Assembly assembly)
        {
            if (assembly == null) return;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null) return;

            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null) continue;

                var attrs = type.GetCustomAttributes<MobaTriggerEventAttribute>(inherit: false);
                foreach (var attr in attrs)
                {
                    if (attr == null || attr.ArgsType == null || string.IsNullOrEmpty(attr.EventIdOrPrefix))
                    {
                        continue;
                    }

                    if (attr.IsPrefix)
                    {
                        RegisterPrefix(attr.EventIdOrPrefix, attr.ArgsType);
                    }
                    else
                    {
                        RegisterExact(attr.EventIdOrPrefix, attr.ArgsType);
                    }
                }
            }
        }

        public bool TryGetArgsType(string eventId, out Type argsType)
        {
            argsType = null;
            if (string.IsNullOrEmpty(eventId)) return false;

            if (_exact.TryGetValue(eventId, out argsType) && argsType != null)
            {
                return true;
            }

            for (int i = 0; i < _prefixes.Count; i++)
            {
                var p = _prefixes[i];
                if (eventId.StartsWith(p.Prefix, StringComparison.Ordinal))
                {
                    argsType = p.ArgsType;
                    return argsType != null;
                }
            }

            return false;
        }

        public bool TrySubscribe<TArgs>(IEventBus eventBus, string eventId, Action<TArgs> handler, out IDisposable sub)
        {
            sub = null;
            if (eventBus == null) return false;
            if (string.IsNullOrEmpty(eventId)) return false;

            if (!TryGetArgsType(eventId, out var mappedType) || mappedType == null)
            {
                Log.Warning($"[MobaEventSubscriptionRegistry] Unsupported eventId (no mapping): {eventId}");
                return false;
            }

            if (mappedType != typeof(TArgs))
            {
                Log.Warning($"[MobaEventSubscriptionRegistry] eventId payload type mismatch: eventId={eventId}, mapped={mappedType.Name}, requested={typeof(TArgs).Name}");
                return false;
            }

            var eid = AbilityKit.Demo.Moba.Services.TriggeringIdUtil.GetEventEid(eventId);
            var key = new EventKey<TArgs>(eid);
            sub = eventBus.Subscribe(key, handler);
            return true;
        }

        public void Dispose()
        {
        }
    }
}
