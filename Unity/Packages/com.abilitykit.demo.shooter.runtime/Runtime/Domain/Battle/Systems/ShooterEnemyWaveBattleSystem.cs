#nullable enable

using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal enum ShooterEnemyWavePhase
    {
        Spawn = 0,
        Attack = 1
    }

    [AllowMultiple]
    internal sealed class ShooterEnemyWaveBattleSystem : IShooterBattleSystem
    {
        private const float Pi = 3.14159265358979323846f;
        private const int FirstEnemyEntityId = 10000;
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ISveltoWorldContext _context;
        private readonly ShooterEnemyWaveOptions _options;
        private readonly ShooterSveltoGameplayWaveConfig[] _waves;
        private readonly int[] _waveSpawned;
        private readonly ShooterSpatialTargetIndex _targetIndex = new();
        private readonly ShooterEnemyWavePhase _phase;
        private int _nextEnemyId = FirstEnemyEntityId;
        private int _lastSynchronizedFrame = -1;

        public ShooterEnemyWaveBattleSystem(IShooterBattleServiceResolver services)
            : this(services, ShooterEnemyWavePhase.Spawn)
        {
        }

        public ShooterEnemyWaveBattleSystem(IShooterBattleServiceResolver services, ShooterEnemyWavePhase phase)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            _state = services.Resolve<ShooterBattleState>();
            _entities = services.Resolve<IShooterEntityManager>();
            _context = services.Resolve<ISveltoWorldContext>();
            _options = services.TryResolve<ShooterEnemyWaveOptions>(out var options) && options != null
                ? options
                : ShooterEnemyWaveOptions.Disabled;
            _waves = _options.Waves;
            _waveSpawned = new int[_waves.Length];
            _phase = phase;
        }

        public int Order => _phase == ShooterEnemyWavePhase.Spawn ? ShooterBattleSystemOrder.EnemyWaveSpawn : ShooterBattleSystemOrder.EnemyWaveAttack;

        public string name => _phase == ShooterEnemyWavePhase.Spawn
            ? nameof(ShooterEnemyWaveBattleSystem) + ".Spawn"
            : nameof(ShooterEnemyWaveBattleSystem) + ".Attack";

        public void Step(in float deltaTime)
        {
            if (!_options.Enabled)
            {
                return;
            }

            if (_phase == ShooterEnemyWavePhase.Spawn)
            {
                if (_state.CurrentFrame <= 1)
                {
                    ResetWaveState();
                }
                else
                {
                    SynchronizeImportedWaveState();
                }

                TickWaveSpawns();
            }
            else
            {
                TickEnemyAttacks();
            }
        }

        private void ResetWaveState()
        {
            if (_context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.GameplayTargets))
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.GameplayTargets);
                _context.SubmitEntities();
            }

            Array.Clear(_waveSpawned, 0, _waveSpawned.Length);
            _nextEnemyId = FirstEnemyEntityId;
            _lastSynchronizedFrame = _state.CurrentFrame;
        }

        private void SynchronizeImportedWaveState()
        {
            if (_lastSynchronizedFrame == _state.CurrentFrame)
            {
                return;
            }

            _nextEnemyId = Math.Max(_nextEnemyId, SynchronizeNextEnemyIdFromExistingTargets());
            _lastSynchronizedFrame = _state.CurrentFrame;
        }

        private int SynchronizeNextEnemyIdFromExistingTargets()
        {
            var (_, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            var nextEnemyId = FirstEnemyEntityId;
            var importedSpawnCount = 0;
            for (var i = 0; i < count; i++)
            {
                var entityId = checked((int)ids[i]);
                nextEnemyId = Math.Max(nextEnemyId, entityId + 1);
                if (entityId >= FirstEnemyEntityId)
                {
                    importedSpawnCount = Math.Max(importedSpawnCount, entityId - FirstEnemyEntityId + 1);
                }
            }

            SynchronizeSpawnedWaveCounters(importedSpawnCount);
            return nextEnemyId;
        }

        private void TickWaveSpawns()
        {
            if (_waves.Length == 0)
            {
                return;
            }

            var activeEnemies = CountAliveEnemies();
            for (var i = 0; i < _waves.Length; i++)
            {
                TickWave(i, in _waves[i], ref activeEnemies);
            }
        }

        private void TickWave(int index, in ShooterSveltoGameplayWaveConfig wave, ref int activeEnemies)
        {
            if (_state.CurrentFrame < wave.StartFrame || _waveSpawned[index] >= wave.EnemyCount || activeEnemies >= _options.MaxActiveEnemies)
            {
                return;
            }

            var framesSinceStart = _state.CurrentFrame - wave.StartFrame;
            if (framesSinceStart % wave.SpawnFrameInterval != 0)
            {
                return;
            }

            SpawnEnemy(wave.WaveId, _waveSpawned[index], wave.EnemyHp, wave.SpawnRadius);
            _waveSpawned[index]++;
            activeEnemies++;
        }

        private void SynchronizeSpawnedWaveCounters(int importedSpawnCount)
        {
            var remaining = Math.Max(0, importedSpawnCount);
            for (var i = 0; i < _waves.Length; i++)
            {
                var spawnedInWave = Math.Min(remaining, _waves[i].EnemyCount);
                _waveSpawned[i] = Math.Max(_waveSpawned[i], spawnedInWave);
                remaining -= spawnedInWave;
                if (remaining <= 0)
                {
                    break;
                }
            }
        }

        private void SpawnEnemy(int waveId, int spawnIndex, int enemyHp, float spawnRadius)
        {
            var enemyId = (uint)_nextEnemyId++;
            var angle = (waveId * 97 + spawnIndex * 37) * Pi / 180f;
            var x = MathF.Cos(angle) * spawnRadius;
            var y = MathF.Sin(angle) * spawnRadius;
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
            ShooterSveltoEntityLayout.BuildGameplayTarget(_context, enemyId, in transform, in health);
            _context.SubmitEntities();
        }

        private void TickEnemyAttacks()
        {
            if (_state.CurrentFrame % 12 != 0 || _entities.PlayerCount == 0)
            {
                return;
            }

            _targetIndex.Rebuild(_context, _state.CurrentFrame);
            var (transforms, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive == 0)
                {
                    continue;
                }

                if (!_targetIndex.TryGetLivePlayerByPosition(transforms[i].X, transforms[i].Y, out var player))
                {
                    continue;
                }

                player.Hp = Math.Max(0, player.Hp - 1);
                if (player.Hp == 0)
                {
                    player.Alive = false;
                }

                _entities.SetPlayer(in player);
                _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, -(int)ids[i], player.PlayerId, 0, transforms[i].X, transforms[i].Y, 1));
            }
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
