using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Pipeline;

namespace ET.Logic
{
    /// <summary>
    /// 战斗帧处理上下文
    /// 包含一帧内所有需要的数据
    /// </summary>
    public sealed class BattleFrameContext : IAbilityPipelineContext
    {
        // ============== IAbilityPipelineContext 实现 ==============

        public object AbilityInstance => null;

        public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();

        public AbilityPipelinePhaseId CurrentPhaseId { get; set; }

        public EAbilityPipelineState PipelineState { get; set; }

        public bool IsAborted { get; set; }

        public bool IsPaused { get; set; }

        public float StartTime { get; set; }

        public float ElapsedTime { get; set; }

        // ============== 数据存取方法 ==============

        public T GetData<T>(string key, T defaultValue = default)
        {
            if (SharedData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void SetData<T>(string key, T value)
        {
            SharedData[key] = value;
        }

        public bool TryGetData<T>(string key, out T value)
        {
            if (SharedData.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        public bool RemoveData(string key)
        {
            return SharedData.Remove(key);
        }

        public void ClearData()
        {
            SharedData.Clear();
        }

        public void Reset()
        {
            CurrentPhaseId = default;
            PipelineState = EAbilityPipelineState.Ready;
            IsAborted = false;
            IsPaused = false;
            StartTime = 0;
            ElapsedTime = 0;
            SharedData.Clear();
        }

        // ============== 帧数据 ==============

        /// <summary>
        /// 当前帧号
        /// </summary>
        public int CurrentFrame { get; set; }

        /// <summary>
        /// 帧间隔时间
        /// </summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// 逻辑时间（秒）
        /// </summary>
        public double LogicTimeSeconds { get; set; }

        /// <summary>
        /// 关联的 BattleDriver
        /// </summary>
        public ETMobaBattleDriver Driver { get; set; }

        // ============== 输入数据 ==============

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; set; }

        // ============== 快照数据 ==============

        /// <summary>
        /// 当前帧从 Runtime 输出端口收集到的共享快照。
        /// </summary>
        public List<FrameSnapshotData> FrameSnapshots { get; } = new List<FrameSnapshotData>();

        /// <summary>
        /// 是否已分发快照
        /// </summary>
        public bool SnapshotDispatched { get; set; }

        // ============== 便捷方法 ==============

        /// <summary>
        /// 获取关联的场景
        /// </summary>
        public Scene GetScene()
        {
            return Driver?.Scene();
        }

        /// <summary>
        /// 获取 ETUnitComponent
        /// </summary>
        public ETUnitComponent GetUnitComponent()
        {
            return GetScene()?.GetComponent<ETUnitComponent>();
        }

        /// <summary>
        /// 获取 ETInputComponent
        /// </summary>
        public ETInputComponent GetInputComponent()
        {
            return GetScene()?.GetComponent<ETInputComponent>();
        }

        /// <summary>
        /// 创建新的帧上下文
        /// </summary>
        public static BattleFrameContext Create(ETMobaBattleDriver driver, int frame, float deltaTime, double logicTime)
        {
            return new BattleFrameContext
            {
                Driver = driver,
                CurrentFrame = frame,
                DeltaTime = deltaTime,
                LogicTimeSeconds = logicTime,
                IsRunning = driver?.IsRunning ?? false,
                PipelineState = EAbilityPipelineState.Ready
            };
        }
    }
}
