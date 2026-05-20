namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 阶段上下文
    /// </summary>
    public sealed class PhaseContext
    {
        /// <summary>
        /// 根对象
        /// </summary>
        public IModuleContext Root { get; set; }

        /// <summary>
        /// 当前阶段名
        /// </summary>
        public string PhaseName { get; set; }

        /// <summary>
        /// 上一个阶段名
        /// </summary>
        public string PreviousPhase { get; set; }

        /// <summary>
        /// 进入时间
        /// </summary>
        public double EnterTime { get; set; }

        /// <summary>
        /// 自定义数据
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Data { get; } = new();
    }

    /// <summary>
    /// 阶段接口
    /// </summary>
    public interface IPhase
    {
        string Name { get; }
        void OnEnter(PhaseContext context);
        void OnTick(PhaseContext context, float deltaTime);
        void OnExit(PhaseContext context, string nextPhase);
    }
}
