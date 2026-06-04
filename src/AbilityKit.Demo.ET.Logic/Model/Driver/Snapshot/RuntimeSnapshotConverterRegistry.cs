using System.Linq;
using System.Reflection;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Snapshot;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    public static class RuntimeSnapshotConverterRegistry
    {
        private static readonly MobaRuntimeSnapshotMapperRegistry<FrameSnapshotData> _converters = BuildConverters();

        public static bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var context = new MobaRuntimeSnapshotContext(frameIndex, timestamp);
            return _converters.TryMap(in snapshot, in context, out frameSnapshot);
        }

        private static MobaRuntimeSnapshotMapperRegistry<FrameSnapshotData> BuildConverters()
        {
            var converterType = typeof(IRuntimeSnapshotConverter);
            var types = typeof(RuntimeSnapshotConverterRegistry).Assembly
                .GetTypes()
                .Where(t => converterType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<RuntimeSnapshotConverterAttribute>() != null);

            var registry = new MobaRuntimeSnapshotMapperRegistry<FrameSnapshotData>();
            foreach (var type in types)
            {
                try
                {
                    if (System.Activator.CreateInstance(type) is IRuntimeSnapshotConverter converter)
                    {
                        registry.TryRegister(new RuntimeSnapshotMapperAdapter(converter));
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[RuntimeSnapshotConverterRegistry] Failed to create snapshot mapper {type.FullName}: {ex.Message}");
                }
            }

            Log.Info($"[RuntimeSnapshotConverterRegistry] Converters registered: {registry.Count}");
            return registry;
        }

        private sealed class RuntimeSnapshotMapperAdapter : IMobaRuntimeSnapshotMapper<FrameSnapshotData>
        {
            private readonly IRuntimeSnapshotConverter _converter;

            public RuntimeSnapshotMapperAdapter(IRuntimeSnapshotConverter converter)
            {
                _converter = converter;
            }

            public int OpCode => _converter.OpCode;

            public bool TryMap(in WorldStateSnapshot snapshot, in MobaRuntimeSnapshotContext context, out FrameSnapshotData output)
            {
                return _converter.TryConvert(in snapshot, context.FrameIndex, context.Timestamp, out output);
            }
        }
    }
}
