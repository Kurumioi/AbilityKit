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
        private uint _nextTargetId;
        private uint _nextProjectileId;
        private int _projectilesSpawned;
        private int _projectilesExpired;
        private int _hits;
        private int _defeatedTargets;
        private int _enemyHits;
        private int[] _waveSpawned = Array.Empty<int>();

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
                TickWaveSpawns(in config, frame);
                TickShooters(in config);
                TickEnemies(in config, frame);
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

            _nextTargetId = 1;
            _nextProjectileId = 1;
            _projectilesSpawned = 0;
            _projectilesExpired = 0;
            _hits = 0;
            _defeatedTargets = 0;
            _enemyHits = 0;
            _waveSpawned = Array.Empty<int>();

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
            _waveSpawned = new int[config.BattleFlow.Waves.Length];
            _nextTargetId = 1;

            var spreadRadians = config.Loadout.SpreadDegrees * Pi / 180f;
            for (uint i = 0; i < config.ShooterCount; i++)
            {
                var angle = i * 2f * Pi / config.ShooterCount;
                var shooterX = MathF.Cos(angle) * config.ArenaRadius * 0.12f;
                var shooterY = MathF.Sin(angle) * config.ArenaRadius * 0.12f;
                var dx = MathF.Cos(angle);
                var dy = MathF.Sin(angle);
                Normalize(ref dx, ref dy);

                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoGameplayShooterDescriptor>(i + 1u, ShooterSveltoGroups.GameplayShooters);
                initializer.Init(new ShooterSveltoTransformComponent
                {
                    X = shooterX,
                    Y = shooterY,
                    DirectionX = dx,
                    DirectionY = dy
                });
                initializer.Init(new ShooterSveltoHealthComponent
                {
                    Current = 12,
                    Max = 12,
                    Alive = 1
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
                initializer.Init(new ShooterSveltoTargetComponent { TargetEntityId = 0u });
            }

            _context.SubmitEntities();
        }

        private void TickWaveSpawns(in ShooterSveltoGameplayScenarioConfig config, int frame)
        {
            var activeEnemies = CountAliveEnemies();
            var waves = config.BattleFlow.Waves;
            for (var i = 0; i < waves.Length; i++)
            {
                var wave = waves[i];
                if (frame < wave.StartFrame || _waveSpawned[i] >= wave.EnemyCount || activeEnemies >= config.BattleFlow.MaxActiveEnemies)
                {
                    continue;
                }

                var framesSinceStart = frame - wave.StartFrame;
                if (framesSinceStart % wave.SpawnFrameInterval != 0)
                {
                    continue;
                }

                SpawnEnemy(in config, in wave, _waveSpawned[i]);
                _waveSpawned[i]++;
                activeEnemies++;
            }
        }

        private void SpawnEnemy(in ShooterSveltoGameplayScenarioConfig config, in ShooterSveltoGameplayWaveConfig wave, int spawnIndex)
        {
            var targetId = _nextTargetId++;
            var angle = (wave.WaveId * 97 + spawnIndex * 37) * Pi / 180f;
            var x = MathF.Cos(angle) * wave.SpawnRadius;
            var y = MathF.Sin(angle) * wave.SpawnRadius;
            var dx = -x;
            var dy = -y;
            Normalize(ref dx, ref dy);

            var transform = new ShooterSveltoTransformComponent
            {
                X = x,
                Y = y,
                DirectionX = dx,
                DirectionY = dy
            };
            var health = new ShooterSveltoHealthComponent
            {
                Current = wave.EnemyHp,
                Max = wave.EnemyHp,
                Alive = 1
            };
            ShooterSveltoEntityLayout.BuildGameplayTarget(_context, targetId, in transform, in health);

            _context.SubmitEntities();
        }

        private void TickShooters(in ShooterSveltoGameplayScenarioConfig config)
        {
            var (transforms, weapons, cooldowns, targets, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoWeaponComponent, ShooterSveltoCooldownComponent, ShooterSveltoTargetComponent>(ShooterSveltoGroups.GameplayShooters);
            for (var i = 0; i < count; i++)
            {
                ref var health = ref _context.EntitiesDB.QueryEntity<ShooterSveltoHealthComponent>(ids[i], ShooterSveltoGroups.GameplayShooters);
                if (health.Alive == 0)
                {
                    continue;
                }

                ref var cooldown = ref cooldowns[i];
                if (cooldown.RemainingFrames > 0)
                {
                    cooldown.RemainingFrames--;
                    continue;
                }

                ref var transform = ref transforms[i];
                ref var weapon = ref weapons[i];
                ref var target = ref targets[i];
                var targetId = AcquireLiveEnemyTarget(target.TargetEntityId);
                if (targetId == 0)
                {
                    cooldown.RemainingFrames = 1;
                    continue;
                }

                ref var targetTransform = ref _context.EntitiesDB.QueryEntity<ShooterSveltoTransformComponent>(targetId, ShooterSveltoGroups.GameplayTargets);
                var dx = targetTransform.X - transform.X;
                var dy = targetTransform.Y - transform.Y;
                Normalize(ref dx, ref dy);
                transform.DirectionX = dx;
                transform.DirectionY = dy;
                target.TargetEntityId = targetId;

                FireBurst(in transform, in weapon, targetId, ShooterSveltoGroups.GameplayTargets, config.TargetCount);
                cooldown.RemainingFrames = weapon.CooldownFrames;
            }
        }

        private void TickEnemies(in ShooterSveltoGameplayScenarioConfig config, int frame)
        {
            if (config.ShooterCount <= 0 || frame % 12 != 0)
            {
                return;
            }

            var enemyWeapon = new ShooterSveltoWeaponComponent
            {
                LoadoutId = 100,
                ProjectileSpeed = config.Loadout.ProjectileSpeed * 0.55f,
                ProjectileLifeFrames = config.Loadout.ProjectileLifeFrames,
                Damage = 1,
                CooldownFrames = 12,
                ProjectilesPerShot = 1,
                SpreadRadians = 0f
            };

            var (enemyTransforms, enemyHealths, enemyIds, enemyCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < enemyCount; i++)
            {
                if (enemyHealths[i].Alive == 0)
                {
                    continue;
                }

                var shooterId = (uint)((enemyIds[i] % (uint)config.ShooterCount) + 1u);
                if (!_context.EntitiesDB.Exists<ShooterSveltoHealthComponent>(shooterId, ShooterSveltoGroups.GameplayShooters))
                {
                    continue;
                }

                ref var shooterHealth = ref _context.EntitiesDB.QueryEntity<ShooterSveltoHealthComponent>(shooterId, ShooterSveltoGroups.GameplayShooters);
                if (shooterHealth.Alive == 0)
                {
                    continue;
                }

                ref var shooterTransform = ref _context.EntitiesDB.QueryEntity<ShooterSveltoTransformComponent>(shooterId, ShooterSveltoGroups.GameplayShooters);
                var enemyTransform = enemyTransforms[i];
                var dx = shooterTransform.X - enemyTransform.X;
                var dy = shooterTransform.Y - enemyTransform.Y;
                Normalize(ref dx, ref dy);
                enemyTransform.DirectionX = dx;
                enemyTransform.DirectionY = dy;
                FireBurst(in enemyTransform, in enemyWeapon, shooterId, ShooterSveltoGroups.GameplayShooters, config.ShooterCount);
            }
        }

        private uint AcquireLiveEnemyTarget(uint currentTargetId)
        {
            if (currentTargetId != 0 && IsAlive(currentTargetId, ShooterSveltoGroups.GameplayTargets))
            {
                return currentTargetId;
            }

            var (_, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive != 0)
                {
                    return ids[i];
                }
            }

            return 0;
        }

        private int CountAliveEnemies()
        {
            var alive = 0;
            var (healths, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive != 0)
                {
                    alive++;
                }
            }

            return alive;
        }

        private bool IsAlive(uint entityId, ExclusiveGroupStruct group)
        {
            if (!_context.EntitiesDB.Exists<ShooterSveltoHealthComponent>(entityId, group))
            {
                return false;
            }

            ref var health = ref _context.EntitiesDB.QueryEntity<ShooterSveltoHealthComponent>(entityId, group);
            return health.Alive != 0;
        }

        private void FireBurst(
            in ShooterSveltoTransformComponent shooter,
            in ShooterSveltoWeaponComponent weapon,
            uint targetEntityId,
            ExclusiveGroupStruct targetGroup,
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
                var targetId = targetEntityId == 0 ? (projectileId % (uint)targetCount + 1u) : targetEntityId;
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
                    OwnerEntityId = targetGroup == ShooterSveltoGroups.GameplayTargets ? 0u : targetId,
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

                var targetGroup = damage.OwnerEntityId == 0u ? ShooterSveltoGroups.GameplayTargets : ShooterSveltoGroups.GameplayShooters;
                if (TryApplyHit(in transform, in damage, targetGroup))
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

        private bool TryApplyHit(in ShooterSveltoTransformComponent projectile, in ShooterSveltoProjectileDamageComponent damage, ExclusiveGroupStruct targetGroup)
        {
            if (damage.TargetEntityId == 0)
            {
                return false;
            }

            if (!_context.EntitiesDB.Exists<ShooterSveltoHealthComponent>(damage.TargetEntityId, targetGroup))
            {
                return false;
            }

            ref var targetHealth = ref _context.EntitiesDB.QueryEntity<ShooterSveltoHealthComponent>(damage.TargetEntityId, targetGroup);
            if (targetHealth.Alive == 0)
            {
                return false;
            }

            ref var targetTransform = ref _context.EntitiesDB.QueryEntity<ShooterSveltoTransformComponent>(damage.TargetEntityId, targetGroup);
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
                if (targetGroup == ShooterSveltoGroups.GameplayTargets)
                {
                    _defeatedTargets++;
                }
            }

            if (targetGroup == ShooterSveltoGroups.GameplayShooters)
            {
                _enemyHits++;
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
                targetCount,
                _projectilesSpawned,
                _projectilesExpired,
                _hits,
                _defeatedTargets,
                activeProjectiles,
                remainingHp,
                _enemyHits,
                ComputeStateHash());
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

            var (shooterHealths, shooterCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayShooters);
            for (var i = 0; i < shooterCount; i++)
            {
                Mix(ref hash, shooterHealths[i].Current);
                Mix(ref hash, shooterHealths[i].Alive);
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
