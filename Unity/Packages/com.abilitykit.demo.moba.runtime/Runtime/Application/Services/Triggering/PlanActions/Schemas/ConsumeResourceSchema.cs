using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// consume_resource Action �?Schema 瀹氫�?
    /// 瀹炵�?IActionSchema锛屾彁渚涘弬鏁拌В鏋愬拰楠岃瘉閫昏�?
    /// </summary>
    public sealed class ConsumeResourceSchema : MobaPlanActionSchemaBase<ConsumeResourceArgs>
    {
        public static readonly ConsumeResourceSchema Instance = new ConsumeResourceSchema();

        protected override string ActionName => TriggeringConstants.Actions.ConsumeResource;

        public override ConsumeResourceArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            float amount = 0f;
            ResourceType resourceType = ResourceType.Mana;
            string failMessageKey = "not_enough_resource";

            if (namedArgs == null || namedArgs.Count == 0)
                return new ConsumeResourceArgs(resourceType, amount, failMessageKey);

            foreach (var kv in namedArgs)
            {
                var rawValue = kv.Value.Ref.Kind == ENumericValueRefKind.Const
                    ? kv.Value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(kv.Value.Ref, ctx);

                switch (kv.Key.ToLowerInvariant())
                {
                    case "amount":
                    case "cost":
                    case "value":
                        amount = (float)rawValue;
                        break;

                    case "resource_type":
                    case "resourcetype":
                    case "type":
                        resourceType = (ResourceType)(int)System.Math.Round(rawValue);
                        break;

                    case "fail_message_key":
                    case "failmessagekey":
                    case "fail_key":
                        // 瀛楃涓茬被鍨嬪弬鏁帮紙鏆備笉鏀寔锛屽拷鐣ワ級
                        break;
                }
            }

            return new ConsumeResourceArgs(resourceType, amount, failMessageKey);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            foreach (var kv in args)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "amount":
                    case "cost":
                    case "value":
                        return true;
                }
            }
            // amount 鏄彲閫夌殑锛岄粯璁や负 0锛堣〃绀轰笉娑堣€楋�?
            return true;
        }
    }
}
