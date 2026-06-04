using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Ability.Host.Extensions.Moba.StartGame
{
    public interface IMobaGameStartOrchestrator : IService
    {
        bool TryStartGame(IWorld world);
    }

    public sealed class MobaGameStartOrchestrator : IMobaGameStartOrchestrator
    {
        private readonly IMobaRoomOrchestrator _room;

        private bool _started;

        public MobaGameStartOrchestrator(IMobaRoomOrchestrator room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
        }

        public bool TryStartGame(IWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            if (_started)
            {
                Log.Info("[MobaGameStartOrchestrator] TryStartGame: already started");
                return false;
            }

            if (!CanStartGame(_room))
            {
                Log.Info("[MobaGameStartOrchestrator] TryStartGame: CanStartGame=false");
                return false;
            }

            if (!TryBuildGameStartSpec(_room, out var spec))
            {
                Log.Info("[MobaGameStartOrchestrator] TryStartGame: GameStartSpec not found/built");
                return false;
            }

            if (world.Services?.TryResolve<IMobaBattleRuntimePort>(out var runtime) != true || runtime == null)
            {
                Log.Error("[MobaGameStartOrchestrator] TryStartGame: IMobaBattleRuntimePort not found");
                return false;
            }

            var result = runtime.TryStartGame(in spec);
            if (!result.Succeeded)
            {
                Log.Warning($"[MobaGameStartOrchestrator] TryStartGame rejected. {result}");
                return false;
            }

            _started = true;
            return true;
        }

        private static bool CanStartGame(IMobaRoomOrchestrator room)
        {
            if (room == null) return false;
            return room.State != null && room.State.CanStart();
        }

        private static bool TryBuildGameStartSpec(IMobaRoomOrchestrator room, out MobaGameStartSpec spec)
        {
            spec = default;
            if (room?.State == null) return false;

            // Pick any joined player as localPlayerId for spec building.
            foreach (var kv in room.State.Players)
            {
                var localPlayerId = new PlayerId(kv.Key);
                return room.TryBuildGameStartSpec(localPlayerId, out spec);
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}

