using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services.Area
{
    [WorldService(typeof(MobaAreaRuntimeService))]
    public sealed class MobaAreaRuntimeService : IService
    {
        [WorldInject(required: false)] private IProjectileService _projectiles = null;
        [WorldInject(required: false)] private IFrameTime _frameTime = null;
        [WorldInject(required: false)] private IMobaTemporaryEntityLifecycleService _lifecycle = null;
        [WorldInject(required: false)] private MobaTraceRegistry _trace = null;

        private readonly Dictionary<int, MobaAreaRuntimeInfo> _areas = new Dictionary<int, MobaAreaRuntimeInfo>();
        private readonly Dictionary<int, List<int>> _areasByOwner = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, List<int>> _areasByTemplate = new Dictionary<int, List<int>>();
        private readonly List<int> _queryBuffer = new List<int>(32);

        public int ActiveCount => _areas.Count;

        public void RegisterSpawn(
            AreaId areaId,
            int templateId,
            int ownerActorId,
            in Vec3 center,
            float radius,
            int collisionLayerMask,
            int maxTargets,
            int frame,
            long sourceContextId,
            long rootContextId,
            long ownerContextId)
        {
            if (areaId.Value <= 0) return;

            if (ownerActorId <= 0 || sourceContextId == 0L)
            {
                throw new InvalidOperationException($"Area spawn requires source context. areaId={areaId.Value} templateId={templateId} ownerActorId={ownerActorId} sourceContextId={sourceContextId}");
            }

            var info = new MobaAreaRuntimeInfo(
                areaId.Value,
                templateId,
                ownerActorId,
                center,
                radius,
                collisionLayerMask,
                maxTargets,
                frame,
                sourceContextId,
                rootContextId != 0L ? rootContextId : sourceContextId,
                ownerContextId != 0L ? ownerContextId : sourceContextId);

            if (_areas.TryGetValue(areaId.Value, out var oldInfo))
            {
                Unindex(oldInfo);
            }

            _areas[areaId.Value] = info;
            Index(_areasByOwner, ownerActorId, areaId.Value);
            Index(_areasByTemplate, templateId, areaId.Value);
            _lifecycle?.RecordSpawn(MobaTemporaryEntityKind.Area, ActiveCount, frame);
        }

        public bool Unregister(AreaId areaId)
        {
            if (areaId.Value <= 0) return false;
            if (!_areas.TryGetValue(areaId.Value, out var info)) return false;

            _areas.Remove(areaId.Value);
            Unindex(info);
            EndAreaTrace(in info, TraceLifecycleReason.Completed);
            _lifecycle?.RecordDespawn(MobaTemporaryEntityKind.Area, ActiveCount, CurrentFrame);
            return true;
        }

        public bool TryGetArea(int areaId, out MobaAreaRuntimeInfo info)
        {
            if (areaId <= 0)
            {
                info = default;
                return false;
            }

            return _areas.TryGetValue(areaId, out info);
        }

        public bool TryGetAreas(List<MobaAreaRuntimeInfo> results, int ownerActorId = 0, int templateId = 0)
        {
            if (results == null) return false;
            results.Clear();

            _queryBuffer.Clear();
            CollectAreaIds(_queryBuffer, ownerActorId, templateId);
            for (var i = 0; i < _queryBuffer.Count; i++)
            {
                if (_areas.TryGetValue(_queryBuffer[i], out var info))
                {
                    results.Add(info);
                }
            }

            _queryBuffer.Clear();
            return results.Count > 0;
        }

        public bool DespawnArea(int areaId)
        {
            if (areaId <= 0) return false;
            if (!_areas.ContainsKey(areaId)) return false;
            if (_projectiles == null) return false;

            return _projectiles.DespawnArea(new AreaId(areaId), CurrentFrame);
        }

        public int DespawnAreas(int ownerActorId, int templateId, bool removeAll)
        {
            _queryBuffer.Clear();
            CollectAreaIds(_queryBuffer, ownerActorId, templateId);

            var removed = 0;
            for (var i = 0; i < _queryBuffer.Count; i++)
            {
                if (DespawnArea(_queryBuffer[i])) removed++;
                if (removed > 0 && !removeAll) break;
            }

            _queryBuffer.Clear();
            return removed;
        }

        public void Dispose()
        {
            _areas.Clear();
            _areasByOwner.Clear();
            _areasByTemplate.Clear();
            _queryBuffer.Clear();
            _lifecycle?.SetActive(MobaTemporaryEntityKind.Area, 0, CurrentFrame);
        }

        private int CurrentFrame
        {
            get
            {
                if (_frameTime != null) return _frameTime.Frame.Value;
                throw new InvalidOperationException("MobaAreaRuntimeService requires IFrameTime for current frame.");
            }
        }

        private void CollectAreaIds(List<int> results, int ownerActorId, int templateId)
        {
            if (results == null) return;

            if (ownerActorId > 0 && templateId > 0)
            {
                if (!_areasByOwner.TryGetValue(ownerActorId, out var ownerList) || ownerList == null) return;
                for (var i = 0; i < ownerList.Count; i++)
                {
                    var areaId = ownerList[i];
                    if (_areas.TryGetValue(areaId, out var info) && info.TemplateId == templateId)
                    {
                        results.Add(areaId);
                    }
                }

                return;
            }

            if (ownerActorId > 0)
            {
                CopyIndexed(_areasByOwner, ownerActorId, results);
                return;
            }

            if (templateId > 0)
            {
                CopyIndexed(_areasByTemplate, templateId, results);
                return;
            }

            foreach (var kv in _areas)
            {
                results.Add(kv.Key);
            }
        }

        private void Unindex(MobaAreaRuntimeInfo info)
        {
            RemoveIndexed(_areasByOwner, info.OwnerActorId, info.AreaId);
            RemoveIndexed(_areasByTemplate, info.TemplateId, info.AreaId);
        }

        private void EndAreaTrace(in MobaAreaRuntimeInfo info, TraceLifecycleReason reason)
        {
            if (_trace == null) return;
            if (info.SourceContextId == 0L) return;
            _trace.EndContext(info.SourceContextId, reason);
        }

        private static void Index(Dictionary<int, List<int>> index, int key, int areaId)
        {
            if (key <= 0 || areaId <= 0) return;
            if (!index.TryGetValue(key, out var list) || list == null)
            {
                list = new List<int>(8);
                index[key] = list;
            }

            if (!list.Contains(areaId)) list.Add(areaId);
        }

        private static void CopyIndexed(Dictionary<int, List<int>> index, int key, List<int> results)
        {
            if (key <= 0) return;
            if (!index.TryGetValue(key, out var list) || list == null) return;
            for (var i = 0; i < list.Count; i++)
            {
                results.Add(list[i]);
            }
        }

        private static void RemoveIndexed(Dictionary<int, List<int>> index, int key, int areaId)
        {
            if (key <= 0 || areaId <= 0) return;
            if (!index.TryGetValue(key, out var list) || list == null) return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == areaId)
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            if (list.Count == 0)
            {
                index.Remove(key);
            }
        }
    }

    public readonly struct MobaAreaRuntimeInfo
    {
        public readonly int AreaId;
        public readonly int TemplateId;
        public readonly int OwnerActorId;
        public readonly Vec3 Center;
        public readonly float Radius;
        public readonly int CollisionLayerMask;
        public readonly int MaxTargets;
        public readonly int SpawnFrame;
        public readonly long SourceContextId;
        public readonly long RootContextId;
        public readonly long OwnerContextId;

        public MobaAreaRuntimeInfo(
            int areaId,
            int templateId,
            int ownerActorId,
            in Vec3 center,
            float radius,
            int collisionLayerMask,
            int maxTargets,
            int spawnFrame,
            long sourceContextId,
            long rootContextId,
            long ownerContextId)
        {
            AreaId = areaId;
            TemplateId = templateId;
            OwnerActorId = ownerActorId;
            Center = center;
            Radius = radius;
            CollisionLayerMask = collisionLayerMask;
            MaxTargets = maxTargets;
            SpawnFrame = spawnFrame;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
        }
    }
}
