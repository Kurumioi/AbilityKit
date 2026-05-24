using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 战斗自动测试组件
    /// 模拟玩家输入，用于验证移动、位置变化等逻辑
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleAutoTestComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // Auto test enabled
        public bool IsEnabled { get; set; } = true;

        // Test parameters
        public long TestActorId { get; set; }
        public int MoveIntervalFrames { get; set; } = 10; // Frames between move commands
        public float MoveSpeed { get; set; } = 5f; // Move speed
        public float TargetX { get; set; } = 50f; // Target X
        public float TargetY { get; set; } = 0f; // Target Y

        // Internal state
        private int _currentFrame;
        private float _currentX;
        private float _currentY;
        private int _moveCommandCount;
        private float _moveDistance;

        // Stats
        public int MoveCommandCount => _moveCommandCount;
        public float CurrentX => _currentX;
        public float CurrentY => _currentY;
        public float MoveDistance => _moveDistance;

        public void Awake()
        {
            Log.Info("[ETBattleAutoTest] Component created");
        }

        /// <summary>
        /// Initialize test
        /// </summary>
        public void Initialize(long actorId, float startX, float startY)
        {
            TestActorId = actorId;
            _currentX = startX;
            _currentY = startY;
            _currentFrame = 0;
            _moveCommandCount = 0;
            _moveDistance = 0f;

            Log.Info($"[ETBattleAutoTest] Initialized for ActorId={actorId}, StartPos=({startX}, {startY})");
        }

        public void Update()
        {
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void OnUpdate(int frame)
        {
            if (!IsEnabled)
                return;

            _currentFrame = frame;

            // 每隔指定帧数发送移动命令
            if (_currentFrame % MoveIntervalFrames == 0)
            {
                SendMoveCommand();
            }
        }

        /// <summary>
        /// 发送移动命令
        /// </summary>
        private void SendMoveCommand()
        {
            // 计算新目标（随机偏移）
            float newTargetX = _currentX + (float)Math.Sin(_currentFrame * 0.1) * 5f;
            float newTargetY = _currentY + (float)Math.Cos(_currentFrame * 0.1) * 5f;

            // 限制范围
            newTargetX = Math.Clamp(newTargetX, -100f, 100f);
            newTargetY = Math.Clamp(newTargetY, -100f, 100f);

            // 获取输入组件
            var inputComponent = this.Scene().GetComponent<ETInputComponent>();
            if (inputComponent != null)
            {
                inputComponent.AddMoveCommand(_currentFrame, (int)TestActorId, newTargetX, newTargetY);
                _moveCommandCount++;

                float dx = newTargetX - _currentX;
                float dy = newTargetY - _currentY;
                _moveDistance += (float)Math.Sqrt(dx * dx + dy * dy);

                Log.Debug($"[ETBattleAutoTest] Move command sent: Frame={_currentFrame}, ActorId={TestActorId}, Target=({newTargetX:F2}, {newTargetY:F2})");
            }
        }

        /// <summary>
        /// 更新当前模拟位置（基于模拟移动）
        /// </summary>
        public void SimulateMove(float deltaTime)
        {
            // 简单模拟移动
            _currentX += (float)Math.Sin(_currentFrame * 0.1) * MoveSpeed * deltaTime;
            _currentY += (float)Math.Cos(_currentFrame * 0.1) * MoveSpeed * deltaTime;

            // 限制范围
            _currentX = Math.Clamp(_currentX, -100f, 100f);
            _currentY = Math.Clamp(_currentY, -100f, 100f);
        }

        public void Destroy()
        {
            Log.Info($"[ETBattleAutoTest] Destroyed. Total move commands: {_moveCommandCount}, Total distance: {_moveDistance:F2}");
        }
    }
}
