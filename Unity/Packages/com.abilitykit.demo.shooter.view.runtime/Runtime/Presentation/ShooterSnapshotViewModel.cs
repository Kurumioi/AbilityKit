using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotViewModel
    {
        private readonly Dictionary<int, ShooterPlayerViewState> _players = new Dictionary<int, ShooterPlayerViewState>();
        private readonly Dictionary<int, ShooterBulletViewState> _bullets = new Dictionary<int, ShooterBulletViewState>();
        private readonly List<ShooterEventSnapshot> _events = new List<ShooterEventSnapshot>();

        public int Frame { get; private set; }

        public IReadOnlyDictionary<int, ShooterPlayerViewState> Players => _players;

        public IReadOnlyDictionary<int, ShooterBulletViewState> Bullets => _bullets;

        public IReadOnlyList<ShooterEventSnapshot> Events => _events;

        public void Apply(in ShooterStateSnapshotPayload snapshot)
        {
            Frame = snapshot.Frame;
            _players.Clear();
            _bullets.Clear();
            _events.Clear();

            if (snapshot.Players != null)
            {
                for (int i = 0; i < snapshot.Players.Length; i++)
                {
                    var player = snapshot.Players[i];
                    _players[player.PlayerId] = new ShooterPlayerViewState(
                        player.PlayerId,
                        player.X,
                        player.Y,
                        player.AimX,
                        player.AimY,
                        player.Hp,
                        player.Score,
                        player.Alive);
                }
            }

            if (snapshot.Bullets != null)
            {
                for (int i = 0; i < snapshot.Bullets.Length; i++)
                {
                    var bullet = snapshot.Bullets[i];
                    _bullets[bullet.BulletId] = new ShooterBulletViewState(
                        bullet.BulletId,
                        bullet.OwnerPlayerId,
                        bullet.X,
                        bullet.Y,
                        bullet.VelocityX,
                        bullet.VelocityY,
                        bullet.RemainingFrames);
                }
            }

            if (snapshot.Events != null)
            {
                _events.AddRange(snapshot.Events);
            }
        }

        public void Apply(in ShooterGatewaySnapshot snapshot)
        {
            if (snapshot.PackedSnapshot.HasValue)
            {
                var packed = snapshot.PackedSnapshot.Value;
                Apply(in packed);
                return;
            }

            Frame = snapshot.Frame;
            _players.Clear();
            _bullets.Clear();
            _events.Clear();

            var actors = snapshot.Actors;
            if (actors == null)
            {
                return;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                _players[actor.ActorId] = new ShooterPlayerViewState(
                    actor.ActorId,
                    actor.X,
                    actor.Y,
                    0f,
                    1f,
                    ToDisplayHp(actor.Hp),
                    score: 0,
                    alive: actor.Hp > 0f);
            }
        }

        public void Apply(in ShooterPackedSnapshotPayload snapshot)
        {
            Frame = snapshot.Frame;
            _players.Clear();
            _bullets.Clear();
            _events.Clear();

            var chunks = snapshot.Chunks;
            if (chunks == null)
            {
                return;
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                if (chunk.EntityKind == ShooterPackedEntityKinds.Player)
                {
                    ApplyPackedPlayers(in chunk);
                }
                else if (chunk.EntityKind == ShooterPackedEntityKinds.Projectile)
                {
                    ApplyPackedProjectiles(in chunk);
                }
            }
        }

        private void ApplyPackedPlayers(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var playerId = GetInt(chunk.EntityIds, i);
                if (playerId <= 0) continue;

                var flags = GetByte(chunk.Flags, i);
                _players[playerId] = new ShooterPlayerViewState(
                    playerId,
                    GetFloat(chunk.PosX, i),
                    GetFloat(chunk.PosY, i),
                    GetFloat(chunk.FacingX, i, 1f),
                    GetFloat(chunk.FacingY, i),
                    GetShort(chunk.Hp, i),
                    GetInt(chunk.Aux, i),
                    (flags & ShooterPackedEntityFlags.Alive) != 0);
            }
        }

        private void ApplyPackedProjectiles(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var bulletId = GetInt(chunk.EntityIds, i);
                if (bulletId <= 0) continue;

                _bullets[bulletId] = new ShooterBulletViewState(
                    bulletId,
                    GetInt(chunk.OwnerIds, i),
                    GetFloat(chunk.PosX, i),
                    GetFloat(chunk.PosY, i),
                    GetFloat(chunk.VelX, i),
                    GetFloat(chunk.VelY, i),
                    GetInt(chunk.Aux, i, GetShort(chunk.Hp, i)));
            }
        }

        private static int ToDisplayHp(float hp)
        {
            if (hp <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(hp);
        }

        private static int GetInt(int[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static float GetFloat(float[] values, int index, float fallback = 0f)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int GetShort(short[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static byte GetByte(byte[] values, int index, byte fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        public void Clear()
        {
            Frame = 0;
            _players.Clear();
            _bullets.Clear();
            _events.Clear();
        }
    }

    public readonly struct ShooterPlayerViewState
    {
        public ShooterPlayerViewState(int playerId, float x, float y, float aimX, float aimY, int hp, int score, bool alive)
        {
            PlayerId = playerId;
            X = x;
            Y = y;
            AimX = aimX;
            AimY = aimY;
            Hp = hp;
            Score = score;
            Alive = alive;
        }

        public int PlayerId { get; }

        public float X { get; }

        public float Y { get; }

        public float AimX { get; }

        public float AimY { get; }

        public int Hp { get; }

        public int Score { get; }

        public bool Alive { get; }
    }

    public readonly struct ShooterBulletViewState
    {
        public ShooterBulletViewState(int bulletId, int ownerPlayerId, float x, float y, float velocityX, float velocityY, int remainingFrames)
        {
            BulletId = bulletId;
            OwnerPlayerId = ownerPlayerId;
            X = x;
            Y = y;
            VelocityX = velocityX;
            VelocityY = velocityY;
            RemainingFrames = remainingFrames;
        }

        public int BulletId { get; }

        public int OwnerPlayerId { get; }

        public float X { get; }

        public float Y { get; }

        public float VelocityX { get; }

        public float VelocityY { get; }

        public int RemainingFrames { get; }
    }
}
