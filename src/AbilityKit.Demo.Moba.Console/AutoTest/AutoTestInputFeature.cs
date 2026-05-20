using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Input;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// 自动测试输入特征模块
    /// 实现 IInputFeature 接口，作为 ConsoleInputFeature 的可切换替代
    ///
    /// 职责：
    /// - 只向 ConsoleBattleContext 提供输入数据
    /// - 不持有 EventBus
    /// - 不订阅任何事件
    /// - 提供预设的输入序列
    /// </summary>
    public sealed class AutoTestInputFeature : IInputFeature, IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private bool _initialized;
        private bool _started;
        private int _currentStep;
        private readonly List<InputStep> _steps = new();
        private readonly Random _random = new Random();

        public int LocalActorId => _ctx?.LocalActorId ?? 0;

        /// <summary>
        /// 注册输入步骤
        /// </summary>
        public void RegisterStep(InputStep step)
        {
            _steps.Add(step);
        }

        /// <summary>
        /// 清除所有步骤
        /// </summary>
        public void ClearSteps()
        {
            _steps.Clear();
        }

        /// <summary>
        /// 开始自动测试
        /// </summary>
        public void Start()
        {
            _started = true;
            _currentStep = 0;
            Log.System("[AutoTest] Auto test started");
        }

        /// <summary>
        /// 停止自动测试
        /// </summary>
        public void Stop()
        {
            _started = false;
            Log.System("[AutoTest] Auto test stopped");
        }

        /// <summary>
        /// 重置自动测试
        /// </summary>
        public void Reset()
        {
            _started = false;
            _currentStep = 0;
            ClearSteps();
            Log.System("[AutoTest] Auto test reset");
        }

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _initialized = true;
            Log.Trace($"[AutoTest] Attached - PlayerId: {_ctx.LocalActorId}");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            _ctx = null;
            _initialized = false;
            Stop();
            Log.Trace("[AutoTest] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            if (!_initialized || _ctx == null || _ctx.State != BattleState.InMatch)
                return;

            if (!_started || _currentStep >= _steps.Count)
                return;

            // 执行当前步骤
            var step = _steps[_currentStep];
            step.TickCount++;

            // 根据步骤类型设置输入
            switch (step.Type)
            {
                case InputStepType.Move:
                    SetMove(step.Dx, step.Dz);
                    break;
                case InputStepType.Skill:
                    ClickSkill(step.Slot);
                    break;
                case InputStepType.Wait:
                    // 等待，不做任何操作
                    break;
                case InputStepType.Idle:
                    SetMove(0, 0);
                    break;
            }

            // 检查步骤是否完成
            if (step.TickCount >= step.DurationTicks)
            {
                _currentStep++;
                Log.Trace($"[AutoTest] Step {_currentStep - 1} completed, moving to step {_currentStep}");
            }
        }

        private void SetMove(float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudMoveDx = dx;
            _ctx.HudMoveDz = dz;
            _ctx.HudHasMove = System.Math.Abs(dx) > 0.01f || System.Math.Abs(dz) > 0.01f;
        }

        #region IInputFeature 实现

        public void SetMoveInput(float dx, float dz)
        {
            // IInputFeature 接口实现，但 AutoTestInputFeature 通过 Tick 方法自动设置
        }

        public void ClickSkill(int slot)
        {
            // 设置技能点击标记，让 ConsoleInputFeature.ProcessSkillInput() 能够采集
            if (_ctx == null) return;
            _ctx.HudSkillClickSlot = slot;
        }

        public void AimSkill(int slot, float dx, float dz)
        {
            // IInputFeature 接口实现，AutoTestInputFeature 不支持瞄准
        }

        public void ReleaseSkillAim(int slot, float dx, float dz)
        {
            // IInputFeature 接口实现，AutoTestInputFeature 不支持瞄准
        }

        #endregion

        /// <summary>
        /// 获取当前步骤索引
        /// </summary>
        public int CurrentStep => _currentStep;

        /// <summary>
        /// 获取总步骤数
        /// </summary>
        public int TotalSteps => _steps.Count;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _started && _currentStep < _steps.Count;
    }

    /// <summary>
    /// 输入步骤类型
    /// </summary>
    public enum InputStepType
    {
        /// <summary>
        /// 移动
        /// </summary>
        Move,
        /// <summary>
        /// 释放技能
        /// </summary>
        Skill,
        /// <summary>
        /// 等待（保持当前输入）
        /// </summary>
        Wait,
        /// <summary>
        /// 待机（停止移动）
        /// </summary>
        Idle
    }

    /// <summary>
    /// 输入步骤
    /// </summary>
    public sealed class InputStep
    {
        public InputStepType Type { get; set; }
        public int DurationTicks { get; set; }
        public int TickCount { get; set; }
        public float Dx { get; set; }
        public float Dz { get; set; }
        public int Slot { get; set; }

        public static InputStep Move(float dx, float dz, int durationTicks)
        {
            return new InputStep
            {
                Type = InputStepType.Move,
                Dx = dx,
                Dz = dz,
                DurationTicks = durationTicks
            };
        }

        public static InputStep Skill(int slot, int durationTicks = 1)
        {
            return new InputStep
            {
                Type = InputStepType.Skill,
                Slot = slot,
                DurationTicks = durationTicks
            };
        }

        public static InputStep Wait(int durationTicks)
        {
            return new InputStep
            {
                Type = InputStepType.Wait,
                DurationTicks = durationTicks
            };
        }

        public static InputStep Idle(int durationTicks)
        {
            return new InputStep
            {
                Type = InputStepType.Idle,
                DurationTicks = durationTicks
            };
        }
    }
}
