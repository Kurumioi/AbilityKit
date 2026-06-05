using System;
using AbilityKit.Coordinator;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Coordinator View Event Sink Adapter
    ///
    /// Bridges AbilityKit.Coordinator.IViewEventSink to ET's event system.
    ///
    /// Design:
    /// - Receives generic frame snapshots from Coordinator
    /// - Interprets the data and converts to ET-specific events
    /// - Uses CustomPayload for MOBA-specific data (damage, projectiles, etc.)
    /// </summary>
    public sealed class ETCoordinatorViewEventSink : IViewEventSink
    {
        private readonly Scene _scene;

        public ETCoordinatorViewEventSink(Scene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        }

        public void OnEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            // Parse EntityStates and create units
            foreach (var entity in snapshot.Entities)
            {
                if (!TryGetEntityState(entity, out var state))
                {
                    continue;
                }

                var evt = new ActorSpawnEvent
                {
                    ActorId = 0,  // ET internal ID
                    EntityCode = entity.EntityId,
                    Kind = state.TeamId == 1 ? ActorKind.Character : ActorKind.Monster,
                    Name = $"Unit_{entity.EntityId}",
                    X = state.X,
                    Y = state.Z,
                    MaxHp = state.HpMax,
                    IsLocalPlayer = false,
                };

                EventSystem.Instance.Publish<Scene, ActorSpawnEvent>(_scene, evt);
            }

            Log.Debug($"[ETCoordinatorViewEventSink] OnEnterGameSnapshot: {snapshot.Entities.Length} entities");
        }

        public void OnActorTransformSnapshot(in FrameSnapshotData snapshot)
        {
            // Parse EntityStates and update positions
            foreach (var entity in snapshot.Entities)
            {
                if (!TryGetEntityState(entity, out var state))
                {
                    continue;
                }

                var evt = new ActorMoveEvent
                {
                    ActorId = entity.EntityId,
                    X = state.X,
                    Y = state.Z,
                };

                EventSystem.Instance.Publish<Scene, ActorMoveEvent>(_scene, evt);
            }
        }

        public void OnDamageEventSnapshot(in FrameSnapshotData snapshot)
        {
            // Parse custom payload for damage events
            // Format: [DamageEventData] serialized or parsed from snapshot
            if (snapshot.CustomPayload == null || snapshot.CustomPayload.Length == 0)
                return;

            // For now, parse as EntityState to extract damage info
            // In production, this should deserialize the actual damage format
            foreach (var entity in snapshot.Entities)
            {
                if (!TryGetEntityState(entity, out var state))
                {
                    continue;
                }

                if (state.Hp < state.HpMax)
                {
                    var evt = new ActorDamageEvent
                    {
                        ActorId = entity.EntityId,
                        Damage = state.HpMax - state.Hp,
                        CurrentHp = state.Hp,
                    };

                    EventSystem.Instance.Publish<Scene, ActorDamageEvent>(_scene, evt);
                }
            }
        }

        public void OnFrameSyncComplete(int frame)
        {
            Log.Debug($"[ETCoordinatorViewEventSink] Frame {frame} sync complete");
        }

        public void OnBattleStart(int frame)
        {
            var evt = new BattleStartEvent
            {
                BattleId = 0,
                PlayerId = 0,
            };

            EventSystem.Instance.Publish<Scene, BattleStartEvent>(_scene, evt);
            Log.Debug($"[ETCoordinatorViewEventSink] Battle start at frame {frame}");
        }

        public void OnBattleEnd(int frame, int winTeamId)
        {
            var evt = new BattleEndEvent
            {
                BattleId = 0,
                IsVictory = winTeamId == 1,
            };

            EventSystem.Instance.Publish<Scene, BattleEndEvent>(_scene, evt);
            Log.Debug($"[ETCoordinatorViewEventSink] Battle end at frame {frame}, winner team: {winTeamId}");
        }

        private static bool TryGetEntityState(SnapshotEntityState snapshotEntity, out EntityState state)
        {
            if (snapshotEntity.TryGetPayload(out state))
            {
                return true;
            }

            return false;
        }

        public void OnCustomEvent(string eventType, int entityId, byte[] customData)
        {
            // Handle custom events based on event type
            Log.Debug($"[ETCoordinatorViewEventSink] Custom event: type={eventType}, entity={entityId}");

            switch (eventType)
            {
                case "SkillCast":
                    // Handle skill cast
                    break;
                case "BuffApply":
                    // Handle buff apply
                    break;
                case "ProjectileSpawn":
                    // Handle projectile spawn
                    break;
                default:
                    Log.Debug($"[ETCoordinatorViewEventSink] Unknown custom event type: {eventType}");
                    break;
            }
        }
    }
}
