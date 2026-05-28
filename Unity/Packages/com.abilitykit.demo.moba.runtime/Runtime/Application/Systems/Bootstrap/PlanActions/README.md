# Moba Plan Action 扩展指南

这份文档描述 MOBA runtime 示例里触发动作的推荐扩展方式。目标是让新逻辑世界只补充自己的动作参数、参数解析和执行模块，其余注册、排序、初始化都复用现有框架。

## 扩展入口

触发动作模块通过 `PlanActionModuleRegistry` 自动发现：

- 模块类必须实现 `IPlanActionModule`。
- 模块类必须带 `[PlanActionModule(order: n)]`。
- 模块类必须有无参构造函数。
- 注册表会扫描当前 runtime 程序集，按 `order` 和类型名稳定排序。

优先使用 `NamedArgsPlanActionModuleBase<TArgs, IWorldResolver, TModule>`。它让动作保持三段式结构：

- `Args`：动作运行时强类型参数。
- `Schema`：把配置里的命名参数解析为 `Args`。
- `Module`：注册动作并执行逻辑。

## 新增动作步骤

1. 在 `TriggeringConstants.Actions` 中增加动作名常量，并暴露对应 `ActionId`。
2. 在 `PlanActions/Args` 下新增只读参数结构，例如 `KnockArgs`。
3. 在 `PlanActions/Schemas` 下新增 `IActionSchema<TArgs, IWorldResolver>` 实现。
4. 新增 `PlanActionModule` 类，继承 `NamedArgsPlanActionModuleBase<TArgs, IWorldResolver, TModule>`。
5. 给模块类添加 `[PlanActionModule(order: n)]`，确保与同类动作的执行注册顺序一致。
6. 在触发计划配置里使用新的动作名和参数名。

## 模块模板

```csharp
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Systems
{
    public readonly struct MyActionArgs
    {
        public readonly int TargetId;

        public MyActionArgs(int targetId)
        {
            TargetId = targetId;
        }

        public static MyActionArgs Default => new MyActionArgs(0);
    }

    public sealed class MyActionSchema : IActionSchema<MyActionArgs, IWorldResolver>
    {
        public static readonly MyActionSchema Instance = new MyActionSchema();

        public ActionId ActionId => TriggeringConstants.GetActionId("my_action");
        public System.Type ArgsType => typeof(MyActionArgs);

        public MyActionArgs ParseArgs(
            System.Collections.Generic.Dictionary<string, ActionArgValue> namedArgs,
            ExecCtx<IWorldResolver> ctx)
        {
            var targetId = 0;
            if (namedArgs != null && namedArgs.TryGetValue("target_id", out var value))
            {
                var raw = value.Ref.Kind == ENumericValueRefKind.Const
                    ? value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(value.Ref, ctx);
                targetId = (int)System.Math.Round(raw);
            }

            return new MyActionArgs(targetId);
        }

        public bool TryValidateArgs(
            System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, ActionArgValue>> args,
            out string error)
        {
            error = null;
            return true;
        }
    }

    [PlanActionModule(order: 100)]
    public sealed class MyActionModule : NamedArgsPlanActionModuleBase<MyActionArgs, IWorldResolver, MyActionModule>
    {
        protected override ActionId ActionId => TriggeringConstants.GetActionId("my_action");
        protected override IActionSchema<MyActionArgs, IWorldResolver> Schema => MyActionSchema.Instance;

        protected override void Execute(object triggerArgs, MyActionArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (ctx.Context == null) return;

            // Resolve logic-world services here and apply deterministic logic changes.
        }
    }
}
```

## 最佳实践

- 动作执行只写逻辑世界状态，不直接访问表现层对象。
- 外部资源、随机数、时间等必须通过世界服务注入，避免非确定性来源散落在动作内部。
- 参数解析集中在 `Schema`，执行模块只接收强类型 `Args`。
- 需要运行时服务时从 `ExecCtx<IWorldResolver>.Context` 解析接口，避免依赖具体实现。
- 日志类动作可以保留为调试工具，但业务动作不要把日志作为主要控制流。
- 动作 `order` 只表达注册顺序，不应该依赖它处理同一帧的业务先后关系；业务先后应由触发计划或系统阶段表达。
