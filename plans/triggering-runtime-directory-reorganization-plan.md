# Triggering Runtime 目录重组规划

## 背景

`Unity/Packages/com.abilitykit.triggering/Runtime` 当前已经积累了较多运行时能力：触发器主线、事件总线、计划执行、Json 转换、调度、行为、黑板、变量、验证、兼容层、遗留层、同步等。随着持续拆分协作者，部分类被放在历史目录、过宽目录或临时目录中，导致以下问题：

- 目录按历史演进混合，而不是按稳定职责边界组织。
- `Runtime/Runtime` 目录过宽，既有公开核心契约，也有运行器内部协作者。
- `Plan` 同时承载领域模型、DSL、执行器、Json 导入导出、注册适配，层次偏扁平。
- `Schedule`、`RuleScheduler`、`ActionScheduler` 三类调度概念并列但边界不够明确。
- `Legacy`、`Experimental`、`TriggerScheduler`、`Scheduler`、`Data`、`Example` 等目录容易造成维护入口混淆。
- 部分命名空间与物理目录不一致，例如 `Runtime/Plan/TriggerRunnerPlanExtensions.cs` 位于 Plan 目录但命名空间回到 `AbilityKit.Triggering.Runtime`。

本规划目标是先建立清晰目录蓝图，再分批迁移，避免一次性移动造成大量 `.meta`、`.csproj`、命名空间和引用风险。

## 重组原则

1. **以职责边界组织，而不是按历史文件类型堆叠。**
2. **Public API 优先稳定。** 第一阶段尽量只移动文件路径，不改公共命名空间；后续若要改命名空间，应单独开兼容批次。
3. **运行时主线与配置/导入/验证分层。** 触发执行主线不依赖 Json 细节。
4. **Plan 分层：模型、构建、执行、序列化、验证分开。**
5. **调度概念统一到 Scheduling 大域下，再按用途分子目录。**
6. **Legacy 与 Experimental 隔离。** 遗留兼容和实验代码不能与主线目录混放。
7. **Unity 包迁移必须保留 `.meta`。** 文件移动需要同时移动 `.cs.meta`，避免 GUID 丢失。
8. **每个批次都跑 `dotnet build Unity/AbilityKit.Triggering.Tests.csproj`。**

## 建议目标目录结构

