using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Demo MOBA strongly typed action schema base.
    /// New schemas only need to provide their config action name and argument parsing rules.
    /// </summary>
    public abstract class MobaPlanActionSchemaBase<TActionArgs> : IActionSchema<TActionArgs, IWorldResolver>
    {
        protected abstract string ActionName { get; }

        public ActionId ActionId => PlanActionRegisterUtil.GetActionId(ActionName);

        public Type ArgsType => typeof(TActionArgs);

        public abstract TActionArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx);

        public abstract bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error);
    }
}
