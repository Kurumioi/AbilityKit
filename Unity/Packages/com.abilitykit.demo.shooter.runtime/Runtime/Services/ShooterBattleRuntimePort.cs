using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private const float PlayerSpeed = 5f;
        private const float BulletSpeed = 12f;
        private const int BulletLifeFrames = 60;
        private const float HitRadius = 0.45f;
        private const int HitDamage = 1;

        private readonly Dictionary<int, ShooterPlayerState> _players = new Dictionary<int, ShooterPlayerState>();
        private readonly Dictionary<int, ShooterPlayerCommand> _latestCommands = new Dictionary<int, ShooterPlayerCommand>();
        private readonly List<ShooterBulletState> _bullets = new List<ShooterBulletState>(32);
        private readonly List<ShooterEventSnapshot> _events = new List<ShooterEventSnapshot>(16);
        private int _nextBulletId = 1;

        public bool IsStarted { get; private set; }

        public int CurrentFrame { get; private set; }

        public ShooterStartGamePayload StartSpec { get; private set; }

        public bool StartGame(in ShooterStartGamePayload spec)
        {
            _players.Clear();
            _latestCommands.Clear();
            _bullets.Clear();
            _events.Clear();
            _nextBulletId = 1;
            CurrentFrame = 0;
            StartSpec = spec;

            var players = spec.Players ?? Array.Empty<ShooterStartPlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player.PlayerId <= 0 || _players.ContainsKey(player.PlayerId)) continue;

                _players[player.PlayerId] = new ShooterPlayerState
                {
                    PlayerId = player.PlayerId,
                    X = player.SpawnX,
                    Y = player.SpawnY,
                    AimX = 1f,
                    AimY = 0f,
                    Hp = ShooterGameplay.DefaultPlayerHp,
                    Score = 0,
                    Alive = true
                };
            }

            IsStarted = _players.Count > 0;
            return IsStarted;
        }

        public int SubmitInput(int frame, ShooterPlayerCommand[] commands)
        {
            if (!IsStarted || commands == null || commands.Length == 0)
            {
                return 0;
            }

            var accepted = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (!_players.ContainsKey(command.PlayerId)) continue;

                _latestCommands[command.PlayerId] = command;
                accepted++;
            }

            return accepted;
        }

        public bool Tick(float deltaTime)
        {
            if (!IsStarted)
            {
                return false;
            }

            CurrentFrame++;
            _events.Clear();

            TickPlayers(deltaTime);
            TickBullets(deltaTime);
            return true;
        }

        public ShooterStateSnapshotPayload GetSnapshot()
        {
            var players = new ShooterPlayerSnapshot[_players.Count];
            var index = 0;
            foreach (var kv in _players)
            {
                var p = kv.Value;
                players[index++] = new ShooterPlayerSnapshot(p.PlayerId, p.X, p.Y, p.AimX, p.AimY, p.Hp, p.Score, p.Alive);
            }

            var bullets = new ShooterBulletSnapshot[_bullets.Count];
            for (int i = 0; i < _bullets.Count; i++)
            {
                var b = _bullets[i];
                bullets[i] = new ShooterBulletSnapshot(b.BulletId, b.OwnerPlayerId, b.X, b.Y, b.VelocityX, b.VelocityY, b.RemainingFrames);
            }

            return new ShooterStateSnapshotPayload(CurrentFrame, players, bullets, _events.ToArray());
        }

        private void TickPlayers(float deltaTime)
        {
            foreach (var kv in _players)
            {
                var player = kv.Value;
                if (!player.Alive) continue;

                _latestCommands.TryGetValue(player.PlayerId, out var command);
                var moveLength = Normalize(ref command.MoveX, ref command.MoveY);
                if (moveLength > 0f)
                {
                    player.X += command.MoveX * PlayerSpeed * deltaTime;
                    player.Y += command.MoveY * PlayerSpeed * deltaTime;
                }

                var aimLength = Normalize(ref command.AimX, ref command.AimY);
                if (aimLength > 0f)
                {
                    player.AimX = command.AimX;
                    player.AimY = command.AimY;
                }

                if (command.Fire)
                {
                    SpawnBullet(player);
                    command.Fire = false;
                    _latestCommands[player.PlayerId] = command;
                }
            }
        }

        private void TickBullets(float deltaTime)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _bullets[i];
                bullet.X += bullet.VelocityX * deltaTime;
                bullet.Y += bullet.VelocityY * deltaTime;
                bullet.RemainingFrames--;

                if (TryHitPlayer(bullet, out var target))
                {
                    target.Hp = Math.Max(0, target.Hp - HitDamage);
                    if (target.Hp == 0)
                    {
                        target.Alive = false;
                    }

                    if (_players.TryGetValue(bullet.OwnerPlayerId, out var owner))
                    {
                        owner.Score++;
                    }

                    _events.Add(new ShooterEventSnapshot(1, bullet.OwnerPlayerId, target.PlayerId, bullet.BulletId, target.X, target.Y, HitDamage));
                    _bullets.RemoveAt(i);
                    continue;
                }

                if (bullet.RemainingFrames <= 0)
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                _bullets[i] = bullet;
            }
        }

        private void SpawnBullet(ShooterPlayerState player)
        {
            var bullet = new ShooterBulletState
            {
                BulletId = _nextBulletId++,
                OwnerPlayerId = player.PlayerId,
                X = player.X + player.AimX * 0.5f,
                Y = player.Y + player.AimY * 0.5f,
                VelocityX = player.AimX * BulletSpeed,
                VelocityY = player.AimY * BulletSpeed,
                RemainingFrames = BulletLifeFrames
            };
            _bullets.Add(bullet);
            _events.Add(new ShooterEventSnapshot(2, player.PlayerId, 0, bullet.BulletId, bullet.X, bullet.Y, 0));
        }

        private bool TryHitPlayer(ShooterBulletState bullet, out ShooterPlayerState? target)
        {
            foreach (var kv in _players)
            {
                var player = kv.Value;
                if (!player.Alive || player.PlayerId == bullet.OwnerPlayerId) continue;

                var dx = player.X - bullet.X;
                var dy = player.Y - bullet.Y;
                if (dx * dx + dy * dy <= HitRadius * HitRadius)
                {
                    target = player;
                    return true;
                }
            }

            target = null;
            return false;
        }

        private static float Normalize(ref float x, ref float y)
        {
            var length = (float)Math.Sqrt(x * x + y * y);
            if (length <= 0.0001f)
            {
                x = 0f;
                y = 0f;
                return 0f;
            }

            x /= length;
            y /= length;
            return length;
        }

        private sealed class ShooterPlayerState
        {
            public int PlayerId;
            public float X;
            public float Y;
            public float AimX;
            public float AimY;
            public int Hp;
            public int Score;
            public bool Alive;
        }

        private struct ShooterBulletState
        {
            public int BulletId;
            public int OwnerPlayerId;
            public float X;
            public float Y;
            public float VelocityX;
            public float VelocityY;
            public int RemainingFrames;
        }
    }
}
