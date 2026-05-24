using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Skill test component
    /// Simulates player casting skills to verify skill config loading and trigger invocation
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleSkillTestComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // Is skill test enabled
        public bool IsEnabled { get; set; } = false;

        // Test parameters
        public long TestActorId { get; set; }
        public int SkillSlot { get; set; } = 0; // Cast first skill
        public int SkillIntervalFrames { get; set; } = 60; // Cast every 60 frames

        // Internal state
        private int _currentFrame;
        private int _skillCastCount;
        private int _lastCastFrame;

        // Stats
        public int SkillCastCount => _skillCastCount;
        public int LastCastFrame => _lastCastFrame;

        public void Awake()
        {
            Log.Info("[ETBattleSkillTest] Component created");
        }

        /// <summary>
        /// Initialize skill test
        /// </summary>
        public void Initialize(long actorId, int skillSlot = 0)
        {
            TestActorId = actorId;
            SkillSlot = skillSlot;
            _currentFrame = 0;
            _skillCastCount = 0;
            _lastCastFrame = 0;

            Log.Info($"[ETBattleSkillTest] Initialized for ActorId={actorId}, SkillSlot={skillSlot}");
        }

        public void Update()
        {
        }

        /// <summary>
        /// Every frame update
        /// </summary>
        public void OnUpdate(int frame)
        {
            if (!IsEnabled)
                return;

            _currentFrame = frame;

            // Cast skill every specified frames
            if (_currentFrame - _lastCastFrame >= SkillIntervalFrames)
            {
                CastSkill(_currentFrame);
                _lastCastFrame = _currentFrame;
            }
        }

        /// <summary>
        /// Cast skill
        /// </summary>
        private void CastSkill(int currentFrame)
        {
            var inputComponent = this.Scene().GetComponent<ETInputComponent>();
            if (inputComponent == null)
            {
                Log.Warning("[ETBattleSkillTest] ETInputComponent not found!");
                return;
            }

            // Get unit position as skill target
            var unitComponent = this.Scene().GetComponent<ETUnitComponent>();
            float targetX = 0f;
            float targetY = 0f;

            if (unitComponent != null)
            {
                var unit = ETUnitComponentSystem.GetUnit(unitComponent, TestActorId);
                if (unit != null)
                {
                    targetX = unit.X + 5f; // Cast in front of unit
                    targetY = unit.Y;
                }
            }

            // Add skill command with current frame
            inputComponent.AddSkillCommand(currentFrame, (int)TestActorId, SkillSlot, targetX, targetY);
            _skillCastCount++;

            Log.Info($"[ETBattleSkillTest] Skill cast: Frame={currentFrame}, ActorId={TestActorId}, Slot={SkillSlot}, Target=({targetX:F2}, {targetY:F2})");
        }

        public void Destroy()
        {
            Log.Info($"[ETBattleSkillTest] Destroyed. Total skill casts: {_skillCastCount}");
        }
    }
}