```text
Runtime/
  Assembly/
    com.abilitykit.triggering.asmdef

  Core/
    Execution/
      ExecCtx.cs
      ExecCtxExtensions.cs
      ExecPolicy.cs
      ExecutionControl.cs
      ContextAccessor.cs
      DefaultTCtx.cs
      IActionResolver.cs
    Identity/
      IIdNameRegistry.cs
      IdNameRegistry.cs
    Context/
      ITriggerContextSource.cs
      CompositeContextSource.cs
      TriggerContext.cs
      TriggerCueContext.cs
      ICueParams.cs

  Triggering/
    Contracts/
      ITrigger.cs
      ITriggerCue.cs
      ITriggerLifecycle.cs
      ITriggerObserver.cs
      ILegacyTriggerExecutor.cs
      ETriggerShortCircuitReason.cs
    Runner/
      TriggerRunner.cs
      TriggerRunnerEntry.cs
      TriggerRunnerRegistration.cs
      TriggerRunnerCueDispatcher.cs
      TriggerRunnerRuntimeServices.cs
    Hierarchy/
      HierarchicalTriggerRunner.cs
    Implementations/
      CompiledTrigger.cs
      DelegateTrigger.cs
      NullTriggerCue.cs
      TriggerLifecycleImplementations.cs

  Events/
    IEventBus.cs
    EventBus.cs
    EventBusOptions.cs
    EventChannel.cs
    EventSchemaRegistry.cs
    StableStringId.cs

  Registries/
    ActionId.cs
    ActionRegistry.cs
    FunctionId.cs
    FunctionRegistry.cs
    TriggerRegistry.cs

  Blackboard/
    Core/
    Schema/
    Resolvers/
    Mapping/

  Payload/
    Accessors/
    Registry/
    PayloadStruct.cs

  Variables/
    Numeric/
      Core/
      Domains/
      Expressions/
      Json/

  Plans/
    Model/
      TriggerPlan.cs
      PlannedTrigger.cs
      ActionCallPlan.cs
      PredicateExprPlan.cs
      TriggerExecutionControlPlan.cs
      ScheduleModePlan.cs
      IntValueRef.cs
    Builders/
      TriggerPlanFactory.cs
      ActionCallPlanFactory.cs
      TriggerPlanDsl.cs
      NumericValueRefDsl.cs
      PredicateExprDsl.cs
    Execution/
      PlannedTriggerActionExecutor.cs
      PlannedTriggerPredicateEvaluator.cs
      PlannedTriggerArgumentResolver.cs
      PlannedTriggerActionBindingResolver.cs
      PlannedTriggerScheduleRegistrar.cs
      TriggerRunnerPlanExtensions.cs
      NamedArgsPlanActionModuleBase.cs
    Executables/
      Contracts/
      Atomic/
      Composite/
      ControlFlow/
      Scheduling/
      Metadata/
    Serialization/
      Json/
        Loading/
        Parsing/
        Conversion/
        Source/
        Database/
    Validation/
      可视情况从 Runtime/Validation 中迁入 Plan 专属校验
    Attributes/
      AutoPlanAction.cs

  Executables/
    Core/
      IExecutable.cs
      Executor.cs
      ExecutableRegistry.cs
      ExecutableTriggerDatabase.cs
    Conversion/
      ExecutableDto.cs
      ExecutableConverterStrategies.cs
      ExecutableConverterStrategyRegistry.cs
      ActionDelegateFactory.cs
      ContextAdapter.cs
    Composition/
      AtomicExecutables.cs
      CompositeExecutables.cs
      ScheduledExecutables.cs
      ECompositeMode.cs
    Decorators/
      DecoratorContract.cs
      DefaultDecorators.cs
      DecoratorBuilder.cs
      DecoratorDsl.cs
      DecoratorRegistry.cs
      ComposableInterfaces.cs
    Modifiers/
      ModifierApplier.cs
    Metadata/
      TypeIdRegistry.cs
      IHasPayload.cs

  Behaviors/
    Core/
      IBehaviorContext.cs
      BehaviorExecutionResult.cs
      ResolvedValue.cs
      TriggerBehavior.cs
    Actions/
    Predicates/
    Composite/
    Scheduling/

  Scheduling/
    Common/
      ScheduleHandle.cs
      IScheduleManager.cs
    Simple/
      SimpleScheduleManager.cs
    Grouped/
      GroupedScheduleManager.cs
      GroupedScheduleStore.cs
      GroupedScheduleIndex.cs
    Rules/
      RuleScheduler.cs
      RuleSchedulerTypes.cs
      RuleSchedulerRegistry.cs
      RuleScheduleEntry.cs
    Actions/
      ActionScheduler.cs
      ActionSchedulerManager.cs
      ActionExecutor.cs
      ActionInstance.cs
    Data/
      ScheduleData.cs
      ScheduleItemData.cs
      ScheduleItemState.cs
      ScheduleRegisterRequest.cs
    Effects/
      EffectExecutor.cs
      EffectExecutorAdapter.cs
    Strategies/
      IScheduleStrategy.cs
      DefaultScheduleStrategy.cs
      IScheduleEffect.cs
      ScheduleContext.cs
      ScheduleToBehaviorContextAdapter.cs
      SchedulableBehaviorScheduleAdapter.cs

  Config/
    Actions/
    Cue/
    Plans/
    Predicates/
    Schedule/
    Values/
    Enums.cs

  Validation/
    Core/
      ITriggerValidator.cs
      ValidationContext.cs
      ValidationResult.cs
      TriggerPlanValidation.cs
    Plans/
      ActionCallPlanValidator.cs
      CompositeTriggerValidator.cs
      CycleDetectorValidator.cs
      ExecutionRootValidator.cs
      ReferenceValidator.cs
      TriggerPlanDatabase.cs
      TriggerPlanExecutableValidator.cs
      UgcLimitsValidator.cs
      RuleSchedulePlanValidator.cs
    Compatibility/
      RuntimeCompatibilityCatalog.cs

  RuntimeServices/
    Continuous/
    Diagnostics/
    Random/
    Sync/
    Time/
    Dispatcher/
    Extensions/
    Factory/
    Instance/
    Compatibility/

  Dsl/
    仅保留面向用户的门面；内部 DSL 归入 Plans/Builders

  Legacy/
    Executable/
    Scheduling/
    TriggerScheduler/

  Experimental/
    Todo/
```

