#nullable enable

using System;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterEnemyWaveMovementBattleSystem : IShooterBattleSystem
    {
        private const float StopDistance = 0.75f;
        private readonly ShooterBattleState _state;
        private readonly ISveltoWorldContext _context;
        private readonly ShooterEnemyWaveOptions _options;
        private readonly ShooterArenaGameplayOptions _arenaOptions;
        private readonly ShooterSpatialTargetIndex _targetIndex = new ShooterSpatialTargetIndex();

        public ShooterEnemyWaveMovementBattleSystem(IShooterBattleServiceResolver services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            _state = services.Resolve<ShooterBattleState>();
            _context = services.Resolve<ISveltoWorldContext>();
            _options = services.TryResolve<ShooterEnemyWaveOptions>(out var options) && options != null
                ? options
                : ShooterEnemyWaveOptions.Disabled;
            _arenaOptions = services.TryResolve<ShooterArenaGameplayOptions>(out var arenaOptions) && arenaOptions != null
                ? arenaOptions
                : ShooterArenaGameplayOptions.Disabled;
        }

        public int Order => ShooterBattleSystemOrder.EnemyWaveMovement;

        public string name => nameof(ShooterEnemyWaveMovementBattleSystem);

        public void Step(in float deltaTime)
        {
            if (!_options.Enabled || _state.MatchState != ShooterBattleMatchState.Running || deltaTime <= 0f)
            {
                return;
            }

            _targetIndex.Rebuild(_context, _state.CurrentFrame);
            if (_targetIndex.IndexedPlayerCount == 0)
            {
                return;
            }

            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out _, out var enemyCount);
            for (var i = 0; i < enemyCount; i++)
            {
                if (enemyHealths[i].Alive == 0 || enemyHealths[i].Current <= 0)
                {
                    continue;
                }

                ref var enemyTransform = ref enemyTransforms[i];
                if (!_targetIndex.TryFindNearestTarget(
                    enemyTransform.X,
                    enemyTransform.Y,
                    selfPlayerId: 0,
                    out _,
                    out var targetX,
                    out var targetY,
                    out var distanceSquared))
                {
                    continue;
                }

                var directionX = targetX - enemyTransform.X;
                var directionY = targetY - enemyTransform.Y;
                ShooterBattleMath.Normalize(ref directionX, ref directionY);
                enemyTransform.DirectionX = directionX;
                enemyTransform.DirectionY = directionY;

                var distance = MathF.Sqrt(distanceSquared);
                if (distance <= StopDistance)
                {
                    continue;
                }

                var moveDistance = MathF.Min(distance - StopDistance, ShooterBattleTuning.EnemySpeed * deltaTime);
                enemyTransform.X += directionX * moveDistance;
                enemyTransform.Y += directionY * moveDistance;
                ShooterCircularArenaMath.Clamp(ref enemyTransform.X, ref enemyTransform.Y, _arenaOptions);
            }
        }

    }
}
