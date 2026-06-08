using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.View
{
    public readonly struct ShooterGatewaySnapshot
    {
        public readonly ulong WorldId;
        public readonly int Frame;
        public readonly double Timestamp;
        public readonly bool IsFullSnapshot;
        public readonly IReadOnlyList<ShooterGatewayActorSnapshot> Actors;

        public ShooterGatewaySnapshot(ulong worldId, int frame, double timestamp, bool isFullSnapshot, IReadOnlyList<ShooterGatewayActorSnapshot> actors)
        {
            WorldId = worldId;
            Frame = frame;
            Timestamp = timestamp;
            IsFullSnapshot = isFullSnapshot;
            Actors = actors ?? Array.Empty<ShooterGatewayActorSnapshot>();
        }
    }

    public readonly struct ShooterGatewayActorSnapshot
    {
        public readonly int ActorId;
        public readonly float X;
        public readonly float Y;
        public readonly float Rotation;
        public readonly float VelocityX;
        public readonly float VelocityY;
        public readonly float Hp;
        public readonly float HpMax;
        public readonly int TeamId;

        public ShooterGatewayActorSnapshot(int actorId, float x, float y, float rotation, float velocityX, float velocityY, float hp, float hpMax, int teamId)
        {
            ActorId = actorId;
            X = x;
            Y = y;
            Rotation = rotation;
            VelocityX = velocityX;
            VelocityY = velocityY;
            Hp = hp;
            HpMax = hpMax;
            TeamId = teamId;
        }
    }

    public static class ShooterGatewaySnapshotMapper
    {
        public static ShooterGatewaySnapshot ToGatewaySnapshot(in WireStateSyncSnapshotPush push)
        {
            var source = push.Actors;
            if (source == null || source.Count == 0)
            {
                return new ShooterGatewaySnapshot(
                    push.WorldId,
                    push.Frame,
                    push.Timestamp,
                    push.IsFullSnapshot,
                    Array.Empty<ShooterGatewayActorSnapshot>());
            }

            var actors = new ShooterGatewayActorSnapshot[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var actor = source[i];
                actors[i] = new ShooterGatewayActorSnapshot(
                    actor.ActorId,
                    actor.X,
                    actor.Z,
                    actor.Rotation,
                    actor.VelocityX,
                    actor.VelocityZ,
                    actor.Hp,
                    actor.HpMax,
                    actor.TeamId);
            }

            return new ShooterGatewaySnapshot(
                push.WorldId,
                push.Frame,
                push.Timestamp,
                push.IsFullSnapshot,
                actors);
        }
    }
}
