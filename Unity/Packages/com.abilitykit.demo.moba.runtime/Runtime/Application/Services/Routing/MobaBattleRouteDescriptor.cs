using System;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaBattleRouteKind
    {
        Unknown = 0,
        ClientInput = 1,
        ServerSnapshot = 2,
        ViewCommand = 3,
        Lifecycle = 4,
    }

    public readonly struct MobaBattleRouteDescriptor
    {
        public MobaBattleRouteDescriptor(
            int opCode,
            MobaBattleRouteKind kind,
            Type ownerType,
            Type payloadType = null,
            Type handlerType = null,
            string name = null)
        {
            OpCode = opCode;
            Kind = kind;
            OwnerType = ownerType;
            PayloadType = payloadType;
            HandlerType = handlerType;
            Name = string.IsNullOrEmpty(name) ? ownerType?.Name : name;
        }

        public int OpCode { get; }

        public MobaBattleRouteKind Kind { get; }

        public Type OwnerType { get; }

        public Type PayloadType { get; }

        public Type HandlerType { get; }

        public string Name { get; }
    }
}
