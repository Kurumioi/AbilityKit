using System;
using AbilityKit.Protocol.Shooter;
using ShooterBulletState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsProjectileEntity;
using ShooterPlayerState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsPlayerEntity;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterBattleSimulation
    {
        void Tick(float deltaTime);
    }

    public sealed class ShooterBattleSimulation : IShooterBattleSimulation
    {
        private readonly ShooterBattleState _state;

        public ShooterBattleSimulation(ShooterBattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Tick(float deltaTime)
        {
            TickPlayers(deltaTime);
            TickBullets(deltaTime);
        }

        private void TickPlayers(float deltaTime)
        {
            foreach (var kv in _state.Players)
            {
                var player = kv.Value;
                if (!player.Alive) continue;

                _state.LatestCommands.TryGetValue(player.PlayerId, out var command);
                var moveLength = ShooterBattleMath.Normalize(ref command.MoveX, ref command.MoveY);
                if (moveLength > 0f)
                {
                    player.X += command.MoveX * ShooterBattleTuning.PlayerSpeed * deltaTime;
                    player.Y += command.MoveY * ShooterBattleTuning.PlayerSpeed * deltaTime;
                }

                var aimLength = ShooterBattleMath.Normalize(ref command.AimX, ref command.AimY);
                if (aimLength > 0f)
                {
                    player.AimX = command.AimX;
                    player.AimY = command.AimY;
                }

                if (command.Fire)
                {
                    SpawnBullet(player);
                    command.Fire = false;
                    _state.LatestCommands[player.PlayerId] = command;
                }
            }
        }

        private void TickBullets(float deltaTime)
        {
            for (int i = _state.Bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _state.Bullets[i];
                bullet.X += bullet.VelocityX * deltaTime;
                bullet.Y += bullet.VelocityY * deltaTime;
                bullet.RemainingFrames--;

                if (TryHitPlayer(bullet, out var target) && target != null)
                {
                    target.Hp = Math.Max(0, target.Hp - ShooterBattleTuning.HitDamage);
                    if (target.Hp == 0)
                    {
                        target.Alive = false;
                    }

                    if (_state.Players.TryGetValue(bullet.OwnerPlayerId, out var owner))
                    {
                        owner.Score++;
                    }

                    _state.Events.Add(new ShooterEventSnapshot(1, bullet.OwnerPlayerId, target.PlayerId, bullet.BulletId, target.X, target.Y, ShooterBattleTuning.HitDamage));
                    _state.Bullets.RemoveAt(i);
                    continue;
                }

                if (bullet.RemainingFrames <= 0)
                {
                    _state.Bullets.RemoveAt(i);
                    continue;
                }

                _state.Bullets[i] = bullet;
            }
        }

        private void SpawnBullet(ShooterPlayerState player)
        {
            var bullet = new ShooterBulletState
            {
                BulletId = _state.AllocateBulletId(),
                OwnerPlayerId = player.PlayerId,
                X = player.X + player.AimX * 0.5f,
                Y = player.Y + player.AimY * 0.5f,
                VelocityX = player.AimX * ShooterBattleTuning.BulletSpeed,
                VelocityY = player.AimY * ShooterBattleTuning.BulletSpeed,
                RemainingFrames = ShooterBattleTuning.BulletLifeFrames
            };
            _state.Bullets.Add(bullet);
            _state.Events.Add(new ShooterEventSnapshot(2, player.PlayerId, 0, bullet.BulletId, bullet.X, bullet.Y, 0));
        }

        private bool TryHitPlayer(ShooterBulletState bullet, out ShooterPlayerState? target)
        {
            foreach (var kv in _state.Players)
            {
                var player = kv.Value;
                if (!player.Alive || player.PlayerId == bullet.OwnerPlayerId) continue;

                var dx = player.X - bullet.X;
                var dy = player.Y - bullet.Y;
                if (dx * dx + dy * dy <= ShooterBattleTuning.HitRadius * ShooterBattleTuning.HitRadius)
                {
                    target = player;
                    return true;
                }
            }

            target = null;
            return false;
        }
    }
}
