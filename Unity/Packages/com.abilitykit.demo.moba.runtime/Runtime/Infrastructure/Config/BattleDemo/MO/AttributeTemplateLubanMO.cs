using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    /// <summary>
    /// 基于 Luban 二进制配置的属性模板 MO。
    /// </summary>
    public sealed class AttributeTemplateLubanMO
    {
        /// <summary>
        /// 模板编号。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 升级战斗属性方案编号。
        /// </summary>
        public int UpgradeCode { get; }

        /// <summary>
        /// 主动技能列表。
        /// </summary>
        public IReadOnlyList<int> ActiveSkills { get; }

        /// <summary>
        /// 被动技能列表。
        /// </summary>
        public IReadOnlyList<int> PassiveSkills { get; }

        /// <summary>
        /// 生命值。
        /// </summary>
        public int Hp { get; }

        /// <summary>
        /// 最大生命值。
        /// </summary>
        public int MaxHp { get; }

        /// <summary>
        /// 额外生命值。
        /// </summary>
        public int ExtraHp { get; }

        /// <summary>
        /// 鐗╃悊鏀诲嚮
        /// </summary>
        public int PhysicsAttack { get; }

        /// <summary>
        /// 娉曟湳鏀诲嚮
        /// </summary>
        public int MagicAttack { get; }

        /// <summary>
        /// 棰濆鐗╃悊鏀诲嚮
        /// </summary>
        public int ExtraPhysicsAttack { get; }

        /// <summary>
        /// 棰濆娉曟湳鏀诲嚮
        /// </summary>
        public int ExtraMagicAttack { get; }

        /// <summary>
        /// 鐗╃悊闃插尽
        /// </summary>
        public int PhysicsDefense { get; }

        /// <summary>
        /// 娉曟湳闃插尽
        /// </summary>
        public int MagicDefense { get; }

        /// <summary>
        /// 娉曞姏鍊?
        /// </summary>
        public int Mana { get; }

        /// <summary>
        /// 最大法力值。
        /// </summary>
        public int MaxMana { get; }

        /// <summary>
        /// 鏆村嚮鐜?
        /// </summary>
        public int CriticalR { get; }

        /// <summary>
        /// 鏀婚€熷€嶇巼
        /// </summary>
        public int AttackSpeedR { get; }

        /// <summary>
        /// 冷却缩减。
        /// </summary>
        public int CooldownReduceR { get; }

        /// <summary>
        /// 鐗╃悊绌块€?
        /// </summary>
        public int PhysicsPenetrationR { get; }

        /// <summary>
        /// 娉曟湳绌块€?
        /// </summary>
        public int MagicPenetrationR { get; }

        /// <summary>
        /// 绉诲姩閫熷害
        /// </summary>
        public int MoveSpeed { get; }

        /// <summary>
        /// 物理吸血。
        /// </summary>
        public int PhysicsBloodsuckingR { get; }

        /// <summary>
        /// 法术吸血。
        /// </summary>
        public int MagicBloodsuckingR { get; }

        /// <summary>
        /// 鏀诲嚮鑼冨洿
        /// </summary>
        public int AttackRange { get; }

        /// <summary>
        /// 每秒回血。
        /// </summary>
        public int PerSecondBloodR { get; }

        /// <summary>
        /// 每秒回蓝。
        /// </summary>
        public int PerSecondManaR { get; }

        /// <summary>
        /// 闊ф€?
        /// </summary>
        public int ResilienceR { get; }

        public AttributeTemplateLubanMO(global::cfg.DRAttributeTemplates dr)
        {
            if (dr == null) throw new ArgumentNullException(nameof(dr));
            Id = dr.Code;
            UpgradeCode = dr.UpgradeCode;
            ActiveSkills = dr.ActiveSkills ?? new List<int>();
            PassiveSkills = dr.PassiveSkills ?? new List<int>();
            Hp = dr.Hp;
            MaxHp = dr.MaxHp;
            ExtraHp = dr.ExtraHp;
            PhysicsAttack = dr.PhysicsAttack;
            MagicAttack = dr.MagicAttack;
            ExtraPhysicsAttack = dr.ExtraPhysicsAttack;
            ExtraMagicAttack = dr.ExtraMagicAttack;
            PhysicsDefense = dr.PhysicsDefense;
            MagicDefense = dr.MagicDefense;
            Mana = dr.Mana;
            MaxMana = dr.MaxMana;
            CriticalR = dr.CriticalR;
            AttackSpeedR = dr.AttackSpeedR;
            CooldownReduceR = dr.CooldownReduceR;
            PhysicsPenetrationR = dr.PhysicsPenetrationR;
            MagicPenetrationR = dr.MagicPenetrationR;
            MoveSpeed = dr.MoveSpeed;
            PhysicsBloodsuckingR = dr.PhysicsBloodsuckingR;
            MagicBloodsuckingR = dr.MagicBloodsuckingR;
            AttackRange = dr.AttackRange;
            PerSecondBloodR = dr.PerSecondBloodR;
            PerSecondManaR = dr.PerSecondManaR;
            ResilienceR = dr.ResilienceR;
        }
    }
}
