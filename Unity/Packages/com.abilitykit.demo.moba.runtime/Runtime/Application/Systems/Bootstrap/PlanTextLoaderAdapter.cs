using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap
{
    /// <summary>
    /// PlanTextLoader 适配器
    /// 将 ITextLoader 适配为 TriggerPlanJsonDatabase.ITextLoader
    /// </summary>
    public sealed class PlanTextLoaderAdapter : TriggerPlanJsonDatabase.ITextLoader
    {
        private readonly ITextLoader _inner;

        public PlanTextLoaderAdapter(ITextLoader inner)
        {
            _inner = inner;
        }

        public bool TryLoad(string id, out string text)
        {
            if (_inner == null)
            {
                text = null;
                return false;
            }

            return _inner.TryLoad(id, out text);
        }
    }
}
