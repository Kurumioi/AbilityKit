using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.World.ECS;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleSyncFeature : IGamePhaseFeature
    {
        private BattleContext _ctx;

        private EC.IECWorld _world;
        private BattleEntityLookup _lookup;
        private BattleEntityFactory _factory;
        private EC.IEntity _node;

        private int _localActorId;

        private IDisposable _subActorTransform;
        private IDisposable _subStateHash;
        private IDisposable _subActorSpawn;

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Root.TryGetRef(out _ctx);
            _world = _ctx?.EntityWorld;
            _lookup = _ctx?.EntityLookup;
            _factory = _ctx?.EntityFactory;
            _node = _ctx != null ? _ctx.EntityNode : default;

            var syncMode = _ctx != null ? _ctx.Plan.SyncMode : BattleSyncMode.Lockstep;

            if (_ctx?.FrameSnapshots != null)
            {
                switch (syncMode)
                {
                    case BattleSyncMode.SnapshotAuthority:
                    case BattleSyncMode.Lockstep:
                    case BattleSyncMode.HybridPredictReconcile:
                    default:
                        try
                        {
                            _subActorSpawn = _ctx.FrameSnapshots.Subscribe<MobaActorSpawnSnapshotEntry[]>(MobaOpCodes.Snapshot.ActorSpawn, OnActorSpawnSnapshot);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex, "[BattleSyncFeature] Failed to subscribe ActorSpawnSnapshot");
                        }
                        _subActorTransform = _ctx.FrameSnapshots.Subscribe<MobaActorTransformSnapshotEntry[]>(MobaOpCodes.Snapshot.ActorTransform, OnActorTransformSnapshot);
                        _subStateHash = _ctx.FrameSnapshots.Subscribe<MobaStateHashSnapshotPayload>(MobaOpCodes.Snapshot.StateHash, OnStateHashSnapshot);
                        break;
                }
            }
            else
            {
                Log.Warning("[BattleSyncFeature] FrameSnapshots is null");
            }

            _localActorId = 0;

            if (_ctx != null)
            {
                _ctx.RuntimeWorldId = default;
                _ctx.HasRuntimeWorldId = false;
            }
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            if (_ctx?.FrameSnapshots != null)
            {
                _subActorSpawn?.Dispose();
                _subActorTransform?.Dispose();
                _subStateHash?.Dispose();
            }

            _subActorSpawn = null;
            _subActorTransform = null;
            _subStateHash = null;

            if (_ctx != null)
            {
                _ctx.RuntimeWorldId = default;
                _ctx.HasRuntimeWorldId = false;
            }

            _ctx = null;
            _world = null;
            _lookup = null;
            _factory = null;
            _node = default;
            _localActorId = 0;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }

        private void OnStateHashSnapshot(ISnapshotEnvelope packet, MobaStateHashSnapshotPayload snap)
        {
            ApplyStateHashSnapshot(snap);

            if (_ctx != null)
            {
                _ctx.RuntimeWorldId = packet.WorldId;
                _ctx.HasRuntimeWorldId = true;
            }

            var target = _ctx?.PredictionReconcileTarget;
            if (target != null)
            {
                target.OnAuthoritativeStateHash(packet.WorldId, new FrameIndex(snap.Frame), new AbilityKit.Ability.FrameSync.Rollback.WorldStateHash(snap.Hash));
            }
        }

        private void OnActorTransformSnapshot(ISnapshotEnvelope packet, MobaActorTransformSnapshotEntry[] entries)
        {
            if (_ctx != null)
            {
                _ctx.RuntimeWorldId = packet.WorldId;
                _ctx.HasRuntimeWorldId = true;
            }
            ApplyTransformSnapshot(entries);
        }

        private void OnActorSpawnSnapshot(ISnapshotEnvelope packet, MobaActorSpawnSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return;
            }
            ApplySpawnSnapshot(entries);
        }

        private void ApplySpawnSnapshot(MobaActorSpawnSnapshotEntry[] entries)
        {
            if (_world == null || _lookup == null || _factory == null)
            {
                Log.Error("[BattleSyncFeature] ApplySpawnSnapshot aborted: EntityWorld/Lookup/Factory is null");
                return;
            }

            if (entries == null || entries.Length == 0)
            {
                return;
            }

            var dirty = _ctx != null ? _ctx.DirtyEntities : null;
            if (dirty == null)
            {
                dirty = new List<EC.IEntityId>(entries.Length);
                if (_ctx != null) _ctx.DirtyEntities = dirty;
            }
            else
            {
                dirty.Clear();
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var en = entries[i];

                if (en.NetId <= 0)
                {
                    continue;
                }

                var netId = new BattleNetId(en.NetId);

                if (_lookup.TryResolve(_world, netId, out var existing))
                {
                    continue;
                }

                EC.IEntity e;
                if (en.Kind == (int)SpawnEntityKind.Projectile)
                {
                    e = _factory.CreateProjectile(netId, ownerNetId: new BattleNetId(en.OwnerNetId), entityCode: en.Code);
                }
                else
                {
                    e = _factory.CreateCharacter(netId, entityCode: en.Code);
                }

                if (!e.TryGetRef(out BattleTransformComponent t) || t == null)
                {
                    t = new BattleTransformComponent();
                    e.WithRef(t);
                }

                t.Position = new Vector3(en.X, en.Y, en.Z);
                if (t.Forward == default) t.Forward = Vector3.forward;

                dirty.Add(e.Id);
            }
        }

        private void ApplyStateHashSnapshot(MobaStateHashSnapshotPayload p)
        {
            if (!_node.IsValid) return;

            var comp = _node.TryGetRef(out BattleStateHashSnapshotComponent existing) ? existing : null;
            if (comp == null)
            {
                comp = new BattleStateHashSnapshotComponent();
                _node.WithRef(comp);
            }

            comp.Version = p.Version;
            comp.Frame = p.Frame;
            comp.Hash = p.Hash;
        }

        private void ApplyTransformSnapshot(MobaActorTransformSnapshotEntry[] entries)
        {
            if (_world == null || _lookup == null || _factory == null) return;

            var dirty = _ctx != null ? _ctx.DirtyEntities : null;
            if (dirty == null)
            {
                dirty = new List<EC.IEntityId>(64);
                if (_ctx != null) _ctx.DirtyEntities = dirty;
            }
            else
            {
                dirty.Clear();
            }

            if (entries == null || entries.Length == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var en = entries[i];
                var netId = new BattleNetId(en.ActorId);

                if (!_lookup.TryResolve(_world, netId, out var e))
                {
                    continue;
                }

                if (!e.TryGetRef(out BattleTransformComponent t) || t == null)
                {
                    t = new BattleTransformComponent();
                    e.WithRef(t);
                }

                t.Position.x = en.X;
                t.Position.y = en.Y;
                t.Position.z = en.Z;
                if (t.Forward == default) t.Forward = Vector3.forward;

                dirty.Add(e.Id);
            }
        }
    }
}

