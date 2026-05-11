using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Samples.Config
{
    /// <summary>
    /// 妫€娴嬪埌鏁屼汉鏉′欢
    /// </summary>
    [TransitionConditionTypeId("EnemyDetected")]
    public sealed class EnemyDetectedCondition { }

    /// <summary>
    /// 鍦ㄦ敾鍑昏寖鍥村唴鏉′欢
    /// </summary>
    [TransitionConditionTypeId("InAttackRange")]
    public sealed class InAttackRangeCondition { }

    /// <summary>
    /// 鐩爣涓㈠け鏉′欢
    /// </summary>
    [TransitionConditionTypeId("TargetLost")]
    public sealed class TargetLostCondition { }

    /// <summary>
    /// 鐩爣瀛樻椿鏉′欢
    /// </summary>
    [TransitionConditionTypeId("TargetAlive")]
    public sealed class TargetAliveCondition { }

    /// <summary>
    /// 鐩爣姝讳骸鏉′欢
    /// </summary>
    [TransitionConditionTypeId("TargetDead")]
    public sealed class TargetDeadCondition { }
}
