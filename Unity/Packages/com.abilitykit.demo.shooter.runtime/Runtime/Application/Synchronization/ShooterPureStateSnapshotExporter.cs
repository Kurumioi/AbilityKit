using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterPureStateSnapshotExporter
    {
        private const int PositionScale = 1000;
        private const int VelocityScale = 1000;

        private readonly ShooterBattleState _state;
        private readonly IShooterSnapshotReadPort _snapshotReadPort;
        private readonly IShooterStateHashProvider _stateHashProvider;
        private readonly ISveltoWorldContext? _context;
        private readonly ShooterSnapshotOrderBuffer _orderBuffer = new();
        private readonly ShooterPureStateInterestPolicy _interestPolicy = new();
        private ShooterPureStateCandidate[] _candidateBuffer = Array.Empty<ShooterPureStateCandidate>();

        public ShooterPureStateSnapshotExporter(
            ShooterBattleState state,
            IShooterSnapshotReadPort snapshotReadPort,
            IShooterStateHashProvider stateHashProvider)
            : this(state, snapshotReadPort, stateHashProvider, entities: null)
        {
        }

        public ShooterPureStateSnapshotExporter(
            ShooterBattleState state,
            IShooterSnapshotReadPort snapshotReadPort,
            IShooterStateHashProvider stateHashProvider,
            IShooterEntityManager? entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _snapshotReadPort = snapshotReadPort ?? throw new ArgumentNullException(nameof(snapshotReadPort));
            _stateHashProvider = stateHashProvider ?? throw new ArgumentNullException(nameof(stateHashProvider));
            _context = entities?.SveltoContext;
        }

        public ShooterPureStateSnapshotPayload Export(
            ulong worldId,
            bool isFullBaseline = true,
            ShooterPureStateSyncSettings? settings = null,
            int baselineFrame = 0,
            uint baselineHash = 0,
            ShooterPureStateInterestScope? interestScope = null)
        {
            var activeSettings = NormalizeSettings(settings ?? ShooterPureStateSyncSettings.Default);
            var frame = _state.CurrentFrame;
            var isLowFrequencyFrame = !isFullBaseline && activeSettings.LowFrequencyIntervalFrames > 0 && frame % activeSettings.LowFrequencyIntervalFrames == 0;
            var entityBudget = isFullBaseline ? activeSettings.MaxEntityCount : activeSettings.ActiveSyncBudget;
            var maxEntities = ResolveMaxEntities(activeSettings, entityBudget, interestScope);
            var candidateCount = _context != null
                ? BuildCandidates(_context, isFullBaseline, isLowFrequencyFrame, interestScope)
                : BuildCandidatesFromSnapshot(isFullBaseline, isLowFrequencyFrame, interestScope, out frame);
            if (candidateCount > 1)
            {
                Array.Sort(_candidateBuffer, 0, candidateCount, ShooterPureStateCandidateComparer.Instance);
            }

            var selectedCount = Math.Min(maxEntities, candidateCount);
            var entities = new ShooterPureStateEntityDelta[selectedCount];
            var visibilityHints = new ShooterPureStateVisibilityHint[selectedCount];
 
            for (var i = 0; i < selectedCount; i++)
            {
                entities[i] = _candidateBuffer[i].Entity;
                visibilityHints[i] = _candidateBuffer[i].VisibilityHint;
            }

            var stateHash = _stateHashProvider.ComputeStateHash();
            return new ShooterPureStateSnapshotPayload(
                ShooterPureStateSyncCodec.CurrentVersion,
                worldId,
                frame,
                frame,
                CreateSnapshotKind(isFullBaseline, isLowFrequencyFrame),
                isFullBaseline ? frame : baselineFrame,
                isFullBaseline ? stateHash : baselineHash,
                stateHash,
                activeSettings,
                entities,
                visibilityHints);
        }

        private int BuildCandidatesFromSnapshot(
            bool isFullBaseline,
            bool isLowFrequencyFrame,
            ShooterPureStateInterestScope? interestScope,
            out int frame)
        {
            var snapshot = _snapshotReadPort.GetSnapshot();
            var players = snapshot.Players ?? Array.Empty<ShooterPlayerSnapshot>();
            var bullets = snapshot.Bullets ?? Array.Empty<ShooterBulletSnapshot>();
            frame = snapshot.Frame <= 0 ? _state.CurrentFrame : snapshot.Frame;
            return BuildCandidates(players, bullets, isFullBaseline, isLowFrequencyFrame, interestScope);
        }
 
        private int BuildCandidates(
            ShooterPlayerSnapshot[] players,
            ShooterBulletSnapshot[] bullets,
            bool isFullBaseline,
            bool isLowFrequencyFrame,
            ShooterPureStateInterestScope? interestScope)
        {
            var candidates = EnsureCandidateCapacity(players.Length + bullets.Length);
            var index = 0;
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var flags = CreatePlayerFlags(in player);
                if (isLowFrequencyFrame && !player.Alive)
                {
                    flags |= ShooterPureStateEntityFlags.LowFrequency;
                }

                var priority = _interestPolicy.CreatePlayerPriority(in player, interestScope);
                var entity = new ShooterPureStateEntityDelta(
                    player.PlayerId,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    CreateDeltaKind(isFullBaseline),
                    player.PlayerId,
                    QuantizePosition(player.X),
                    QuantizePosition(player.Y),
                    QuantizeVelocity(player.AimX),
                    QuantizeVelocity(player.AimY),
                    player.Hp,
                    player.Score,
                    0,
                    flags);
                var hint = new ShooterPureStateVisibilityHint(
                    player.PlayerId,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    flags,
                    priority);
                candidates[index++] = new ShooterPureStateCandidate(entity, hint, priority, _interestPolicy.ComputeDistanceSquared(player.X, player.Y, interestScope), player.PlayerId);
            }

            for (var i = 0; i < bullets.Length; i++)
            {
                var bullet = bullets[i];
                var flags = (byte)(ShooterPureStateEntityFlags.Alive | ShooterPureStateEntityFlags.Visible);
                if (isLowFrequencyFrame)
                {
                    flags |= ShooterPureStateEntityFlags.LowFrequency;
                }

                var priority = _interestPolicy.CreateBulletPriority(in bullet, interestScope);
                if (priority <= 0)
                {
                    flags = (byte)(flags & ~ShooterPureStateEntityFlags.Visible);
                }

                var entity = new ShooterPureStateEntityDelta(
                    bullet.BulletId,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    CreateDeltaKind(isFullBaseline),
                    bullet.OwnerPlayerId,
                    QuantizePosition(bullet.X),
                    QuantizePosition(bullet.Y),
                    QuantizeVelocity(bullet.VelocityX),
                    QuantizeVelocity(bullet.VelocityY),
                    0,
                    0,
                    bullet.RemainingFrames,
                    flags);
                var hint = new ShooterPureStateVisibilityHint(
                    bullet.BulletId,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    flags,
                    priority);
                candidates[index++] = new ShooterPureStateCandidate(entity, hint, priority, _interestPolicy.ComputeDistanceSquared(bullet.X, bullet.Y, interestScope), bullet.BulletId);
            }

            return index;
        }
 
        private int BuildCandidates(
            ISveltoWorldContext context,
            bool isFullBaseline,
            bool isLowFrequencyFrame,
            ShooterPureStateInterestScope? interestScope)
        {
            var playerCollection = context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var playerCount);
            var projectileCollection = context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var bulletCount);
            var enemyCollection = context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out var enemyIds, out var enemyCount);
            var candidates = EnsureCandidateCapacity(playerCount + bulletCount + enemyCount);
            var index = 0;

            var playerOrder = _orderBuffer.CreateSortedPlayerOrder(players, playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var player = players[playerOrder[i]];
                var flags = CreatePlayerFlags(in player);
                if (isLowFrequencyFrame && !player.Alive)
                {
                    flags |= ShooterPureStateEntityFlags.LowFrequency;
                }

                var priority = _interestPolicy.CreatePlayerPriority(in player, interestScope);
                var entity = new ShooterPureStateEntityDelta(
                    player.PlayerId,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    CreateDeltaKind(isFullBaseline),
                    player.PlayerId,
                    QuantizePosition(player.X),
                    QuantizePosition(player.Y),
                    QuantizeVelocity(player.AimX),
                    QuantizeVelocity(player.AimY),
                    player.Hp,
                    player.Score,
                    0,
                    flags);
                var hint = new ShooterPureStateVisibilityHint(
                    player.PlayerId,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    flags,
                    priority);
                candidates[index++] = new ShooterPureStateCandidate(entity, hint, priority, _interestPolicy.ComputeDistanceSquared(player.X, player.Y, interestScope), player.PlayerId);
            }

            var projectileOrder = _orderBuffer.CreateSortedProjectileOrder(bullets, bulletCount);
            for (var i = 0; i < bulletCount; i++)
            {
                var bullet = bullets[projectileOrder[i]];
                var flags = (byte)(ShooterPureStateEntityFlags.Alive | ShooterPureStateEntityFlags.Visible);
                if (isLowFrequencyFrame)
                {
                    flags |= ShooterPureStateEntityFlags.LowFrequency;
                }

                var priority = _interestPolicy.CreateBulletPriority(in bullet, interestScope);
                if (priority <= 0)
                {
                    flags = (byte)(flags & ~ShooterPureStateEntityFlags.Visible);
                }

                var entity = new ShooterPureStateEntityDelta(
                    bullet.BulletId,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    CreateDeltaKind(isFullBaseline),
                    bullet.OwnerPlayerId,
                    QuantizePosition(bullet.X),
                    QuantizePosition(bullet.Y),
                    QuantizeVelocity(bullet.VelocityX),
                    QuantizeVelocity(bullet.VelocityY),
                    0,
                    0,
                    bullet.RemainingFrames,
                    flags);
                var hint = new ShooterPureStateVisibilityHint(
                    bullet.BulletId,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    flags,
                    priority);
                candidates[index++] = new ShooterPureStateCandidate(entity, hint, priority, _interestPolicy.ComputeDistanceSquared(bullet.X, bullet.Y, interestScope), bullet.BulletId);
            }

            var enemyOrder = _orderBuffer.CreateSortedEnemyOrder(enemyIds, enemyCount);
            for (var i = 0; i < enemyCount; i++)
            {
                var enemyIndex = enemyOrder[i];
                var entityId = checked((int)enemyIds[enemyIndex]);
                var transform = enemyTransforms[enemyIndex];
                var health = enemyHealths[enemyIndex];
                var flags = (byte)ShooterPureStateEntityFlags.Visible;
                if (health.Alive != 0)
                {
                    flags |= ShooterPureStateEntityFlags.Alive;
                }

                if (isLowFrequencyFrame && health.Alive == 0)
                {
                    flags |= ShooterPureStateEntityFlags.LowFrequency;
                }

                var priority = _interestPolicy.CreateEnemyPriority(in transform, in health, interestScope);
                if (priority <= 0)
                {
                    flags = (byte)(flags & ~ShooterPureStateEntityFlags.Visible);
                }

                var entity = new ShooterPureStateEntityDelta(
                    entityId,
                    ShooterPackedEntityKinds.Enemy,
                    ShooterPureStateEntityLayers.Combat,
                    CreateDeltaKind(isFullBaseline),
                    0,
                    QuantizePosition(transform.X),
                    QuantizePosition(transform.Y),
                    QuantizeVelocity(transform.DirectionX),
                    QuantizeVelocity(transform.DirectionY),
                    health.Current,
                    0,
                    0,
                    flags);
                var hint = new ShooterPureStateVisibilityHint(
                    entityId,
                    ShooterPackedEntityKinds.Enemy,
                    ShooterPureStateEntityLayers.Combat,
                    flags,
                    priority);
                candidates[index++] = new ShooterPureStateCandidate(entity, hint, priority, _interestPolicy.ComputeDistanceSquared(transform.X, transform.Y, interestScope), entityId);
            }

            return index;
        }

        private ShooterPureStateCandidate[] EnsureCandidateCapacity(int count)
        {
            if (_candidateBuffer.Length >= count)
            {
                return _candidateBuffer;
            }

            var newCapacity = _candidateBuffer.Length == 0 ? 16 : _candidateBuffer.Length;
            while (newCapacity < count)
            {
                newCapacity = checked(newCapacity * 2);
            }

            _candidateBuffer = new ShooterPureStateCandidate[newCapacity];
            return _candidateBuffer;
        }
 
        private static int ResolveMaxEntities(ShooterPureStateSyncSettings settings, int entityBudget, ShooterPureStateInterestScope? interestScope)
        {
            var maxEntities = Math.Min(settings.MaxEntityCount, Math.Max(0, entityBudget));
            if (interestScope.HasValue && interestScope.Value.MaxEntities > 0)
            {
                maxEntities = Math.Min(maxEntities, interestScope.Value.MaxEntities);
            }

            return maxEntities;
        }

        private sealed class ShooterPureStateCandidateComparer : IComparer<ShooterPureStateCandidate>
        {
            public static readonly ShooterPureStateCandidateComparer Instance = new();

            public int Compare(ShooterPureStateCandidate left, ShooterPureStateCandidate right)
            {
                var priority = right.Priority.CompareTo(left.Priority);
                if (priority != 0)
                {
                    return priority;
                }
 
                var distance = left.DistanceSquared.CompareTo(right.DistanceSquared);
                return distance != 0 ? distance : left.TieBreaker.CompareTo(right.TieBreaker);
            }
        }

        private static int CreatePlayerPriority(in ShooterPlayerSnapshot player, ShooterPureStateInterestScope? interestScope)
        {
            var priority = player.Alive ? 100 : 10;
            if (!interestScope.HasValue)
            {
                return priority;
            }

            var scope = interestScope.Value;
            if (scope.ObserverPlayerId > 0 && player.PlayerId == scope.ObserverPlayerId)
            {
                return 1000;
            }

            return IsInsideScope(player.X, player.Y, scope) ? priority + 200 : priority;
        }

        private static int CreateBulletPriority(in ShooterBulletSnapshot bullet, ShooterPureStateInterestScope? interestScope)
        {
            if (!interestScope.HasValue)
            {
                return 80;
            }

            var scope = interestScope.Value;
            if (scope.ObserverPlayerId > 0 && bullet.OwnerPlayerId == scope.ObserverPlayerId)
            {
                return 250;
            }

            return IsInsideScope(bullet.X, bullet.Y, scope) ? 180 : 1;
        }

        private static bool IsInsideScope(float x, float y, ShooterPureStateInterestScope scope)
        {
            if (!scope.HasRadius)
            {
                return true;
            }

            return ComputeDistanceSquared(x, y, scope) <= scope.Radius * scope.Radius;
        }

        private static float ComputeDistanceSquared(float x, float y, ShooterPureStateInterestScope? interestScope)
        {
            return interestScope.HasValue ? ComputeDistanceSquared(x, y, interestScope.Value) : 0f;
        }

        private static float ComputeDistanceSquared(float x, float y, ShooterPureStateInterestScope interestScope)
        {
            var dx = x - interestScope.CenterX;
            var dy = y - interestScope.CenterY;
            return (dx * dx) + (dy * dy);
        }

        private static byte CreatePlayerFlags(in ShooterPlayerSnapshot player)
        {
            var flags = (byte)ShooterPureStateEntityFlags.Visible;
            if (player.Alive)
            {
                flags |= ShooterPureStateEntityFlags.Alive;
            }

            return flags;
        }

        private static byte CreatePlayerFlags(in ShooterSveltoPlayerComponent player)
        {
            var flags = (byte)ShooterPureStateEntityFlags.Visible;
            if (player.Alive)
            {
                flags |= ShooterPureStateEntityFlags.Alive;
            }

            return flags;
        }

        private static int CreatePlayerPriority(in ShooterSveltoPlayerComponent player, ShooterPureStateInterestScope? interestScope)
        {
            var priority = player.Alive ? 100 : 10;
            if (!interestScope.HasValue)
            {
                return priority;
            }

            var scope = interestScope.Value;
            if (scope.ObserverPlayerId > 0 && player.PlayerId == scope.ObserverPlayerId)
            {
                return 1000;
            }

            return IsInsideScope(player.X, player.Y, scope) ? priority + 200 : priority;
        }

        private static int CreateBulletPriority(in ShooterSveltoProjectileComponent bullet, ShooterPureStateInterestScope? interestScope)
        {
            if (!interestScope.HasValue)
            {
                return 80;
            }

            var scope = interestScope.Value;
            if (scope.ObserverPlayerId > 0 && bullet.OwnerPlayerId == scope.ObserverPlayerId)
            {
                return 250;
            }

            return IsInsideScope(bullet.X, bullet.Y, scope) ? 180 : 1;
        }

        private static int CreateDeltaKind(bool isFullBaseline)
        {
            return isFullBaseline ? ShooterPureStateDeltaKinds.Spawn : ShooterPureStateDeltaKinds.Update;
        }

        private static int CreateSnapshotKind(bool isFullBaseline, bool isLowFrequencyFrame)
        {
            if (isFullBaseline)
            {
                return ShooterPureStateSnapshotKinds.FullBaseline;
            }

            return isLowFrequencyFrame ? ShooterPureStateSnapshotKinds.LowFrequency : ShooterPureStateSnapshotKinds.Delta;
        }

        private static ShooterPureStateSyncSettings NormalizeSettings(ShooterPureStateSyncSettings settings)
        {
            var defaults = ShooterPureStateSyncSettings.Default;
            return new ShooterPureStateSyncSettings(
                settings.MaxEntityCount > 0 ? settings.MaxEntityCount : defaults.MaxEntityCount,
                settings.ActiveSyncBudget > 0 ? settings.ActiveSyncBudget : defaults.ActiveSyncBudget,
                settings.BaselineIntervalFrames > 0 ? settings.BaselineIntervalFrames : defaults.BaselineIntervalFrames,
                settings.DeltaIntervalFrames > 0 ? settings.DeltaIntervalFrames : defaults.DeltaIntervalFrames,
                settings.LowFrequencyIntervalFrames > 0 ? settings.LowFrequencyIntervalFrames : defaults.LowFrequencyIntervalFrames,
                settings.InterpolationDelayFrames > 0 ? settings.InterpolationDelayFrames : defaults.InterpolationDelayFrames);
        }

        private static int QuantizePosition(float value)
        {
            return (int)MathF.Round(value * PositionScale);
        }

        private static int QuantizeVelocity(float value)
        {
            return (int)MathF.Round(value * VelocityScale);
        }

        private readonly struct ShooterPureStateCandidate
        {
            public ShooterPureStateCandidate(
                ShooterPureStateEntityDelta entity,
                ShooterPureStateVisibilityHint visibilityHint,
                int priority,
                float distanceSquared,
                int tieBreaker)
            {
                Entity = entity;
                VisibilityHint = visibilityHint;
                Priority = priority;
                DistanceSquared = distanceSquared;
                TieBreaker = tieBreaker;
            }

            public ShooterPureStateEntityDelta Entity { get; }

            public ShooterPureStateVisibilityHint VisibilityHint { get; }

            public int Priority { get; }

            public float DistanceSquared { get; }

            public int TieBreaker { get; }
        }
    }
}
