using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ???? - ????
    ///
    /// ?????
    /// - ?? moba.core ?????????????????
    /// - ?????????????????
    /// - ????? ETBattleViewEventSink ?????
    /// - ????? ETBattleEntityCacheComponent ????
    /// - ?? Moba.Console ? BattleEntity
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnit : Entity, IAwake
    {
        // ========== ?? ==========
        public long ActorId { get; set; }
        public int EntityCode { get; set; }
        public ActorKind Kind { get; set; } = ActorKind.None;
        public string Name { get; set; }

        // ========== ????????? ==========
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }

        // ========== ???? ==========
        public float RenderX { get; set; }
        public float RenderY { get; set; }
        public float PrevX { get; set; }
        public float PrevY { get; set; }
        public long LastUpdateTime { get; set; }

        // ========== ??????????? ==========
        public float Hp { get; set; } = 100f;
        public float MaxHp { get; set; } = 100f;
        public float Attack { get; set; } = 10f;
        public float Defense { get; set; } = 5f;
        public float MoveSpeed { get; set; } = 5f;

        // ========== ?? ==========
        public bool IsDead => Hp <= 0;
        public bool IsLocalPlayer { get; set; }

        // ========== ????????????????? ==========
        public float TargetX { get; set; }
        public float TargetY { get; set; }

        // ========== ??????????? ==========
        public float[] SkillCooldowns { get; set; } = new float[4];

        public void Awake()
        {
            if (SkillCooldowns == null)
                SkillCooldowns = new float[4];
            LastUpdateTime = Environment.TickCount64;
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public void UpdateFromSnapshot(float x, float y, float rotation = 0)
        {
            PrevX = X;
            PrevY = Y;
            X = x;
            Y = y;
            Rotation = rotation;
            LastUpdateTime = Environment.TickCount64;
        }

        /// <summary>
        /// ????? HP ??
        /// </summary>
        public void UpdateHpFromSnapshot(float hp, float maxHp)
        {
            Hp = hp;
            MaxHp = maxHp;
        }

        /// <summary>
        /// ????????????
        /// </summary>
        public void UpdateRenderPosition(float interpolationSpeed, float deltaTime)
        {
            RenderX += (X - RenderX) * interpolationSpeed * deltaTime;
            RenderY += (Y - RenderY) * interpolationSpeed * deltaTime;
        }

        // ========== ???????? ==========
        // TakeDamage() - ??? moba.core ?????????
        // MoveTo() - ??? moba.core ?????????
        // StopMove() - ??? moba.core ?????????
    }
}
