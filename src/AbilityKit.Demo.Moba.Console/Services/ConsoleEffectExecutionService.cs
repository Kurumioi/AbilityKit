using System;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// Console 平台效果表现服务（表现层）
    /// 仅负责播放视觉表现效果，不包含任何逻辑层代码
    ///
    /// 职责边界：
    /// - ✅ 播放技能特效动画
    /// - ✅ 播放命中特效
    /// - ✅ 播放死亡特效
    /// - ✅ 播放区域效果
    /// - ✅ 播放弹道效果
    /// - ✅ 播放 BUFF 特效
    /// - ❌ 伤害计算（应在 ConsoleDamageService）
    /// - ❌ HP 修改（应在 BattleServices）
    /// - ❌ 目标查找（应在 SkillExecutor）
    /// - ❌ 冷却管理（应在 SkillExecutor）
    /// </summary>
    public sealed class ConsoleEffectExecutionService : IService
    {
        public ConsoleEffectExecutionService()
        {
        }

        /// <summary>
        /// 播放技能施法特效（表现层）
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <param name="casterActorId">施法者ID</param>
        public void PlaySkillEffect(int skillId, int casterActorId)
        {
            Log.Skill($"[Visual] Actor#{casterActorId} cast skill#{skillId}");
        }

        /// <summary>
        /// 播放命中特效（表现层）
        /// </summary>
        /// <param name="targetActorId">目标ID</param>
        /// <param name="damageType">伤害类型 1=物理 2=魔法 3=真实</param>
        public void PlayHitEffect(int targetActorId, int damageType)
        {
            var typeName = damageType switch
            {
                1 => "物理",
                2 => "魔法",
                3 => "真实",
                _ => "未知"
            };
            Log.Damage($"[Visual] Hit effect on #{targetActorId} ({typeName}伤害)");
        }

        /// <summary>
        /// 播放死亡特效（表现层）
        /// </summary>
        /// <param name="actorId">角色ID</param>
        public void PlayDeathEffect(int actorId)
        {
            Log.Battle($"[Visual] Death effect on #{actorId}");
        }

        /// <summary>
        /// 播放效果特效（表现层）
        /// </summary>
        /// <param name="effectId">效果ID</param>
        /// <param name="sourceActorId">施法者ID</param>
        /// <param name="targetActorId">目标ID</param>
        public void PlayEffect(int effectId, int sourceActorId, int targetActorId)
        {
            if (effectId <= 0) return;

            switch (effectId)
            {
                case 10001:
                    Log.Skill($"[Visual] #{sourceActorId} 释放了普通攻击特效 -> #{targetActorId}");
                    break;
                case 10002:
                    Log.Skill($"[Visual] #{sourceActorId} 释放了技能2特效 -> #{targetActorId}");
                    break;
                case 10003:
                    Log.Skill($"[Visual] #{sourceActorId} 释放了大招特效 -> #{targetActorId}");
                    break;
                default:
                    Log.Skill($"[Visual] #{sourceActorId} 释放了效果#{effectId} -> #{targetActorId}");
                    break;
            }
        }

        /// <summary>
        /// 播放区域效果开始（表现层）
        /// </summary>
        public void PlayAreaEffectStart(int areaId, int templateId, float centerX, float centerZ, float radius)
        {
            Log.Area($"[Visual] Area#{areaId} started at ({centerX:F1}, {centerZ:F1}) radius={radius:F1}");
        }

        /// <summary>
        /// 播放区域效果结束（表现层）
        /// </summary>
        public void PlayAreaEffectEnd(int areaId)
        {
            Log.Area($"[Visual] Area#{areaId} ended");
        }

        /// <summary>
        /// 播放弹道生成（表现层）
        /// </summary>
        public void PlayProjectileSpawn(int projectileId, int templateId, float x, float y, float z)
        {
            Log.Projectile($"[Visual] Projectile#{projectileId} (Template#{templateId}) spawned at ({x:F1}, {y:F1}, {z:F1})");
        }

        /// <summary>
        /// 播放弹道命中（表现层）
        /// </summary>
        public void PlayProjectileHit(int projectileId, int targetActorId)
        {
            Log.Projectile($"[Visual] Projectile#{projectileId} hit #{targetActorId}");
        }

        /// <summary>
        /// 播放弹道消失（表现层）
        /// </summary>
        public void PlayProjectileExpire(int projectileId)
        {
            Log.Projectile($"[Visual] Projectile#{projectileId} expired");
        }

        /// <summary>
        /// 播放BUFF施加（表现层）
        /// </summary>
        public void PlayBuffApply(int targetId, int buffId, int casterId)
        {
            Log.Buff($"[Visual] #{targetId} gains Buff#{buffId} from #{casterId}");
        }

        /// <summary>
        /// 播放BUFF移除（表现层）
        /// </summary>
        public void PlayBuffRemove(int targetId, int buffId)
        {
            Log.Buff($"[Visual] #{targetId} loses Buff#{buffId}");
        }

        public void Dispose()
        {
        }
    }
}
