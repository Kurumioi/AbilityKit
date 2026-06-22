#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

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
        private readonly Dictionary<uint, int> _targetIndexByEntityId = new();
        private readonly Dictionary<uint, int> _shooterIndexByEntityId = new();
        private readonly List<uint> _projectileRemovalBuffer = new(1024);
        private readonly List<PendingEnemySpawn> _enemySpawnBuffer = new(64);

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
            _enemySpawnBuffer.Clear();
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

                QueueEnemySpawn(in wave, _waveSpawned[i]);
                _waveSpawned[i]++;
                activeEnemies++;
            }

            FlushEnemySpawns();
        }

        private void QueueEnemySpawn(in ShooterSveltoGameplayWaveConfig wave, int spawnIndex)
        {
            var targetId = _nextTargetId++;
            var angle = (wave.WaveId * 97 + spawnIndex * 37) * Pi / 180f;
            var x = MathF.Cos(angle) * wave.SpawnRadius;
            var y = MathF.Sin(angle) * wave.SpawnRadius;
            var dx = -x;
            var dy = -y;
            Normalize(ref dx, ref dy);

            _enemySpawnBuffer.Add(new PendingEnemySpawn(
                targetId,
                new ShooterSveltoTransformComponent
                {
                    X = x,
                    Y = y,
                    DirectionX = dx,
                    DirectionY = dy
                },
                new ShooterSveltoHealthComponent
                {
                    Current = wave.EnemyHp,
                    Max = wave.EnemyHp,
                    Alive = 1
                }));
        }

        private void FlushEnemySpawns()
        {
            if (_enemySpawnBuffer.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _enemySpawnBuffer.Count; i++)
            {
                var spawn = _enemySpawnBuffer[i];
                var transform = spawn.Transform;
                var health = spawn.Health;
                ShooterSveltoEntityLayout.BuildGameplayTarget(_context, spawn.EntityId, in transform, in health);
            }

            _context.SubmitEntities();
        }

        private void TickShooters(in ShooterSveltoGameplayScenarioConfig config)
        {
            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoWeaponComponent, ShooterSveltoCooldownComponent, ShooterSveltoTargetComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> transforms, out NB<ShooterSveltoWeaponComponent> weapons, out NB<ShooterSveltoCooldownComponent> cooldowns, out NB<ShooterSveltoTargetComponent> targets, out _, out var count);
            var healthCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            healthCollection.Deconstruct(out NB<ShooterSveltoHealthComponent> healths, out _, out var healthCount);
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            RebuildIndex(_targetIndexByEntityId, enemyIds, enemyCount);
            var spawned = false;
            var shooterCount = Math.Min(count, healthCount);
            for (var i = 0; i < shooterCount; i++)
            {
                if (healths[i].Alive == 0)
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
                var targetId = AcquireLiveEnemyTarget(target.TargetEntityId, enemyHealths, enemyIds, enemyCount, _targetIndexByEntityId);
                if (targetId == 0 || !TryGetTransform(targetId, enemyTransforms, _targetIndexByEntityId, out var targetTransform))
                {
                    cooldown.RemainingFrames = 1;
                    continue;
                }

                var dx = targetTransform.X - transform.X;
                var dy = targetTransform.Y - transform.Y;
                Normalize(ref dx, ref dy);
                transform.DirectionX = dx;
                transform.DirectionY = dy;
                target.TargetEntityId = targetId;

                spawned |= FireBurst(in transform, in weapon, targetId, ShooterSveltoGroups.GameplayTargets, config.TargetCount);
                cooldown.RemainingFrames = weapon.CooldownFrames;
            }

            if (spawned)
            {
                _context.SubmitEntities();
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

            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> shooterTransforms, out NB<ShooterSveltoHealthComponent> shooterHealths, out NativeEntityIDs shooterIds, out var shooterCount);
            RebuildIndex(_shooterIndexByEntityId, shooterIds, shooterCount);
            var spawned = false;
            for (var i = 0; i < enemyCount; i++)
            {
                if (enemyHealths[i].Alive == 0)
                {
                    continue;
                }

                var shooterId = (uint)((enemyIds[i] % (uint)config.ShooterCount) + 1u);
                if (!TryGetLiveTarget(shooterId, shooterTransforms, shooterHealths, _shooterIndexByEntityId, out var shooterTransform))
                {
                    continue;
                }

                var enemyTransform = enemyTransforms[i];
                var dx = shooterTransform.X - enemyTransform.X;
                var dy = shooterTransform.Y - enemyTransform.Y;
                Normalize(ref dx, ref dy);
                enemyTransform.DirectionX = dx;
                enemyTransform.DirectionY = dy;
                spawned |= FireBurst(in enemyTransform, in enemyWeapon, shooterId, ShooterSveltoGroups.GameplayShooters, config.ShooterCount);
            }

            if (spawned)
            {
                _context.SubmitEntities();
            }
        }

        private static uint AcquireLiveEnemyTarget(uint currentTargetId, NB<ShooterSveltoHealthComponent> healths, NativeEntityIDs ids, int count, Dictionary<uint, int> indexByEntityId)
        {
            if (currentTargetId != 0 && IsAlive(currentTargetId, healths, indexByEntityId))
            {
                return currentTargetId;
            }

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
            var healthCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            healthCollection.Deconstruct(out NB<ShooterSveltoHealthComponent> healths, out _, out var count);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive != 0)
                {
                    alive++;
                }
            }

            return alive;
        }

        private static bool IsAlive(uint entityId, NB<ShooterSveltoHealthComponent> healths, Dictionary<uint, int> indexByEntityId)
        {
            return indexByEntityId.TryGetValue(entityId, out var index) && healths[index].Alive != 0;
        }

        private static bool TryGetTransform(uint entityId, NB<ShooterSveltoTransformComponent> transforms, Dictionary<uint, int> indexByEntityId, out ShooterSveltoTransformComponent transform)
        {
            if (indexByEntityId.TryGetValue(entityId, out var index))
            {
                transform = transforms[index];
                return true;
            }

            transform = default;
            return false;
        }

        private static bool TryGetLiveTarget(
            uint entityId,
            NB<ShooterSveltoTransformComponent> transforms,
            NB<ShooterSveltoHealthComponent> healths,
            Dictionary<uint, int> indexByEntityId,
            out ShooterSveltoTransformComponent transform)
        {
            if (indexByEntityId.TryGetValue(entityId, out var index) && healths[index].Alive != 0)
            {
                transform = transforms[index];
                return true;
            }

            transform = default;
            return false;
        }

        private static void RebuildIndex(Dictionary<uint, int> indexByEntityId, NativeEntityIDs ids, int count)
        {
            indexByEntityId.Clear();
            for (var i = 0; i < count; i++)
            {
                indexByEntityId[ids[i]] = i;
            }
        }

        private bool FireBurst(
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

            return weapon.ProjectilesPerShot > 0;
        }

        private void TickProjectiles(float deltaTime)
        {
            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoProjectileComponent, ShooterSveltoProjectileDamageComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayProjectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> transforms, out NB<ShooterSveltoProjectileComponent> projectiles, out NB<ShooterSveltoProjectileDamageComponent> damageComponents, out NativeEntityIDs ids, out var count);
            var targetCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            targetCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> targetTransforms, out NB<ShooterSveltoHealthComponent> targetHealths, out NativeEntityIDs targetIds, out var targetCount);
            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> shooterTransforms, out NB<ShooterSveltoHealthComponent> shooterHealths, out NativeEntityIDs shooterIds, out var shooterCount);
            RebuildIndex(_targetIndexByEntityId, targetIds, targetCount);
            RebuildIndex(_shooterIndexByEntityId, shooterIds, shooterCount);
            _projectileRemovalBuffer.Clear();
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

                var hit = damage.OwnerEntityId == 0u
                    ? TryApplyHit(in transform, in damage, ShooterSveltoGroups.GameplayTargets, targetTransforms, targetHealths, _targetIndexByEntityId)
                    : TryApplyHit(in transform, in damage, ShooterSveltoGroups.GameplayShooters, shooterTransforms, shooterHealths, _shooterIndexByEntityId);
                if (hit)
                {
                    _projectileRemovalBuffer.Add(ids[i]);
                    _hits++;
                    continue;
                }

                if (projectile.RemainingFrames <= 0)
                {
                    _projectileRemovalBuffer.Add(ids[i]);
                    _projectilesExpired++;
                }
            }

            FlushProjectileRemovals();
        }

        private bool TryApplyHit(
            in ShooterSveltoTransformComponent projectile,
            in ShooterSveltoProjectileDamageComponent damage,
            ExclusiveGroupStruct targetGroup,
            NB<ShooterSveltoTransformComponent> targetTransforms,
            NB<ShooterSveltoHealthComponent> targetHealths,
            Dictionary<uint, int> targetIndexByEntityId)
        {
            if (damage.TargetEntityId == 0 || !targetIndexByEntityId.TryGetValue(damage.TargetEntityId, out var targetIndex) || targetHealths[targetIndex].Alive == 0)
            {
                return false;
            }

            var dx = targetTransforms[targetIndex].X - projectile.X;
            var dy = targetTransforms[targetIndex].Y - projectile.Y;
            if (dx * dx + dy * dy > 0.64f)
            {
                return false;
            }

            targetHealths[targetIndex].Current = Math.Max(0, targetHealths[targetIndex].Current - damage.Damage);
            if (targetHealths[targetIndex].Current == 0)
            {
                targetHealths[targetIndex].Alive = 0;
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

        private void FlushProjectileRemovals()
        {
            if (_projectileRemovalBuffer.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _projectileRemovalBuffer.Count; i++)
            {
                _context.EntityFunctions.RemoveEntity<ShooterSveltoGameplayProjectileDescriptor>(_projectileRemovalBuffer[i], ShooterSveltoGroups.GameplayProjectiles);
            }

            _context.SubmitEntities();
        }

        private ShooterSveltoGameplayScenarioResult BuildResult(in ShooterSveltoGameplayScenarioConfig config)
        {
            var remainingHp = 0;
            var healthCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            healthCollection.Deconstruct(out NB<ShooterSveltoHealthComponent> healths, out _, out var targetCount);
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
            var targetCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            targetCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> targetTransforms, out NB<ShooterSveltoHealthComponent> targetHealths, out _, out var targetCount);
            for (var i = 0; i < targetCount; i++)
            {
                Mix(ref hash, Quantize(targetTransforms[i].X));
                Mix(ref hash, Quantize(targetTransforms[i].Y));
                Mix(ref hash, targetHealths[i].Current);
                Mix(ref hash, targetHealths[i].Alive);
            }

            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoHealthComponent> shooterHealths, out _, out var shooterCount);
            for (var i = 0; i < shooterCount; i++)
            {
                Mix(ref hash, shooterHealths[i].Current);
                Mix(ref hash, shooterHealths[i].Alive);
            }

            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayProjectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> projectileTransforms, out NB<ShooterSveltoProjectileComponent> projectileComponents, out _, out var projectileCount);
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

        private readonly struct PendingEnemySpawn
        {
            public PendingEnemySpawn(uint entityId, ShooterSveltoTransformComponent transform, ShooterSveltoHealthComponent health)
            {
                EntityId = entityId;
                Transform = transform;
                Health = health;
            }

            public uint EntityId { get; }
            public ShooterSveltoTransformComponent Transform { get; }
            public ShooterSveltoHealthComponent Health { get; }
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
