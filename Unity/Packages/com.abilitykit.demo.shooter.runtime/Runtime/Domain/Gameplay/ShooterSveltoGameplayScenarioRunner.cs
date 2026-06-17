#nullable enable

using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterSveltoGameplayScenarioRunner
    {
        ShooterSveltoGameplayScenarioResult Run(in ShooterSveltoGameplayScenarioConfig config);
    }

    [WorldService(typeof(ShooterSveltoGameplayScenarioRunner), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterSveltoGameplayScenarioRunner), WorldLifetime.Singleton)]
    public sealed class ShooterSveltoGameplayScenarioRunner : IShooterSveltoGameplayScenarioRunner
    {
        private const float Pi = 3.14159265358979323846f;
        private readonly ISveltoWorldContext _context;
        private uint _nextProjectileId;
        private int _projectilesSpawned;
        private int _projectilesExpired;
        private int _hits;
        private int _defeatedTargets;

        public ShooterSveltoGameplayScenarioRunner(ISveltoWorldContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ShooterSveltoGameplayScenarioResult Run(in ShooterSveltoGameplayScenarioConfig config)
        {
            ResetGroups();
            BuildScenario(in config);

            for (var frame = 0; frame < config.TickCount; frame++)
            {
                TickShooters(in config);
                TickProjectiles(config.TickDeltaTime);
            }

            return BuildResult(in config);
        }

        private void ResetGroups()
        {
            var removed = false;
            removed |= RemoveGroupIfExists(ShooterSveltoGroups.GameplayShooters);
            removed |= RemoveGroupIfExists(ShooterSveltoGroups.GameplayTargets);
            removed |= RemoveGroupIfExists(ShooterSveltoGroups.GameplayProjectiles);

            _nextProjectileId = 1;
            _projectilesSpawned = 0;
            _projectilesExpired = 0;
            _hits = 0;
            _defeatedTargets = 0;

            if (removed)
            {
                _context.SubmitEntities();
            }
        }

        private bool RemoveGroupIfExists(ExclusiveGroupStruct group)
        {
            if (!_context.EntitiesDB.ExistsAndIsNotEmpty(group))
            {
                return false;
            }

            _context.EntityFunctions.RemoveEntitiesFromGroup(group);
            return true;
        }

        private void BuildScenario(in ShooterSveltoGameplayScenarioConfig config)
        {
            for (uint i = 0; i < config.TargetCount; i++)
            {
                var angle = i * 2f * Pi / config.TargetCount;
                var radius = config.ArenaRadius * (0.45f + (i % 3) * 0.18f);
                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoGameplayTargetDescriptor>(i + 1u, ShooterSveltoGroups.GameplayTargets);
                initializer.Init(new ShooterSveltoTransformComponent
                {
                    X = MathF.Cos(angle) * radius,
                    Y = MathF.Sin(angle) * radius,
                    DirectionX = -MathF.Cos(angle),
                    DirectionY = -MathF.Sin(angle)
                });
                initializer.Init(new ShooterSveltoHealthComponent
                {
                    Current = 6,
                    Max = 6,
                    Alive = 1
                });
            }

            _context.SubmitEntities();

            var spreadRadians = config.Loadout.SpreadDegrees * Pi / 180f;
            for (uint i = 0; i < config.ShooterCount; i++)
            {
                var targetId = i % (uint)config.TargetCount + 1u;
                var target = TargetPosition(targetId);
                var angle = i * 2f * Pi / config.ShooterCount;
                var shooterX = MathF.Cos(angle) * config.ArenaRadius * 0.12f;
                var shooterY = MathF.Sin(angle) * config.ArenaRadius * 0.12f;
                var dx = target.X - shooterX;
                var dy = target.Y - shooterY;
                Normalize(ref dx, ref dy);

                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoGameplayShooterDescriptor>(i + 1u, ShooterSveltoGroups.GameplayShooters);
                initializer.Init(new ShooterSveltoTransformComponent
                {
                    X = shooterX,
                    Y = shooterY,
                    DirectionX = dx,
                    DirectionY = dy
                });
                initializer.Init(new ShooterSveltoWeaponComponent
                {
                    LoadoutId = config.Loadout.LoadoutId,
                    ProjectileSpeed = config.Loadout.ProjectileSpeed,
                    ProjectileLifeFrames = config.Loadout.ProjectileLifeFrames,
                    Damage = config.Loadout.Damage,
                    CooldownFrames = config.Loadout.CooldownFrames,
                    ProjectilesPerShot = config.Loadout.ProjectilesPerShot,
                    SpreadRadians = spreadRadians
                });
                initializer.Init(new ShooterSveltoCooldownComponent { RemainingFrames = (int)(i % (uint)config.Loadout.CooldownFrames) });
                initializer.Init(new ShooterSveltoTargetComponent { TargetEntityId = targetId });
            }

            _context.SubmitEntities();
        }

        private void TickShooters(in ShooterSveltoGameplayScenarioConfig config)
        {
            var (transforms, weapons, cooldowns, targets, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoWeaponComponent, ShooterSveltoCooldownComponent, ShooterSveltoTargetComponent>(ShooterSveltoGroups.GameplayShooters);
            for (var i = 0; i < count; i++)
            {
                ref var cooldown = ref cooldowns[i];
                if (cooldown.RemainingFrames > 0)
                {
                    cooldown.RemainingFrames--;
                    continue;
                }

                ref var transform = ref transforms[i];
                ref var weapon = ref weapons[i];
                ref var target = ref targets[i];
                FireBurst(in transform, in weapon, in target, config.TargetCount);
                cooldown.RemainingFrames = weapon.CooldownFrames;
            }
        }

        private void FireBurst(
            in ShooterSveltoTransformComponent shooter,
            in ShooterSveltoWeaponComponent weapon,
            in ShooterSveltoTargetComponent target,
            int targetCount)
        {
            var baseX = shooter.DirectionX;
            var baseY = shooter.DirectionY;
            Normalize(ref baseX, ref baseY);

            for (var i = 0; i < weapon.ProjectilesPerShot; i++)
            {
                var offset = weapon.ProjectilesPerShot == 1
                    ? 0f
                    : ((float)i / (weapon.ProjectilesPerShot - 1) - 0.5f) * weapon.SpreadRadians;
                var dirX = baseX * MathF.Cos(offset) - baseY * MathF.Sin(offset);
                var dirY = baseX * MathF.Sin(offset) + baseY * MathF.Cos(offset);
                var projectileId = _nextProjectileId++;
                var targetId = target.TargetEntityId == 0 ? (projectileId % (uint)targetCount + 1u) : target.TargetEntityId;
                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoGameplayProjectileDescriptor>(projectileId, ShooterSveltoGroups.GameplayProjectiles);
                initializer.Init(new ShooterSveltoTransformComponent
                {
                    X = shooter.X,
                    Y = shooter.Y,
                    DirectionX = dirX,
                    DirectionY = dirY
                });
                initializer.Init(new ShooterSveltoProjectileComponent
                {
                    BulletId = (int)projectileId,
                    OwnerPlayerId = 0,
                    X = shooter.X,
                    Y = shooter.Y,
                    VelocityX = dirX * weapon.ProjectileSpeed,
                    VelocityY = dirY * weapon.ProjectileSpeed,
                    RemainingFrames = weapon.ProjectileLifeFrames
                });
                initializer.Init(new ShooterSveltoProjectileDamageComponent
                {
                    Damage = weapon.Damage,
                    OwnerEntityId = 0,
                    TargetEntityId = targetId
                });
                _projectilesSpawned++;
            }

            _context.SubmitEntities();
        }

        private void TickProjectiles(float deltaTime)
        {
            var (transforms, projectiles, damageComponents, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoProjectileComponent, ShooterSveltoProjectileDamageComponent>(ShooterSveltoGroups.GameplayProjectiles);
            for (var i = count - 1; i >= 0; i--)
            {
                ref var transform = ref transforms[i];
                ref var projectile = ref projectiles[i];
                ref var damage = ref damageComponents[i];
                transform.X += projectile.VelocityX * deltaTime;
                transform.Y += projectile.VelocityY * deltaTime;
                projectile.X = transform.X;
                projectile.Y = transform.Y;
                projectile.RemainingFrames--;

                if (TryApplyHit(in transform, in damage))
                {
                    _context.EntityFunctions.RemoveEntity<ShooterSveltoGameplayProjectileDescriptor>(ids[i], ShooterSveltoGroups.GameplayProjectiles);
                    _hits++;
                    continue;
                }

                if (projectile.RemainingFrames <= 0)
                {
                    _context.EntityFunctions.RemoveEntity<ShooterSveltoGameplayProjectileDescriptor>(ids[i], ShooterSveltoGroups.GameplayProjectiles);
                    _projectilesExpired++;
                }
            }

            _context.SubmitEntities();
        }

        private bool TryApplyHit(in ShooterSveltoTransformComponent projectile, in ShooterSveltoProjectileDamageComponent damage)
        {
            if (damage.TargetEntityId == 0)
            {
                return false;
            }

            if (!_context.EntitiesDB.Exists<ShooterSveltoHealthComponent>(damage.TargetEntityId, ShooterSveltoGroups.GameplayTargets))
            {
                return false;
            }

            ref var targetHealth = ref _context.EntitiesDB.QueryEntity<ShooterSveltoHealthComponent>(damage.TargetEntityId, ShooterSveltoGroups.GameplayTargets);
            if (targetHealth.Alive == 0)
            {
                return false;
            }

            ref var targetTransform = ref _context.EntitiesDB.QueryEntity<ShooterSveltoTransformComponent>(damage.TargetEntityId, ShooterSveltoGroups.GameplayTargets);
            var dx = targetTransform.X - projectile.X;
            var dy = targetTransform.Y - projectile.Y;
            if (dx * dx + dy * dy > 0.64f)
            {
                return false;
            }

            targetHealth.Current = Math.Max(0, targetHealth.Current - damage.Damage);
            if (targetHealth.Current == 0)
            {
                targetHealth.Alive = 0;
                _defeatedTargets++;
            }

            return true;
        }

        private ShooterSveltoGameplayScenarioResult BuildResult(in ShooterSveltoGameplayScenarioConfig config)
        {
            var remainingHp = 0;
            var (healths, targetCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < targetCount; i++)
            {
                remainingHp += healths[i].Current;
            }

            var activeProjectiles = _context.EntitiesDB.Count<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.GameplayProjectiles);
            return new ShooterSveltoGameplayScenarioResult(
                config.Id,
                config.TickCount,
                config.ShooterCount,
                config.TargetCount,
                _projectilesSpawned,
                _projectilesExpired,
                _hits,
                _defeatedTargets,
                activeProjectiles,
                remainingHp,
                ComputeStateHash());
        }

        private ShooterSveltoTransformComponent TargetPosition(uint targetId)
        {
            ref var target = ref _context.EntitiesDB.QueryEntity<ShooterSveltoTransformComponent>(targetId, ShooterSveltoGroups.GameplayTargets);
            return target;
        }

        private uint ComputeStateHash()
        {
            var hash = 2166136261u;
            var (targetTransforms, targetHealths, targetCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < targetCount; i++)
            {
                Mix(ref hash, Quantize(targetTransforms[i].X));
                Mix(ref hash, Quantize(targetTransforms[i].Y));
                Mix(ref hash, targetHealths[i].Current);
                Mix(ref hash, targetHealths[i].Alive);
            }

            var (projectileTransforms, projectileComponents, projectileCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoProjectileComponent>(ShooterSveltoGroups.GameplayProjectiles);
            for (var i = 0; i < projectileCount; i++)
            {
                Mix(ref hash, Quantize(projectileTransforms[i].X));
                Mix(ref hash, Quantize(projectileTransforms[i].Y));
                Mix(ref hash, projectileComponents[i].RemainingFrames);
            }

            return hash;
        }

        private static void Mix(ref uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
            }
        }

        private static int Quantize(float value)
        {
            return (int)MathF.Round(value * 1000f);
        }

        private static void Normalize(ref float x, ref float y)
        {
            var lengthSquared = x * x + y * y;
            if (lengthSquared <= 0.000001f)
            {
                x = 1f;
                y = 0f;
                return;
            }

            var inv = 1f / MathF.Sqrt(lengthSquared);
            x *= inv;
            y *= inv;
        }
    }
}
