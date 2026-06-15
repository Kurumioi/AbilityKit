using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Core.Utilities;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class RemoteDrivenHandles
        {
            internal RemoteDrivenWorldRuntime WorldRuntime;
            internal IWorldManager Worlds;
            internal HostRuntime Runtime;
            internal IWorld World;

            internal RemoteDrivenInputRuntime InputRuntime;
            internal IRemoteFrameSource<PlayerInputCommand[]> InputSource;
            internal IConsumableRemoteFrameSource<PlayerInputCommand[]> Consumable;
            internal IRemoteFrameSink<PlayerInputCommand[]> Sink;

            internal void BindWorldRuntime(RemoteDrivenWorldRuntime runtime)
            {
                WorldRuntime = runtime;
                Worlds = runtime != null ? runtime.Worlds : null;
                Runtime = runtime != null ? runtime.Runtime : null;
                World = runtime != null ? runtime.World : null;
            }

            internal void BindInputRuntime(RemoteDrivenInputRuntime runtime)
            {
                InputRuntime = runtime;
                InputSource = runtime != null ? runtime.Source : null;
                Consumable = runtime != null ? runtime.Consumable : null;
                Sink = runtime != null ? runtime.Sink : null;
            }

            internal void DestroyWorld(WorldId fallbackWorldId)
            {
                if (WorldRuntime != null)
                {
                    WorldRuntime.DestroyWorld();
                    return;
                }

                Runtime?.DestroyWorld(fallbackWorldId);
            }

            internal void ClearWorldRuntime()
            {
                WorldRuntime = null;
                Worlds = null;
                Runtime = null;
                World = null;
            }

            internal void DisposeInput()
            {
                if (InputRuntime != null)
                {
                    DisposeUtils.TryDispose(ref InputRuntime, ex => Log.Exception(ex));
                    InputSource = null;
                }
                else
                {
                    IDisposable inputSourceDisposable = InputSource;
                    InputSource = null;
                    DisposeUtils.TryDispose(ref inputSourceDisposable, ex => Log.Exception(ex));
                }

                Consumable = null;
                Sink = null;
            }

            public void Reset()
            {
                ClearWorldRuntime();
                DisposeInput();
            }
        }
    }
}
