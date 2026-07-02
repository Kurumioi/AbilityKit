#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterEnemyWaveSpawnDirector
    {
        private const float Pi = 3.14159265358979323846f;
        private readonly ShooterEnemyWaveOptions _options;
        private readonly ShooterSveltoGameplayWaveConfig[] _waves;
        private readonly ShooterEnemyWaveProgress _progress;
        private readonly ShooterEnemyIdAllocator _allocator;
        private readonly IShooterEntityManager _entities;
        private readonly ShooterArenaGameplayOptions _arenaOptions;

        public ShooterEnemyWaveSpawnDirector(
            ShooterEnemyWaveOptions options,
            ShooterEnemyWaveProgress progress,
            ShooterEnemyIdAllocator allocator,
            IShooterEntityManager entities)
            : this(options, progress, allocator, entities, ShooterArenaGameplayOptions.Disabled)
        {
        }

        public ShooterEnemyWaveSpawnDirector(
            ShooterEnemyWaveOptions options,
            ShooterEnemyWaveProgress progress,
            ShooterEnemyIdAllocator allocator,
            IShooterEntityManager entities,
            ShooterArenaGameplayOptions arenaOptions)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _arenaOptions = arenaOptions ?? ShooterArenaGameplayOptions.Disabled;
            _waves = _options.Waves;
        }

        public void Reset()
        {
            _progress.Reset();
            _allocator.Reset();
        }

        public void SynchronizeFromImportedTargets(int importedSpawnCount)
        {
            _progress.RestoreFromSpawnCount(importedSpawnCount, _waves);
        }

        public void Tick(ShooterBattleState state)
        {
            if (!_options.Enabled || _waves.Length == 0)
            {
                return;
            }

            var activeEnemies = _entities.EnemyCount;
            for (var i = 0; i < _waves.Length; i++)
            {
                TickWave(state, i, in _waves[i], ref activeEnemies);
            }
        }

        private void TickWave(ShooterBattleState state, int index, in ShooterSveltoGameplayWaveConfig wave, ref int activeEnemies)
        {
            if (state.CurrentFrame < wave.StartFrame || _progress.GetSpawned(index) >= wave.EnemyCount || activeEnemies >= _options.MaxActiveEnemies)
            {
                return;
            }

            var framesSinceStart = state.CurrentFrame - wave.StartFrame;
            if (framesSinceStart % wave.SpawnFrameInterval != 0)
            {
                return;
            }

            SpawnEnemy(wave.WaveId, _progress.GetSpawned(index), wave.EnemyHp, wave.SpawnRadius);
            _progress.Increment(index);
            activeEnemies++;
        }

        private void SpawnEnemy(int waveId, int spawnIndex, int enemyHp, float spawnRadius)
        {
            var enemyId = _allocator.Allocate();
            var angle = (waveId * 97 + spawnIndex * 37) * Pi / 180f;
            var activeSpawnRadius = ShooterCircularArenaMath.ClampSpawnRadius(spawnRadius, _arenaOptions);
            var x = MathF.Cos(angle) * activeSpawnRadius;
            var y = MathF.Sin(angle) * activeSpawnRadius;
            var directionX = -x;
            var directionY = -y;
            Normalize(ref directionX, ref directionY);

            var transform = new ShooterSveltoTransformComponent
            {
                X = x,
                Y = y,
                DirectionX = directionX,
                DirectionY = directionY
            };
            var health = new ShooterSveltoHealthComponent
            {
                Current = enemyHp,
                Max = enemyHp,
                Alive = 1
            };
            _entities.AddEnemy(enemyId, in transform, in health);
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
