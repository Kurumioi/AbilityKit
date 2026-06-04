using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Gameplay
{
    [WorldService(typeof(MobaGameplayVariableService), WorldLifetime.Scoped)]
    public sealed class MobaGameplayVariableService : IService
    {
        private readonly Dictionary<int, double> _values = new Dictionary<int, double>();

        public bool TryGet(int keyId, out double value)
        {
            if (keyId == 0)
            {
                value = default;
                return false;
            }

            return _values.TryGetValue(keyId, out value);
        }

        public double Get(int keyId, double defaultValue = 0d)
        {
            return TryGet(keyId, out var value) ? value : defaultValue;
        }

        public void Set(int keyId, double value)
        {
            if (keyId == 0)
            {
                return;
            }

            _values[keyId] = value;
        }

        public double Add(int keyId, double delta)
        {
            if (keyId == 0)
            {
                return 0d;
            }

            var next = Get(keyId) + delta;
            _values[keyId] = next;
            return next;
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
