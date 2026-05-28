using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 输入命令 - 移动命令
    /// 发送移动方向向量（dx, dz），不是目标坐标
    /// 与 moba.view 的 BattleInputFeature 保持一致
    ///
    /// Design:
    /// - PlayerId 是与 moba.core 中 MobaPlayerActorMapService 注册时一致的 PlayerId
    /// - 在 ProcessETInputPhase 中直接使用 PlayerId，不再进行类型转换
    /// </summary>
    public sealed record MoveCommand(int Frame, string PlayerId, float Dx, float Dz);

    /// <summary>
    /// 输入命令 - 技能命令
    /// </summary>
    public sealed record SkillCommand(int Frame, string PlayerId, int SkillSlot, float TargetX, float TargetY);

    /// <summary>
    /// 输入命令 - 停止命令
    /// </summary>
    public sealed record StopCommand(int Frame, string PlayerId);
}