> 注：上面是职责目录蓝图。实际迁移时可先保留现有命名空间，减少破坏性；等目录稳定后，再决定是否做命名空间对齐。

## 当前目录到目标目录映射建议

| 当前目录 | 目标目录 | 处理建议 |
|---|---|---|
| `Runtime/Runtime` | `Runtime/Core` + `Runtime/Triggering` | 拆成核心执行上下文、触发器契约、Runner、Hierarchy、默认实现 |
| `Runtime/Eventing` | `Runtime/Events` | 统一事件基础设施命名，避免和 Core.Eventing 混淆 |
| `Runtime/Registry` | `Runtime/Registries` | 放 Action/Function/Trigger 注册表；TriggerRegistry 命名空间后续单独对齐 |
| `Runtime/Plan` | `Runtime/Plans` | 拆 Model、Builders、Execution、Executables、Serialization、Validation |
| `Runtime/Plan/Json` | `Runtime/Plans/Serialization/Json` | 再按 Loading、Parsing、Conversion、Source、Database 分层 |
| `Runtime/Schedule` | `Runtime/Scheduling` | 与 RuleScheduler、ActionScheduler 合并到 Scheduling 大域 |
| `Runtime/RuleScheduler` | `Runtime/Scheduling/Rules` | 与其他调度语义统一管理 |
| `Runtime/ActionScheduler` | `Runtime/Scheduling/Actions` | Action 执行时延迟/推进调度 |
| `Runtime/Schedule/Behavior` | `Runtime/Scheduling/Strategies` + `Runtime/Scheduling/Effects` | 策略、效果执行器、适配器分开 |
| `Runtime/Schedule/Data` | `Runtime/Scheduling/Data` | 纯调度数据结构 |
| `Runtime/Executable` | `Runtime/Executables` | Core、Conversion、Composition、Decorators、Modifiers、Metadata |
| `Runtime/Behavior` | `Runtime/Behaviors` | Core、Actions、Predicates、Composite、Scheduling |
| `Runtime/Config` | `Runtime/Config` | 可保留，内部结构基本合理 |
| `Runtime/Validation` | `Runtime/Validation` + `Runtime/Plans/Validation` | Plan 强相关校验可迁入 Plans/Validation，通用校验留 Runtime/Validation |
| `Runtime/Variables` | `Runtime/Variables` | 保留，但 Numeric 内部进一步分 Core/Domains/Expressions/Json |
| `Runtime/Blackboard` | `Runtime/Blackboard` | 保留，内部按 Core/Schema/Resolvers/Mapping 细化 |
| `Runtime/Payload` | `Runtime/Payload` | 保留，内部按 Accessors/Registry 细化 |
| `Runtime/Continuous` | `Runtime/RuntimeServices/Continuous` | 属于执行服务能力而不是触发核心模型 |
| `Runtime/Sync` | `Runtime/RuntimeServices/Sync` | 同步服务能力 |
| `Runtime/Time` | `Runtime/RuntimeServices/Time` | 时间服务 |
| `Runtime/Random` | `Runtime/RuntimeServices/Random` | 随机服务 |
| `Runtime/Diagnostics` | `Runtime/RuntimeServices/Diagnostics` | 诊断服务 |
| `Runtime/Dispatcher` | `Runtime/RuntimeServices/Dispatcher` | 外部驱动适配层 |
| `Runtime/Extensions` | `Runtime/RuntimeServices/Extensions` 或相邻领域 | 按扩展目标重新归类，避免万能 Extensions |
| `Runtime/Factory` | `Runtime/RuntimeServices/Factory` 或 `Behaviors/Factory` | BehaviorFactory 更适合进入 Behaviors/Factory |
| `Runtime/Instance` | `Runtime/RuntimeServices/Instance` | 实例状态管理服务 |
| `Runtime/Compatibility` | `Runtime/RuntimeServices/Compatibility` 或 `Validation/Compatibility` | 按用途拆分 |
| `Runtime/Dsl` | `Runtime/Dsl` 门面 + `Runtime/Plans/Builders` | 用户门面保留，内部构建 DSL 迁回 Plans |
| `Runtime/Legacy` | `Runtime/Legacy` | 保留并隔离，避免主线引用扩散 |
| `Runtime/Experimental` | `Runtime/Experimental` | 保留，禁止主线依赖 |
| `Runtime/Data`、`Runtime/Scheduler`、`Runtime/TriggerScheduler`、`Runtime/Example` | 待清理 | 若为空删除；若有代码，归并到 Scheduling/Legacy/Examples |

