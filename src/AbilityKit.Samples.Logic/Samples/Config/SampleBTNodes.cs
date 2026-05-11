using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Samples.Config
{
    /// <summary>
    /// 閫夋嫨鍣ㄨ妭鐐?- 渚濇鎵ц瀛愯妭鐐癸紝杩斿洖绗竴涓垚鍔熺殑
    /// </summary>
    [BTNodeTypeId("Selector")]
    public sealed class SelectorBTNode { }

    /// <summary>
    /// 搴忓垪鑺傜偣 - 渚濇鎵ц瀛愯妭鐐癸紝杩斿洖绗竴涓け璐ョ殑
    /// </summary>
    [BTNodeTypeId("Sequence")]
    public sealed class SequenceBTNode { }

    /// <summary>
    /// 鏉′欢鑺傜偣 - 鎵ц鏉′欢妫€鏌?
    /// </summary>
    [BTNodeTypeId("Condition")]
    public sealed class ConditionBTNode { }

    /// <summary>
    /// 鍔ㄤ綔鑺傜偣 - 鎵ц鍏蜂綋鍔ㄤ綔
    /// </summary>
    [BTNodeTypeId("Action")]
    public sealed class ActionBTNode { }

    /// <summary>
    /// 骞惰鑺傜偣 - 鍚屾椂鎵ц鎵€鏈夊瓙鑺傜偣
    /// </summary>
    [BTNodeTypeId("Parallel")]
    public sealed class ParallelBTNode { }

    /// <summary>
    /// 寰幆鑺傜偣 - 閲嶅鎵ц瀛愯妭鐐?
    /// </summary>
    [BTNodeTypeId("Loop")]
    public sealed class LoopBTNode { }
}
