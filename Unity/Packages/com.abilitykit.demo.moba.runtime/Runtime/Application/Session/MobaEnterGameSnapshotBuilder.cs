using System;
using System.Collections.Generic;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// 从玩家生成数据构建 Coordinator 首帧快照
    /// </summary>
    public static class MobaEnterGameSnapshotBuilder
    {
        public static FrameSnapshotData BuildEnterGameSnapshot(PlayerSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return new FrameSnapshotData(0, 0, SnapshotType.Full, Array.Empty<EntityState>());
            }

            var entities = new EntityState[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                var spawn = spawns[i];
                entities[i] = new EntityState
                {
                    EntityId = spawn.PlayerId,
                    X = spawn.X,
                    Y = spawn.Y,
                    Z = spawn.Z,
                    Rotation = 0,
                    Hp = 200,
                    HpMax = 200,
                    TeamId = spawn.TeamId,
                    IsDead = false
                };
            }

            return new FrameSnapshotData(0, 0, SnapshotType.Full, entities);
        }

        public static EntityState[] ToEntityStates(PlayerSpawnData[] spawns)
        {
            var snapshot = BuildEnterGameSnapshot(spawns);
            return snapshot.Entities;
        }
    }
}