## 分批迁移路线

### 第 0 批：准备与约束

- 生成当前 `Runtime` 文件清单与类清单。
- 明确是否允许改命名空间：建议第一阶段不改。
- 明确 Unity `.meta` 移动策略：必须随 `.cs` 同步移动。
- 统一显式 `.csproj` 更新方式：移动后同步 `Unity/AbilityKit.Triggering.csproj` 与测试项目引用。
- 建议新增一份目录规范文档到 `Documentation~/RuntimeDirectoryGuide.md`。

### 第 1 批：无行为风险的空目录/临时目录清理

优先处理目录层级中最容易混淆但风险最低的项目：

- 删除或归档空目录：`Data`、`Scheduler`、`TriggerScheduler`、`Example`，前提是确认没有有效 `.cs`。
- `Experimental/Todo` 保留，但补 README 或文档说明“不参与主线依赖”。
- `Legacy` 保留，但补边界说明：只允许兼容入口依赖 Legacy，主线新代码不得依赖。

### 第 2 批：Runner 主线目录调整

将 `Runtime/Runtime` 拆成：

- `Triggering/Contracts`
- `Triggering/Runner`
- `Triggering/Hierarchy`
- `Triggering/Implementations`
- `Core/Execution`
- `Core/Identity`
- `Core/Context`

本批收益最大：`TriggerRunner`、`HierarchicalTriggerRunner`、`ExecCtx`、`ExecutionControl`、`ITrigger` 等入口会更清晰。

建议暂不修改命名空间，减少外部引用破坏。

### 第 3 批：调度目录统一

将以下目录归并到 `Scheduling`：

- `Schedule` -> `Scheduling/Common`、`Scheduling/Simple`、`Scheduling/Grouped`、`Scheduling/Data`、`Scheduling/Effects`、`Scheduling/Strategies`
- `RuleScheduler` -> `Scheduling/Rules`
- `ActionScheduler` -> `Scheduling/Actions`

本批要重点验证：

- GroupedScheduleManager 相关拆分文件是否全部纳入 `.csproj`。
- RuleScheduler 相关内部类型可见性是否不受影响。
- ActionSchedulerManager 与 TriggerRunner 的引用是否仍正确。

### 第 4 批：Plan 大域分层

`Plan` 是目前最需要体系化的目录，建议拆为：

- `Plans/Model`
- `Plans/Builders`
- `Plans/Execution`
- `Plans/Executables`
- `Plans/Serialization/Json`
- `Plans/Validation`
- `Plans/Attributes`

其中 `Json` 可再拆：

- `Loading`：目录加载、加载选项、接口。
- `Parsing`：Json parser、parse result。
- `Conversion`：TriggerPlanConverter、ExecutionNodeConverter、ReferenceResolver。
- `Source`：SourceConverter 及 source DTO、shape、writer、resolver。
- `Database`：TriggerPlanJsonDatabase、ValidatingTriggerPlanJsonDatabase。

