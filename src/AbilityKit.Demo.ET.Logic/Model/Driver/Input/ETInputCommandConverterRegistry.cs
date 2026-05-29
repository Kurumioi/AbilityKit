using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    public static class ETInputCommandConverterRegistry
    {
        private static readonly Dictionary<Type, IETInputCommandConverter> _converters = BuildConverters();

        public static bool TryConvert(object command, FrameIndex frameIndex, out PlayerInputCommand playerCommand)
        {
            playerCommand = default;
            if (command == null)
            {
                return false;
            }

            var commandType = command.GetType();
            if (!_converters.TryGetValue(commandType, out var converter))
            {
                Log.Debug($"[ETInputCommandConverterRegistry] No converter for {commandType.Name}");
                return false;
            }

            return converter.TryConvert(command, frameIndex, out playerCommand);
        }

        private static Dictionary<Type, IETInputCommandConverter> BuildConverters()
        {
            var converters = new Dictionary<Type, IETInputCommandConverter>();
            var converterType = typeof(IETInputCommandConverter);
            var types = typeof(ETInputCommandConverterRegistry).Assembly
                .GetTypes()
                .Where(t => converterType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<ETInputCommandConverterAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                try
                {
                    var converter = (IETInputCommandConverter)Activator.CreateInstance(type);
                    converters[attribute.CommandType] = converter;
                    Log.Debug($"[ETInputCommandConverterRegistry] Registered {type.Name} for {attribute.CommandType.Name}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[ETInputCommandConverterRegistry] Failed to create {type.Name}: {ex.Message}");
                }
            }

            Log.Info($"[ETInputCommandConverterRegistry] Converters registered: {converters.Count}");
            return converters;
        }
    }
}
