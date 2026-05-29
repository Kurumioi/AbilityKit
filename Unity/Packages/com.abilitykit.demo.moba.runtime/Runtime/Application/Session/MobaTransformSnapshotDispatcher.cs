using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;

/// <summary>
/// 文件名称: MobaTransformSnapshotDispatcher.cs
/// 
/// 功能描述: 从世界快照服务读取并派发表现层位置快照。
/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// 位置快照派发器，隔离快照读取、类型判断和回调派发逻辑。
    /// </summary>
    public sealed class MobaTransformSnapshotDispatcher
    {
        private readonly IWorld _world;

        /// <summary>
        /// 创建位置快照派发器。
        /// </summary>
        /// <param name="world">战斗逻辑世界</param>
        public MobaTransformSnapshotDispatcher(IWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// 尝试读取当前帧的位置快照并派发给表现层。
        /// </summary>
        /// <param name="currentFrame">当前逻辑帧</param>
        /// <param name="callback">表现层快照回调</param>
        public void TryDispatch(FrameIndex frame, Action<int, MobaActorTransformSnapshotEntry[]> callback)
        {
            if (_world?.Services?.TryResolve<IMobaBattleOutputPort>(out var output) != true)
            {
                return;
            }

            if (!output.TryGetSnapshot(frame, out WorldStateSnapshot snapshot))
            {
                return;
            }

            if (snapshot.OpCode != (int)MobaOpCode.ActorTransformSnapshot)
            {
                return;
            }

            MobaActorTransformSnapshotEntry[] entries = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
            callback?.Invoke(frame.Value, entries);
            Log.Info($"[MobaBattleDriverHost] Transform snapshot: {entries?.Length ?? 0} entities");
        }
    }
}