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
        private readonly ShooterSveltoGameplayScenarioWaveSpawnSystem _waveSpawnSystem;
        private readonly ShooterSveltoGameplayScenarioProjectileSystem _projectileSystem;
        private readonly ShooterSveltoGameplayScenarioResultCollector _resultCollector;
        private readonly Dictionary<uint, int> _targetIndexByEntityId = new();
        private readonly Dictionary<uint, int> _shooterIndexByEntityId = new();

        public ShooterSveltoGameplayScenarioRunner(ISveltoWorldContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _waveSpawnSystem = new ShooterSveltoGameplayScenarioWaveSpawnSystem(_context);
            _projectileSystem = new ShooterSveltoGameplayScenarioProjectileSystem(_context);
            _resultCollector = new ShooterSveltoGameplayScenarioResultCollector(_context);
        }

        public ShooterSveltoGameplayScenarioResult Run(in ShooterSveltoGameplayScenarioConfig config)
        {
            ResetGroups();
            BuildScenario(in config);

            for (var frame = 0; frame < config.TickCount; frame++)
            {
                _waveSpawnSystem.Tick(in config, frame);
                TickShooters(in config);
                TickEnemies(in config, frame);
                _projectileSystem.Tick(config.TickDeltaTime);
            }

            return _resultCollector.BuildResult(in config, _projectileSystem.CreateCounters());
        }

        private void ResetGroups()
        {
            var removed = false;
            removed |= ShooterSveltoGameplayScenarioEcsUtility.RemoveGroupIfExists(_context, ShooterSveltoGroups.GameplayShooters);
            removed |= ShooterSveltoGameplayScenarioEcsUtility.RemoveGroupIfExists(_context, ShooterSveltoGroups.GameplayTargets);
            removed |= ShooterSveltoGameplayScenarioEcsUtility.RemoveGroupIfExists(_context, ShooterSveltoGroups.GameplayProjectiles);

            _projectileSystem.Reset();

            if (removed)
            {
                _context.SubmitEntities();
            }
        }

        private void BuildScenario(in ShooterSveltoGameplayScenarioConfig config)
        {
            _waveSpawnSystem.Reset(in config);

            var spreadRadians = config.Loadout.SpreadDegrees * Pi / 180f;
            for (uint i = 0; i < config.ShooterCount; i++)
            {
                var angle = i * 2f * Pi / config.ShooterCount;
                var shooterX = MathF.Cos(angle) * config.ArenaRadius * 0.12f;
                var shooterY = MathF.Sin(angle) * config.ArenaRadius * 0.12f;
                var dx = MathF.Cos(angle);
                var dy = MathF.Sin(angle);
                ShooterSveltoGameplayScenarioEcsUtility.Normalize(ref dx, ref dy);

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

        private void TickShooters(in ShooterSveltoGameplayScenarioConfig config)
        {
            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoWeaponComponent, ShooterSveltoCooldownComponent, ShooterSveltoTargetComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> transforms, out NB<ShooterSveltoWeaponComponent> weapons, out NB<ShooterSveltoCooldownComponent> cooldowns, out NB<ShooterSveltoTargetComponent> targets, out _, out var count);
            var healthCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            healthCollection.Deconstruct(out NB<ShooterSveltoHealthComponent> healths, out _, out var healthCount);
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            ShooterSveltoGameplayScenarioEcsUtility.RebuildIndex(_targetIndexByEntityId, enemyIds, enemyCount);
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
                if (targetId == 0 || !ShooterSveltoGameplayScenarioEcsUtility.TryGetTransform(targetId, enemyTransforms, _targetIndexByEntityId, out var targetTransform))
                {
                    cooldown.RemainingFrames = 1;
                    continue;
                }

                var dx = targetTransform.X - transform.X;
                var dy = targetTransform.Y - transform.Y;
                ShooterSveltoGameplayScenarioEcsUtility.Normalize(ref dx, ref dy);
                transform.DirectionX = dx;
                transform.DirectionY = dy;
                target.TargetEntityId = targetId;

                spawned |= _projectileSystem.FireBurst(in transform, in weapon, targetId, ShooterSveltoGroups.GameplayTargets, config.TargetCount);
                cooldown.RemainingFrames = weapon.CooldownFrames;
            }

            if (spawned)
            {
                _context.SubmitEntities();
            }
        }

        private void TickEnemies(in ShooterSveltoGameplayScenarioConfig config, int frame)
        {
            var battleFlow = config.BattleFlow;
            if (config.ShooterCount <= 0 || frame % battleFlow.EnemyAttackIntervalFrames != 0)
            {
                return;
            }

            var enemyWeapon = new ShooterSveltoWeaponComponent
            {
                LoadoutId = battleFlow.EnemyLoadoutId,
                ProjectileSpeed = config.Loadout.ProjectileSpeed * battleFlow.EnemyProjectileSpeedScale,
                ProjectileLifeFrames = config.Loadout.ProjectileLifeFrames,
                Damage = battleFlow.EnemyAttackDamage,
                CooldownFrames = battleFlow.EnemyAttackIntervalFrames,
                ProjectilesPerShot = battleFlow.EnemyProjectilesPerShot,
                SpreadRadians = battleFlow.EnemySpreadDegrees * Pi / 180f
            };

            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            var shooterCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayShooters);
            shooterCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> shooterTransforms, out NB<ShooterSveltoHealthComponent> shooterHealths, out NativeEntityIDs shooterIds, out var shooterCount);
            ShooterSveltoGameplayScenarioEcsUtility.RebuildIndex(_shooterIndexByEntityId, shooterIds, shooterCount);
            var spawned = false;
            for (var i = 0; i < enemyCount; i++)
            {
                if (enemyHealths[i].Alive == 0)
                {
                    continue;
                }

                var shooterId = (uint)((enemyIds[i] % (uint)config.ShooterCount) + 1u);
                if (!ShooterSveltoGameplayScenarioEcsUtility.TryGetLiveTarget(shooterId, shooterTransforms, shooterHealths, _shooterIndexByEntityId, out var shooterTransform))
                {
                    continue;
                }

                var enemyTransform = enemyTransforms[i];
                var dx = shooterTransform.X - enemyTransform.X;
                var dy = shooterTransform.Y - enemyTransform.Y;
                ShooterSveltoGameplayScenarioEcsUtility.Normalize(ref dx, ref dy);
                enemyTransform.DirectionX = dx;
                enemyTransform.DirectionY = dy;
                spawned |= _projectileSystem.FireBurst(in enemyTransform, in enemyWeapon, shooterId, ShooterSveltoGroups.GameplayShooters, config.ShooterCount);
            }

            if (spawned)
            {
                _context.SubmitEntities();
            }
        }

        private static uint AcquireLiveEnemyTarget(uint currentTargetId, NB<ShooterSveltoHealthComponent> healths, NativeEntityIDs ids, int count, Dictionary<uint, int> indexByEntityId)
        {
            if (currentTargetId != 0 && ShooterSveltoGameplayScenarioEcsUtility.IsAlive(currentTargetId, healths, indexByEntityId))
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

    }
}
