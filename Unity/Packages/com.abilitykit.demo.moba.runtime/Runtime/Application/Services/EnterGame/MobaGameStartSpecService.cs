using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaGameStartSpecService))]
    public sealed class MobaGameStartSpecService : IService
    {
        private MobaGameStartSpec _spec;

        public bool HasSpec { get; private set; }

        public void Set(in MobaGameStartSpec spec)
        {
            _spec = spec;
            HasSpec = true;
        }

        public bool TryGet(out MobaGameStartSpec spec)
        {
            spec = _spec;
            return HasSpec;
        }

        public void Clear()
        {
            _spec = default;
            HasSpec = false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

