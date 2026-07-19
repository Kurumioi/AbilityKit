using System;
using System.Collections.Generic;
using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.Collision
{
    /// <summary>
    /// 基于网格空间划分的碰撞世界
    ///
    /// 使用网格分区加速碰撞查询，支持：
    /// - 层过滤（LayerFilter）
    /// - 层关系矩阵（CollisionLayerMatrix）
    /// </summary>
    public sealed class GridCollisionWorld : ICollisionWorld, ICollisionLayerRelation
    {
        private readonly GridBroadphase _broadphase;
        private readonly int _initialCapacity;
        private Entry[] _entries;
        private int _nextId = 1;
        private readonly CollisionLayerMatrix _layerMatrix;

        private struct Entry
        {
            public Transform3 Transform;
            public ColliderShape LocalShape;
            public int LayerId;
            public bool Alive;
        }

        public GridCollisionWorld(float cellSize = 4f, int initialCapacity = 64)
        {
            _broadphase = new GridBroadphase(cellSize, initialCapacity);
            _initialCapacity = initialCapacity;
            _entries = new Entry[initialCapacity];
            _layerMatrix = new CollisionLayerMatrix();
        }

        // ============ ICollisionLayerRelation 实现 ============

        public void SetRelation(int layerA, int layerB, CollisionResponse response)
        {
            _layerMatrix.SetRelation(layerA, layerB, response);
        }

        public CollisionResponse GetRelation(int layerA, int layerB)
        {
            return _layerMatrix.GetRelation(layerA, layerB);
        }

        public bool ShouldDetect(int layerA, int layerB)
        {
            return _layerMatrix.ShouldDetect(layerA, layerB);
        }

        // ============ ICollisionWorld 实现 ============

        public ColliderId Add(in Transform3 transform, in ColliderShape localShape, int layerId)
        {
            var id = new ColliderId(_nextId++);

            var worldShape = ToWorldAabb(in transform, in localShape);

            if (_nextId > _entries.Length)
            {
                var newEntries = new Entry[_entries.Length * 2];
                Array.Copy(_entries, newEntries, _entries.Length);
                _entries = newEntries;
            }

            _entries[id.Value - 1] = new Entry
            {
                Transform = transform,
                LocalShape = localShape,
                LayerId = layerId,
                Alive = true
            };

            _broadphase.Update(id.Value, in worldShape);
            return id;
        }

        public bool Remove(ColliderId id)
        {
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            if (!_entries[idx].Alive) return false;

            _entries[idx].Alive = false;
            _broadphase.Remove(id.Value);
            return true;
        }

        public bool UpdateTransform(ColliderId id, in Transform3 transform)
        {
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            if (!_entries[idx].Alive) return false;

            _entries[idx].Transform = transform;
            var worldShape = ToWorldAabb(in transform, in _entries[idx].LocalShape);
            _broadphase.Update(id.Value, in worldShape);
            return true;
        }

        public bool UpdateShape(ColliderId id, in ColliderShape localShape)
        {
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            if (!_entries[idx].Alive) return false;

            _entries[idx].LocalShape = localShape;
            var worldShape = ToWorldAabb(in _entries[idx].Transform, in localShape);
            _broadphase.Update(id.Value, in worldShape);
            return true;
        }

        public bool UpdateLayer(ColliderId id, int layerId)
        {
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            if (!_entries[idx].Alive) return false;

            _entries[idx].LayerId = layerId;
            return true;
        }

        public bool Update(ColliderId id, in Transform3 transform, in ColliderShape localShape)
        {
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            if (!_entries[idx].Alive) return false;

            _entries[idx].Transform = transform;
            _entries[idx].LocalShape = localShape;
            var worldShape = ToWorldAabb(in transform, in localShape);
            _broadphase.Update(id.Value, in worldShape);
            return true;
        }

        public bool GetLayer(ColliderId id, out int layerId)
        {
            layerId = 0;
            var idx = id.Value - 1;
            if (idx < 0 || idx >= _entries.Length) return false;
            var e = _entries[idx];
            if (!e.Alive) return false;
            layerId = e.LayerId;
            return true;
        }

        public bool ShouldCollide(int layerA, int layerB)
        {
            return _layerMatrix.ShouldDetect(layerA, layerB);
        }

        public bool Raycast(in Ray3 ray, float maxDistance, in LayerFilter filter, out RaycastHit hit)
        {
            var candidateIds = new int[_initialCapacity];
            var candidates = CollectCandidates(in ray, maxDistance, candidateIds);

            var best = float.PositiveInfinity;
            var bestId = default(ColliderId);
            var bestNormal = Vec3.Zero;

            for (var i = 0; i < candidates; i++)
            {
                var idx = candidateIds[i] - 1;
                if (idx < 0 || idx >= _entries.Length || !_entries[idx].Alive) continue;
                if (!filter.IsLayerIncluded(_entries[idx].LayerId)) continue;

                var worldShape = ToWorldShape(in _entries[idx].Transform, in _entries[idx].LocalShape);
                if (!CollisionQueries.Raycast(ray, worldShape, out var d, out var n)) continue;
                if (d < 0f || d > maxDistance) continue;

                if (d < best)
                {
                    best = d;
                    bestId = new ColliderId(candidateIds[i]);
                    bestNormal = n;
                }
            }

            if (best < float.PositiveInfinity)
            {
                hit = new RaycastHit(bestId, best, ray.GetPoint(best), bestNormal);
                return true;
            }

            hit = default;
            return false;
        }

        public int OverlapSphere(in Sphere sphere, in LayerFilter filter, List<ColliderId> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var queryAabb = new Aabb(
                sphere.Center - new Vec3(sphere.Radius, sphere.Radius, sphere.Radius),
                sphere.Center + new Vec3(sphere.Radius, sphere.Radius, sphere.Radius));

            var candidates = new int[_initialCapacity];
            var count = _broadphase.Query(in queryAabb, candidates, candidates.Length);

            var resultCount = 0;
            for (var i = 0; i < count; i++)
            {
                var idx = candidates[i] - 1;
                if (idx < 0 || idx >= _entries.Length || !_entries[idx].Alive) continue;
                if (!filter.IsLayerIncluded(_entries[idx].LayerId)) continue;

                var worldShape = ToWorldShape(in _entries[idx].Transform, in _entries[idx].LocalShape);
                if (!CollisionQueries.Overlap(sphere, worldShape)) continue;

                results.Add(new ColliderId(candidates[i]));
                resultCount++;
            }

            return resultCount;
        }

        private int CollectCandidates(in Ray3 ray, float maxDistance, int[] results)
        {
            var far = ray.GetPoint(maxDistance);
            var queryAabb = new Aabb(
                new Vec3(
                    System.Math.Min(ray.Origin.X, far.X),
                    System.Math.Min(ray.Origin.Y, far.Y),
                    System.Math.Min(ray.Origin.Z, far.Z)),
                new Vec3(
                    System.Math.Max(ray.Origin.X, far.X),
                    System.Math.Max(ray.Origin.Y, far.Y),
                    System.Math.Max(ray.Origin.Z, far.Z)));

            return _broadphase.Query(in queryAabb, results, results.Length);
        }

        private static Aabb ToWorldAabb(in Transform3 t, in ColliderShape local)
        {
            var worldShape = ToWorldShape(in t, in local);
            switch (worldShape.Type)
            {
                case ColliderShapeType.Sphere:
                    var r = worldShape.Sphere.Radius;
                    var c = worldShape.Sphere.Center;
                    return new Aabb(c - new Vec3(r, r, r), c + new Vec3(r, r, r));
                case ColliderShapeType.Aabb:
                    return worldShape.Aabb;
                case ColliderShapeType.Capsule:
                    var min = Vec3.Min(worldShape.Capsule.A, worldShape.Capsule.B);
                    var max = Vec3.Max(worldShape.Capsule.A, worldShape.Capsule.B);
                    return new Aabb(min - new Vec3(worldShape.Capsule.Radius, worldShape.Capsule.Radius, worldShape.Capsule.Radius),
                        max + new Vec3(worldShape.Capsule.Radius, worldShape.Capsule.Radius, worldShape.Capsule.Radius));
                default:
                    return new Aabb(Vec3.Zero, Vec3.Zero);
            }
        }

        private static ColliderShape ToWorldShape(in Transform3 t, in ColliderShape local)
        {
            switch (local.Type)
            {
                case ColliderShapeType.Sphere:
                {
                    var c = t.TransformPoint(local.Sphere.Center);
                    var r = local.Sphere.Radius * MaxAbsComponent(t.Scale);
                    return ColliderShape.CreateSphere(c, r);
                }
                case ColliderShapeType.Capsule:
                {
                    var a = t.TransformPoint(local.Capsule.A);
                    var b = t.TransformPoint(local.Capsule.B);
                    var r = local.Capsule.Radius * MaxAbsComponent(t.Scale);
                    return ColliderShape.CreateCapsule(a, b, r);
                }
                case ColliderShapeType.Aabb:
                {
                    var aabb = ToWorldAabbConservative(in t, in local.Aabb);
                    return ColliderShape.CreateAabb(aabb.Min, aabb.Max);
                }
                case ColliderShapeType.OBB:
                {
                    var c = t.TransformPoint(local.Obb.Center);
                    var rot = t.Rotation * local.Obb.Rotation;
                    var ext = local.Obb.HalfExtents * MaxAbsComponent(t.Scale);
                    return ColliderShape.CreateObb(c, rot, ext);
                }
                default:
                    return local;
            }
        }

        private static float MaxAbsComponent(in Vec3 v)
        {
            var ax = System.Math.Abs(v.X);
            var ay = System.Math.Abs(v.Y);
            var az = System.Math.Abs(v.Z);
            return System.Math.Max(ax, System.Math.Max(ay, az));
        }

        private static Aabb ToWorldAabbConservative(in Transform3 t, in Aabb local)
        {
            var min = local.Min;
            var max = local.Max;

            var c0 = t.TransformPoint(new Vec3(min.X, min.Y, min.Z));
            var c1 = t.TransformPoint(new Vec3(min.X, min.Y, max.Z));
            var c2 = t.TransformPoint(new Vec3(min.X, max.Y, min.Z));
            var c3 = t.TransformPoint(new Vec3(min.X, max.Y, max.Z));
            var c4 = t.TransformPoint(new Vec3(max.X, min.Y, min.Z));
            var c5 = t.TransformPoint(new Vec3(max.X, min.Y, max.Z));
            var c6 = t.TransformPoint(new Vec3(max.X, max.Y, min.Z));
            var c7 = t.TransformPoint(new Vec3(max.X, max.Y, max.Z));

            var wMin = Vec3.Min(Vec3.Min(Vec3.Min(c0, c1), Vec3.Min(c2, c3)), Vec3.Min(Vec3.Min(c4, c5), Vec3.Min(c6, c7)));
            var wMax = Vec3.Max(Vec3.Max(Vec3.Max(c0, c1), Vec3.Max(c2, c3)), Vec3.Max(Vec3.Max(c4, c5), Vec3.Max(c6, c7)));

            return new Aabb(wMin, wMax);
        }
    }
}
