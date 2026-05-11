using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Samples.Config
{
    /// <summary>
    /// 鎾斁寰呮満鍔ㄧ敾
    /// </summary>
    [StateActionTypeId("PlayIdleAnimation")]
    public sealed class PlayIdleAnimationAction { }

    /// <summary>
    /// 妫€鏌ユ槸鍚︽湁鏁屼汉
    /// </summary>
    [StateActionTypeId("CheckForEnemies")]
    public sealed class CheckForEnemiesAction { }

    /// <summary>
    /// 鍋滄寰呮満鍔ㄧ敾
    /// </summary>
    [StateActionTypeId("StopIdleAnimation")]
    public sealed class StopIdleAnimationAction { }

    /// <summary>
    /// 鎾斁濂旇窇鍔ㄧ敾
    /// </summary>
    [StateActionTypeId("PlayRunAnimation")]
    public sealed class PlayRunAnimationAction { }

    /// <summary>
    /// 绉诲姩鍒扮洰鏍?
    /// </summary>
    [StateActionTypeId("MoveToTarget")]
    public sealed class MoveToTargetAction { }

    /// <summary>
    /// 鍋滄绉诲姩
    /// </summary>
    [StateActionTypeId("StopMoving")]
    public sealed class StopMovingAction { }

    /// <summary>
    /// 鎾斁鏀诲嚮鍔ㄧ敾
    /// </summary>
    [StateActionTypeId("PlayAttackAnimation")]
    public sealed class PlayAttackAnimationAction { }

    /// <summary>
    /// 妫€鏌ユ敾鍑昏寖鍥?
    /// </summary>
    [StateActionTypeId("CheckAttackRange")]
    public sealed class CheckAttackRangeAction { }

    /// <summary>
    /// 閲嶇疆鏀诲嚮鍐峰嵈
    /// </summary>
    [StateActionTypeId("ResetAttackCooldown")]
    public sealed class ResetAttackCooldownAction { }

    /// <summary>
    /// 鎾斁姝讳骸鍔ㄧ敾
    /// </summary>
    [StateActionTypeId("PlayDeathAnimation")]
    public sealed class PlayDeathAnimationAction { }

    /// <summary>
    /// 娣″嚭鏁堟灉
    /// </summary>
    [StateActionTypeId("FadeOut")]
    public sealed class FadeOutAction { }
}
