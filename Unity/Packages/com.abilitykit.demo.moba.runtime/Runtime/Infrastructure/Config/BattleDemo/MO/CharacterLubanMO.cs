using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    /// <summary>
    /// 基于 Luban 二进制配置的 Character MO。
    /// 技能和被动技能由 AttributeTemplate 提供，不在 Character 配置中重复维护。
    /// </summary>
    public sealed class CharacterLubanMO
    {
        /// <summary>
        /// 角色编号，对应 DRCharacters.Code。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 职业列表。
        /// </summary>
        public IReadOnlyList<int> Career { get; }

        /// <summary>
        /// 模型编号。
        /// </summary>
        public int ModelId { get; }

        /// <summary>
        /// 属性模板编号，引用 AttributeTemplate 表。
        /// </summary>
        public int AttributeTemplateId { get; }

        public CharacterLubanMO(global::cfg.DRCharacters dr)
        {
            if (dr == null) throw new ArgumentNullException(nameof(dr));
            Id = dr.Code;
            Career = dr.Career ?? new List<int>();
            ModelId = dr.ModelId;
            AttributeTemplateId = dr.AttributeTemplateId;
        }
    }
}