本批建议和后续 Plan/Json 余留热点拆分结合，但每次只迁一组。

### 第 5 批：Executable 与 Behavior 收口

将 `Executable` 整理为：

- `Executables/Core`
- `Executables/Conversion`
- `Executables/Composition`
- `Executables/Decorators`
- `Executables/Modifiers`
- `Executables/Metadata`

将 `Behavior` 整理为：

- `Behaviors/Core`
- `Behaviors/Actions`
- `Behaviors/Predicates`
- `Behaviors/Composite`
- `Behaviors/Scheduling`

并处理 `Behavior/Schedule/TriggerBehavior.cs` 与 `Behavior/TriggerBehavior.cs` 重名造成的认知负担。

### 第 6 批：基础服务与支撑能力归位

将运行时支撑能力归到 `RuntimeServices`：

- `Continuous`
- `Diagnostics`
- `Random`
- `Sync`
- `Time`
- `Dispatcher`
- `Instance`
- `Compatibility`

`Factory` 与 `Extensions` 不建议作为长期大杂烩目录，应按目标领域拆：

- `BehaviorFactory` -> `Behaviors/Factory`。
- `RpnNumericExprParserExtensions` -> `Variables/Numeric/Expressions` 或 `Plans/Builders`。
- Accessor 相关扩展 -> `Payload` 或 `Context`，按真实领域归位。

### 第 7 批：命名空间对齐（可选破坏性批次）

只有当文件路径稳定后，才考虑命名空间对齐。建议规则：

- Public API 命名空间尽量不变，或通过类型转发/兼容 facade 过渡。
- Internal 类型可随目录改命名空间。
- 每次只对一个大域改命名空间。
- 改命名空间后必须全仓搜索旧 namespace。

## 风险与控制

### Unity `.meta` 风险

移动 `.cs` 必须移动对应 `.cs.meta`。如果只通过脚本或外部工具移动源码，Unity 可能认为资源被删除重建，导致 GUID 改变。

### 显式 `.csproj` 风险

当前 Unity 生成的 `.csproj` 包含显式 `<Compile Include=...>`。移动文件后必须同步：

- `Unity/AbilityKit.Triggering.csproj`
- `Unity/AbilityKit.Triggering.Tests.csproj` 若测试直接引用 Runtime 文件或生成项

### 命名空间风险

第一阶段建议只移动物理文件，不修改 namespace。否则会造成大量 using 调整与外部包兼容风险。

### 目录与程序集边界风险

当前只有一个 runtime asmdef。目录重组不改变程序集边界。如果未来要拆 asmdef，应单独规划依赖方向，不与本次目录迁移混做。

## 推荐落地顺序

1. 文档定稿：确认目标目录树与迁移批次。
2. 低风险清理：空目录、Experimental/Legacy 说明。
3. Runner 主线迁移：先迁 `TriggerRunner` 及协作者。
4. Scheduling 迁移：统一 `Schedule`、`RuleScheduler`、`ActionScheduler`。
5. Plan/Json 分层迁移：结合现有热点继续拆分。
6. Executable/Behavior 收口。
7. RuntimeServices 收口。
8. 可选命名空间对齐。

## 每批完成定义

每个迁移批次至少满足：

- 文件和 `.meta` 已同步移动。
- `.csproj` 显式编译项已更新。
- 没有丢失 public 类型。
- `dotnet build Unity/AbilityKit.Triggering.Tests.csproj` 通过。
- 新增/更新目录说明文档。

## 下一步建议

建议下一轮从“第 2 批 Runner 主线目录调整”开始，因为最近已经在拆 `TriggerRunner`、`TriggerRunnerRuntimeServices`、`TriggerRunnerCueDispatcher` 等协作者，迁移上下文最完整，且收益明显。迁移时先只移动物理路径，不改命名空间，完成后立即跑构建。