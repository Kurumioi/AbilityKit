using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;

/// <summary>
/// 文件名称: MobaTransformSnapshotDispatcher.cs
/// 
/// 功能描述: 从世界快照服务读取并派发表现层位置快照�?/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// 位置快照派发器，隔离快照读取、类型判断和回调派发逻辑�?    /// </summary>
    public sealed class MobaTransformSnapshotDispatcher
    {
        private readonly IWorld _world;

        /// <summary>
        /// 创建位置快照派发器�?        /// </summary>
        /// <param name="world">战斗逻辑世界</param>
        public MobaTransformSnapshotDispatcher(IWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// 尝试读取当前帧的位置快照并派发给表现层�?        /// </summary>
        /// <param name="currentFrame">当前逻辑�?/param>
        /// <param name="callback">表现层快照回�?/param>
        public void TryDispatch(FrameIndex frame, Action<int, MobaActorTransformSnapshotEntry[]> callback)
        {
            if (_world?.Services?.TryResolve<IMobaBattleRuntimePort>(out var runtime) != true || runtime == null)
            {
                throw new InvalidOperationException("MobaTransformSnapshotDispatcher requires IMobaBattleRuntimePort.");
            }

            var snapshots = new List<WorldStateSnapshot>(8);
            var count = runtime.CollectSnapshots(frame, snapshots);
            if (count <= 0)
            {
                MobaRuntimeLog.Warning(MobaRuntimeLogModule.Snapshot, MobaRuntimeLogPurpose.Validation, nameof(MobaTransformSnapshotDispatcher), $"No snapshots available. frame={frame.Value}");
                return;
            }

            WorldStateSnapshot transformSnapshot = default;
            var found = false;
            for (int i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.OpCode != AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.ActorTransform) continue;

                transformSnapshot = snapshot;
                found = true;
                break;
            }

            if (!found)
            {
                MobaRuntimeLog.Warning(MobaRuntimeLogModule.Snapshot, MobaRuntimeLogPurpose.Validation, nameof(MobaTransformSnapshotDispatcher), $"ActorTransform snapshot not available. frame={frame.Value} snapshotCount={snapshots.Count}");
                return;
            }

            MobaActorTransformSnapshotEntry[] entries = MobaActorTransformSnapshotCodec.Deserialize(transformSnapshot.Payload);
            callback?.Invoke(frame.Value, entries);
            MobaRuntimeLog.Trace(MobaRuntimeLogModule.Snapshot, MobaRuntimeLogPurpose.RuntimeTrace, nameof(MobaTransformSnapshotDispatcher), $"Transform snapshot: {entries?.Length ?? 0} entities");
        }
    }
}