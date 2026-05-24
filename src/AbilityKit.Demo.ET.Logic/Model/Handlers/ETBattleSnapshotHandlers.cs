using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 快照处理器上下文
    /// 封装视图事件接收器，用于分发快照到视图层
    /// </summary>
    public sealed class SnapshotHandlerContext
    {
        public IBattleViewEventSink ViewSink { get; set; }
        public int CurrentFrame { get; set; }

        public SnapshotHandlerContext(IBattleViewEventSink viewSink)
        {
            ViewSink = viewSink;
            CurrentFrame = 0;
        }
    }

    /// <summary>
    /// 快照处理器基类
    /// 提供通用的快照处理逻辑
    /// </summary>
    public abstract class BaseSnapshotHandler
    {
        protected SnapshotHandlerContext Context { get; private set; }

        public void SetContext(SnapshotHandlerContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            OnContextSet();
        }

        protected virtual void OnContextSet() { }

        protected void DispatchEnterGame(int frame, in EnterGameData data)
        {
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, enterGame: data);
            Context?.ViewSink?.OnEnterGameSnapshot(in snapshot);
        }

        protected void DispatchActorTransform(int frame, List<ActorTransformData> transforms)
        {
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Delta, actorTransforms: transforms);
            Context?.ViewSink?.OnActorTransformSnapshot(in snapshot);
        }

        protected void DispatchDamageEvent(int frame, List<DamageEventData> events)
        {
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Delta, damageEvents: events);
            Context?.ViewSink?.OnDamageEventSnapshot(in snapshot);
        }

        protected void DispatchActorSpawn(int frame, List<ActorSpawnData> spawns)
        {
            // Note: OnActorSpawnSnapshot is not part of IBattleViewEventSink
            // Actor spawn is handled through EnterGameSnapshot or ActorTransformSnapshot
            Log.Debug($"[BaseSnapshotHandler] DispatchActorSpawn: {spawns.Count} actors at frame {frame}");
        }
    }

    /// <summary>
    /// EnterGame 快照处理器
    /// </summary>
    public sealed class EnterGameSnapshotHandler : BaseSnapshotHandler
    {
        public void Handle(int frame, EnterGameData data)
        {
            DispatchEnterGame(frame, in data);
        }
    }

    /// <summary>
    /// Actor 变换快照处理器
    /// </summary>
    public sealed class ActorTransformSnapshotHandler : BaseSnapshotHandler
    {
        public void Handle(int frame, ActorTransformData[] data)
        {
            var transforms = data != null ? new List<ActorTransformData>(data) : new List<ActorTransformData>();
            DispatchActorTransform(frame, transforms);
        }
    }

    /// <summary>
    /// 伤害事件快照处理器
    /// </summary>
    public sealed class DamageEventSnapshotHandler : BaseSnapshotHandler
    {
        public void Handle(int frame, DamageEventData[] data)
        {
            var events = data != null ? new List<DamageEventData>(data) : new List<DamageEventData>();
            DispatchDamageEvent(frame, events);
        }
    }

    /// <summary>
    /// Actor 生成快照处理器
    /// </summary>
    public sealed class ActorSpawnSnapshotHandler : BaseSnapshotHandler
    {
        public void Handle(int frame, ActorSpawnData[] data)
        {
            var spawns = data != null ? new List<ActorSpawnData>(data) : new List<ActorSpawnData>();
            DispatchActorSpawn(frame, spawns);
        }
    }

    /// <summary>
    /// 快照处理器管理器
    /// 统一管理所有快照处理器
    /// </summary>
    public sealed class ETBattleSnapshotHandlers
    {
        public EnterGameSnapshotHandler EnterGame { get; }
        public ActorTransformSnapshotHandler ActorTransform { get; }
        public DamageEventSnapshotHandler DamageEvent { get; }
        public ActorSpawnSnapshotHandler ActorSpawn { get; }

        public ETBattleSnapshotHandlers()
        {
            EnterGame = new EnterGameSnapshotHandler();
            ActorTransform = new ActorTransformSnapshotHandler();
            DamageEvent = new DamageEventSnapshotHandler();
            ActorSpawn = new ActorSpawnSnapshotHandler();
        }

        /// <summary>
        /// 初始化所有处理器上下文
        /// </summary>
        public void Initialize(SnapshotHandlerContext context)
        {
            EnterGame.SetContext(context);
            ActorTransform.SetContext(context);
            DamageEvent.SetContext(context);
            ActorSpawn.SetContext(context);

            Log.Info("[ETBattleSnapshotHandlers] All handlers initialized");
        }

        /// <summary>
        /// 根据 OpCode 分发到对应处理器
        /// </summary>
        public bool Dispatch(int opCode, int frame, object data)
        {
            switch (opCode)
            {
                case (int)MobaOpCode.EnterGameSnapshot:
                    if (data is EnterGameData enterGameData)
                    {
                        EnterGame.Handle(frame, enterGameData);
                        return true;
                    }
                    break;

                case (int)MobaOpCode.ActorTransformSnapshot:
                    if (data is ActorTransformData[] transformData)
                    {
                        ActorTransform.Handle(frame, transformData);
                        return true;
                    }
                    break;

                case (int)MobaOpCode.DamageEventSnapshot:
                    if (data is DamageEventData[] damageData)
                    {
                        DamageEvent.Handle(frame, damageData);
                        return true;
                    }
                    break;

                case (int)MobaOpCode.ActorSpawnSnapshot:
                    if (data is ActorSpawnData[] spawnData)
                    {
                        ActorSpawn.Handle(frame, spawnData);
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
