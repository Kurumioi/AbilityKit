using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    public static class RuntimeSnapshotConverterRegistry
    {
        private static readonly Dictionary<int, IRuntimeSnapshotConverter> _converters = BuildConverters();

        public static bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            if (!_converters.TryGetValue(snapshot.OpCode, out var converter))
            {
                frameSnapshot = default;
                return false;
            }

            return converter.TryConvert(in snapshot, frameIndex, timestamp, out frameSnapshot);
        }

        private static Dictionary<int, IRuntimeSnapshotConverter> BuildConverters()
        {
            var converters = new Dictionary<int, IRuntimeSnapshotConverter>();
            var converterType = typeof(IRuntimeSnapshotConverter);
            var types = typeof(RuntimeSnapshotConverterRegistry).Assembly
                .GetTypes()
                .Where(t => converterType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<RuntimeSnapshotConverterAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                try
                {
                    var converter = (IRuntimeSnapshotConverter)Activator.CreateInstance(type);
                    converters[attribute.OpCode] = converter;
                    Log.Debug($"[RuntimeSnapshotConverterRegistry] Registered {type.Name} for {attribute.OpCode}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RuntimeSnapshotConverterRegistry] Failed to create {type.Name}: {ex.Message}");
                }
            }

            Log.Info($"[RuntimeSnapshotConverterRegistry] Converters registered: {converters.Count}");
            return converters;
        }
    }
}
