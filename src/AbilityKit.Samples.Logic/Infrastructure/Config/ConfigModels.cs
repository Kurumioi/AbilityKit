using System.Collections.Generic;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    #region 閫氱敤閰嶇疆妯″瀷

    /// <summary>
    /// 閫氱敤閿€煎閰嶇疆
    /// </summary>
    public class KeyValueConfig
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 鍛藉悕鐨勬暟鍊奸厤缃?
    /// </summary>
    public class NamedValue
    {
        public string Name { get; set; }
        public float Value { get; set; }
    }

    /// <summary>
    /// 甯﹀弬鏁扮殑鍛藉悕鐨勬暟鍊奸厤缃?
    /// </summary>
    public class NamedValueEx : NamedValue
    {
        public Dictionary<string, float> Params { get; set; } = new();
    }

    #endregion

    #region 鐘舵€佹満閰嶇疆

    /// <summary>
    /// 鐘舵€侀厤缃?
    /// </summary>
    public class StateConfig
    {
        public string Name { get; set; }
        public string OnEnter { get; set; }
        public string OnLogic { get; set; }
        public string OnExit { get; set; }
        public List<string> Transitions { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 鐘舵€佽浆鎹㈤厤缃?
    /// </summary>
    public class TransitionConfig
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Condition { get; set; }
        public float Priority { get; set; }
    }

    /// <summary>
    /// 鐘舵€佹満閰嶇疆
    /// </summary>
    public class StateMachineConfig
    {
        public string Name { get; set; }
        public string InitialState { get; set; }
        public List<StateConfig> States { get; set; } = new();
        public List<TransitionConfig> Transitions { get; set; } = new();
    }

    #endregion

    #region 鎶€鑳?绠＄嚎閰嶇疆

    /// <summary>
    /// 鎶€鑳介樁娈甸厤缃?
    /// </summary>
    public class PhaseConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public float Duration { get; set; }
        public List<PhaseConfig> Children { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 鎶€鑳介厤缃?
    /// </summary>
    public class AbilityConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Cooldown { get; set; }
        public float ManaCost { get; set; }
        public List<PhaseConfig> Phases { get; set; } = new();
        public List<string> Conditions { get; set; } = new();
    }

    #endregion

    #region 琛屼负鏍戦厤缃?

    /// <summary>
    /// 琛屼负鑺傜偣閰嶇疆
    /// </summary>
    public class BehaviorNodeConfig
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<BehaviorNodeConfig> Children { get; set; } = new();
    }

    /// <summary>
    /// 琛屼负鏍戦厤缃?
    /// </summary>
    public class BehaviorTreeConfig
    {
        public string Name { get; set; }
        public BehaviorNodeConfig Root { get; set; }
        public Dictionary<string, object> Blackboard { get; set; } = new();
    }

    #endregion

    #region 鎴樻枟瀹炰綋閰嶇疆

    /// <summary>
    /// 鎴樻枟瀹炰綋閰嶇疆
    /// </summary>
    public class BattleEntityConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Health { get; set; }
        public float Attack { get; set; }
        public float Defense { get; set; }
        public float Speed { get; set; }
        public Dictionary<string, float> Attributes { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 鎴樻枟閰嶇疆
    /// </summary>
    public class BattleConfig
    {
        public string Name { get; set; }
        public List<BattleEntityConfig> Allies { get; set; } = new();
        public List<BattleEntityConfig> Enemies { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    #endregion
}
