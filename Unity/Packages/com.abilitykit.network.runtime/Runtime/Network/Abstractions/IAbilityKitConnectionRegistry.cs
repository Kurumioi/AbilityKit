using System;

namespace AbilityKit.Network.Abstractions
{
    public interface IAbilityKitConnectionRegistry : IDisposable
    {
        bool TryGet(AbilityKitConnectionRole role, out IConnection connection);

        IConnection GetRequired(AbilityKitConnectionRole role);

        void Register(AbilityKitConnectionRole role, IConnection connection, bool disposeOnReplace = true);

        IConnection GetOrCreate(AbilityKitConnectionDescriptor descriptor, Func<AbilityKitConnectionDescriptor, IConnection> factory);

        bool Remove(AbilityKitConnectionRole role, bool dispose = true);
    }
}
