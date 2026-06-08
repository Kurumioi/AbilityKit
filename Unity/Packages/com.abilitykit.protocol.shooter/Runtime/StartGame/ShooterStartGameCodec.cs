using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Shooter
{
    [MemoryPackable]
    public partial struct ShooterStartPlayer
    {
        [MemoryPackOrder(0)] public int PlayerId;
        [MemoryPackOrder(1)] public string Name;
        [MemoryPackOrder(2)] public float SpawnX;
        [MemoryPackOrder(3)] public float SpawnY;

        public ShooterStartPlayer(int playerId, string name, float spawnX, float spawnY)
        {
            PlayerId = playerId;
            Name = name ?? string.Empty;
            SpawnX = spawnX;
            SpawnY = spawnY;
        }
    }

    [MemoryPackable]
    public partial struct ShooterStartGamePayload
    {
        [MemoryPackOrder(0)] public string MatchId;
        [MemoryPackOrder(1)] public int TickRate;
        [MemoryPackOrder(2)] public int RandomSeed;
        [MemoryPackOrder(3)] public ShooterStartPlayer[] Players;

        [MemoryPackConstructor]
        public ShooterStartGamePayload(string matchId, int tickRate, int randomSeed, ShooterStartPlayer[] players)
        {
            MatchId = matchId ?? string.Empty;
            TickRate = tickRate;
            RandomSeed = randomSeed;
            Players = players;
        }
    }

    public static class ShooterStartGameCodec
    {
        public static byte[] Serialize(in ShooterStartGamePayload payload)
        {
            return WireSerializer.Serialize(in payload);
        }

        public static ShooterStartGamePayload Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new ShooterStartGamePayload(string.Empty, 30, 0, Array.Empty<ShooterStartPlayer>());
            }

            var value = WireSerializer.Deserialize<ShooterStartGamePayload>(payload);
            return new ShooterStartGamePayload(
                value.MatchId,
                value.TickRate,
                value.RandomSeed,
                value.Players ?? Array.Empty<ShooterStartPlayer>());
        }
    }
}
