using System;
using System.Collections.Generic;
using AbilityKit.Core.Math;

namespace AbilityKit.Network.Runtime.LagCompensation
{
    /// <summary>
    /// Configuration for server-side rewind lag compensation.
    /// </summary>
    public readonly struct ServerRewindLagCompensationConfig
    {
        public readonly int MaxHistoryFrames;
        public readonly int MaxRewindFrames;
        public readonly float HitRadiusPadding;

        public ServerRewindLagCompensationConfig(int maxHistoryFrames = 120, int maxRewindFrames = 30, float hitRadiusPadding = 0f)
        {
            if (maxHistoryFrames <= 0) throw new ArgumentOutOfRangeException(nameof(maxHistoryFrames));
            if (maxRewindFrames < 0) throw new ArgumentOutOfRangeException(nameof(maxRewindFrames));
            if (hitRadiusPadding < 0f) throw new ArgumentOutOfRangeException(nameof(hitRadiusPadding));

            MaxHistoryFrames = maxHistoryFrames;
            MaxRewindFrames = maxRewindFrames;
            HitRadiusPadding = hitRadiusPadding;
        }

        public static ServerRewindLagCompensationConfig Default => new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 120,
            maxRewindFrames: 30,
            hitRadiusPadding: 0f);
    }

    /// <summary>
    /// Sphere hitbox snapshot for one server-authoritative entity at a captured frame.
    /// </summary>
    public readonly struct LagCompensatedEntitySnapshot
    {
        public readonly int EntityId;
        public readonly Vec3 Position;
        public readonly float Radius;
        public readonly int LayerMask;
        public readonly bool IsAlive;

        public LagCompensatedEntitySnapshot(int entityId, in Vec3 position, float radius, int layerMask = -1, bool isAlive = true)
        {
            if (radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius));

            EntityId = entityId;
            Position = position;
            Radius = radius;
            LayerMask = layerMask;
            IsAlive = isAlive;
        }
    }

    /// <summary>
    /// Input query for evaluating a client-reported hit against rewound server history.
    /// </summary>
    public readonly struct LagCompensationQuery
    {
        public readonly int ShooterEntityId;
        public readonly Vec3 Origin;
        public readonly Vec3 Direction;
        public readonly float MaxDistance;
        public readonly int TargetLayerMask;
        public readonly int RewindFrame;
        public readonly int ServerReceiveFrame;

        public LagCompensationQuery(
            int shooterEntityId,
            in Vec3 origin,
            in Vec3 direction,
            float maxDistance,
            int targetLayerMask,
            int rewindFrame,
            int serverReceiveFrame)
        {
            ShooterEntityId = shooterEntityId;
            Origin = origin;
            Direction = direction;
            MaxDistance = maxDistance;
            TargetLayerMask = targetLayerMask;
            RewindFrame = rewindFrame;
            ServerReceiveFrame = serverReceiveFrame;
        }
    }

    public enum LagCompensationResultReason
    {
        None = 0,
        Hit = 1,
        Miss = 2,
        InvalidQuery = 3,
        RewindWindowExceeded = 4,
        HistoryUnavailable = 5
    }

    /// <summary>
    /// Deterministic result of a server-side rewound hit evaluation.
    /// </summary>
    public readonly struct LagCompensationHitResult
    {
        public readonly bool Accepted;
        public readonly LagCompensationResultReason Reason;
        public readonly int RequestedFrame;
        public readonly int EvaluatedFrame;
        public readonly int HitEntityId;
        public readonly float Distance;
        public readonly Vec3 Point;

        public LagCompensationHitResult(
            bool accepted,
            LagCompensationResultReason reason,
            int requestedFrame,
            int evaluatedFrame,
            int hitEntityId,
            float distance,
            in Vec3 point)
        {
            Accepted = accepted;
            Reason = reason;
            RequestedFrame = requestedFrame;
            EvaluatedFrame = evaluatedFrame;
            HitEntityId = hitEntityId;
            Distance = distance;
            Point = point;
        }

        public static LagCompensationHitResult Reject(LagCompensationResultReason reason, int requestedFrame, int evaluatedFrame = -1)
        {
            return new LagCompensationHitResult(false, reason, requestedFrame, evaluatedFrame, 0, 0f, Vec3.Zero);
        }
    }

    /// <summary>
    /// Framework-level server rewind helper for favor-the-shooter hit validation.
    /// Demos provide authoritative entity snapshots; this service owns history and deterministic ray-sphere evaluation.
    /// </summary>
    public sealed class ServerRewindLagCompensationService
    {
        private readonly ServerRewindLagCompensationConfig _config;
        private readonly List<FrameSnapshot> _history = new List<FrameSnapshot>(128);

        public ServerRewindLagCompensationService()
            : this(ServerRewindLagCompensationConfig.Default)
        {
        }

        public ServerRewindLagCompensationService(ServerRewindLagCompensationConfig config)
        {
            _config = config;
        }

        public NetworkSyncModel SyncModel => NetworkSyncModel.ServerRewindLagCompensation;

        public int CapturedFrameCount => _history.Count;

        public int OldestFrame => _history.Count == 0 ? -1 : _history[0].Frame;

        public int LatestFrame => _history.Count == 0 ? -1 : _history[_history.Count - 1].Frame;

        public void RecordFrame(int frame, IReadOnlyList<LagCompensatedEntitySnapshot> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var copy = new LagCompensatedEntitySnapshot[entities.Count];
            for (var i = 0; i < entities.Count; i++) copy[i] = entities[i];

            var existing = FindExactFrameIndex(frame);
            if (existing >= 0)
            {
                _history[existing] = new FrameSnapshot(frame, copy);
            }
            else
            {
                InsertSorted(new FrameSnapshot(frame, copy));
            }

            TrimHistory();
        }

        public bool TryEvaluateHit(in LagCompensationQuery query, out LagCompensationHitResult result)
        {
            if (query.Direction.SqrMagnitude <= 0f || query.MaxDistance <= 0f)
            {
                result = LagCompensationHitResult.Reject(LagCompensationResultReason.InvalidQuery, query.RewindFrame);
                return false;
            }

            if (query.ServerReceiveFrame - query.RewindFrame > _config.MaxRewindFrames)
            {
                result = LagCompensationHitResult.Reject(LagCompensationResultReason.RewindWindowExceeded, query.RewindFrame);
                return false;
            }

            var frameIndex = FindFloorFrameIndex(query.RewindFrame);
            if (frameIndex < 0)
            {
                result = LagCompensationHitResult.Reject(LagCompensationResultReason.HistoryUnavailable, query.RewindFrame);
                return false;
            }

            var frame = _history[frameIndex];
            var direction = query.Direction.Normalized;
            var bestDistance = float.PositiveInfinity;
            var bestEntity = 0;
            var bestPoint = Vec3.Zero;

            for (var i = 0; i < frame.Entities.Length; i++)
            {
                var entity = frame.Entities[i];
                if (!entity.IsAlive) continue;
                if (entity.EntityId == query.ShooterEntityId) continue;
                if ((entity.LayerMask & query.TargetLayerMask) == 0) continue;

                var radius = entity.Radius + _config.HitRadiusPadding;
                if (!TryRaycastSphere(in query.Origin, in direction, query.MaxDistance, in entity.Position, radius, out var distance)) continue;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                bestEntity = entity.EntityId;
                bestPoint = query.Origin + direction * distance;
            }

            if (bestEntity == 0)
            {
                result = new LagCompensationHitResult(
                    accepted: false,
                    reason: LagCompensationResultReason.Miss,
                    requestedFrame: query.RewindFrame,
                    evaluatedFrame: frame.Frame,
                    hitEntityId: 0,
                    distance: 0f,
                    point: Vec3.Zero);
                return false;
            }

            result = new LagCompensationHitResult(
                accepted: true,
                reason: LagCompensationResultReason.Hit,
                requestedFrame: query.RewindFrame,
                evaluatedFrame: frame.Frame,
                hitEntityId: bestEntity,
                distance: bestDistance,
                point: bestPoint);
            return true;
        }

        public void Clear()
        {
            _history.Clear();
        }

        private int FindExactFrameIndex(int frame)
        {
            for (var i = 0; i < _history.Count; i++)
            {
                if (_history[i].Frame == frame) return i;
            }

            return -1;
        }

        private int FindFloorFrameIndex(int frame)
        {
            var best = -1;
            for (var i = 0; i < _history.Count; i++)
            {
                if (_history[i].Frame > frame) break;
                best = i;
            }

            return best;
        }

        private void InsertSorted(in FrameSnapshot snapshot)
        {
            var insertAt = _history.Count;
            for (var i = 0; i < _history.Count; i++)
            {
                if (_history[i].Frame <= snapshot.Frame) continue;
                insertAt = i;
                break;
            }

            _history.Insert(insertAt, snapshot);
        }

        private void TrimHistory()
        {
            while (_history.Count > _config.MaxHistoryFrames)
            {
                _history.RemoveAt(0);
            }
        }

        private static bool TryRaycastSphere(
            in Vec3 origin,
            in Vec3 direction,
            float maxDistance,
            in Vec3 center,
            float radius,
            out float distance)
        {
            var toCenter = center - origin;
            var projection = Vec3.Dot(in toCenter, in direction);
            var radiusSqr = radius * radius;
            var closestSqr = toCenter.SqrMagnitude - projection * projection;

            if (closestSqr > radiusSqr)
            {
                distance = 0f;
                return false;
            }

            var offset = (float)Math.Sqrt(Math.Max(0f, radiusSqr - closestSqr));
            var entry = projection - offset;
            var exit = projection + offset;
            distance = entry >= 0f ? entry : exit;

            return distance >= 0f && distance <= maxDistance;
        }

        private readonly struct FrameSnapshot
        {
            public readonly int Frame;
            public readonly LagCompensatedEntitySnapshot[] Entities;

            public FrameSnapshot(int frame, LagCompensatedEntitySnapshot[] entities)
            {
                Frame = frame;
                Entities = entities;
            }
        }
    }
}
