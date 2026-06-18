using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Blackboard;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Triggering.Validation;
using NUnit.Framework;

namespace AbilityKit.Triggering.Tests
{
    public sealed class ActionCallPlanValidatorTests
    {
        [Test]
        public void Validate_RejectsUnsupportedArityBeforeRuntimeExecution()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:unsupported_arity"));
            var call = new ActionCallPlan(
                actionId,
                3,
                default,
                default,
                new Dictionary<string, ActionArgValue>
                {
                    ["a"] = ActionArgValue.OfConst(1, "a"),
                    ["b"] = ActionArgValue.OfConst(2, "b"),
                    ["c"] = ActionArgValue.OfConst(3, "c")
                },
                EActionScheduleMode.Immediate,
                0,
                -1,
                true,
                EActionExecutionPolicy.Immediate);

            var result = Validate(call);

            Assert.That(result.Errors.Select(e => e.Code), Does.Contain(ValidationErrorCodes.UNSUPPORTED_ACTION_ARITY));
        }

        [Test]
        public void Validate_RejectsNamedArgCountMismatch()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:named_arg_mismatch"));
            var call = new ActionCallPlan(
                actionId,
                2,
                default,
                default,
                new Dictionary<string, ActionArgValue>
                {
                    ["amount"] = ActionArgValue.OfConst(1, "amount")
                },
                EActionScheduleMode.Immediate,
                0,
                -1,
                true,
                EActionExecutionPolicy.Immediate);

            var result = Validate(call);

            Assert.That(result.Errors.Select(e => e.Code), Does.Contain(ValidationErrorCodes.ACTION_ARG_COUNT_MISMATCH));
        }

        [Test]
        public void Validate_RejectsPeriodicActionWithoutPositiveInterval()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:invalid_periodic_interval"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Periodic,
                0,
                -1,
                true,
                EActionExecutionPolicy.Immediate);

            var result = Validate(call);

            Assert.That(result.Errors.Select(e => e.Code), Does.Contain(ValidationErrorCodes.INVALID_ACTION_SCHEDULE));
        }

        [Test]
        public void Validate_RejectsTimelineScheduleAsUnsupportedMainlinePlan()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:timeline_schedule"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Timeline,
                0,
                -1,
                true,
                EActionExecutionPolicy.Immediate);

            var result = Validate(call);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationIssue>(issue => issue.Code == ValidationErrorCodes.UNSUPPORTED_ACTION_SCHEDULE));
        }

        [Test]
        public void Validate_RejectsRollbackPolicyAsUnsupportedMainlinePlan()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:rollback_policy"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Immediate,
                0,
                -1,
                true,
                EActionExecutionPolicy.WithRollback);

            var result = Validate(call);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationIssue>(issue => issue.Code == ValidationErrorCodes.UNSUPPORTED_ACTION_EXECUTION_POLICY));
        }

        [Test]
        public void MinimalCompositeValidator_IncludesActionCallPlanValidation()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:minimal_composite"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Timeline,
                0,
                -1,
                true,
                EActionExecutionPolicy.Immediate);
            var database = CreateDatabase(call);
            var context = ValidationContext<Ping>.CreateForDevelopment(
                definedActionIds: new HashSet<string> { actionId.Value.ToString() });

            var result = CompositeTriggerValidator<Ping>.CreateMinimal().Validate(in database, in context);

            Assert.That(result.Errors.Select(e => e.Code), Does.Contain(ValidationErrorCodes.UNSUPPORTED_ACTION_SCHEDULE));
        }

        [Test]
        public void ActionCallPlan_ExposesCompatibleSemanticSubPlans()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:semantic_sub_plans"));
            var args = new Dictionary<string, ActionArgValue>
            {
                ["amount"] = ActionArgValue.OfConst(10, "amount")
            };
            var call = new ActionCallPlan(
                actionId,
                1,
                default,
                default,
                args,
                EActionScheduleMode.Delayed,
                150f,
                2,
                false,
                EActionExecutionPolicy.WithRetry,
                retryMaxRetries: 4,
                retryDelayMs: 25f);

            AssertSemanticProjectionMatchesRawFields(call);
        }

        [Test]
        public void ActionCallPlanFactory_ModifiersPreserveSemanticSubPlanProjection()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:factory_projection"));
            var args = new Dictionary<string, ActionArgValue>
            {
                ["amount"] = ActionArgValue.OfConst(7, "amount"),
                ["scale"] = ActionArgValue.OfConst(2, "scale")
            };

            var call = ActionCallPlanFactory.CreateWithArgs(actionId, args);
            call = ActionCallPlanFactory.WithSchedule(call, EActionScheduleMode.Periodic, 33f, 5, false);
            call = ActionCallPlanFactory.WithRetry(call, maxRetries: 2, retryDelayMs: 10f);

            AssertSemanticProjectionMatchesRawFields(call);
            Assert.That(call.Arguments.NamedArgs, Is.SameAs(args));
            Assert.That(call.Schedule.Mode, Is.EqualTo(EActionScheduleMode.Periodic));
            Assert.That(call.Schedule.Param, Is.EqualTo(33f));
            Assert.That(call.Schedule.MaxExecutions, Is.EqualTo(5));
            Assert.That(call.Schedule.CanBeInterrupted, Is.False);
            Assert.That(call.Execution.Policy, Is.EqualTo(EActionExecutionPolicy.WithRetry));
            Assert.That(call.Execution.RetryMaxRetries, Is.EqualTo(2));
            Assert.That(call.Execution.RetryDelayMs, Is.EqualTo(10f));
        }

        [Test]
        public void ActionCallPlanExtensions_ModifiersPreserveSemanticSubPlanProjection()
        {
            var actionId = new ActionId(StableStringId.Get("test:action_plan_validator:extension_projection"));

            var call = ActionCallPlanExtensions.Call(actionId, NumericValueRef.Const(3), NumericValueRef.Const(4))
                .WithSchedule(EActionScheduleMode.Delayed, 50f, 1, true)
                .WithExecutionPolicy(EActionExecutionPolicy.Queued);

            AssertSemanticProjectionMatchesRawFields(call);
            Assert.That(call.Arguments.Arity, Is.EqualTo(2));
            Assert.That(call.Arguments.Arg0.ConstValue, Is.EqualTo(3));
            Assert.That(call.Arguments.Arg1.ConstValue, Is.EqualTo(4));
            Assert.That(call.Schedule.Mode, Is.EqualTo(EActionScheduleMode.Delayed));
            Assert.That(call.Schedule.Param, Is.EqualTo(50f));
            Assert.That(call.Execution.Policy, Is.EqualTo(EActionExecutionPolicy.Queued));
        }

        [Test]
        public void NumericValueRef_UsesFallbackAndPoliciesWhenOptionalSourceMissing()
        {
            var valueRef = NumericValueRef.Blackboard(100, 200)
                .WithFallback(5)
                .WithScale(2)
                .WithOffset(1)
                .WithClamp(0, 10)
                .WithLabel("damage.amount")
                .WithScope("skill:a");
            var args = new Ping();
            var ctx = default(ExecCtx<Ping>);

            var resolved = ActionSchemaRegistry.TryResolveNumericRef(in valueRef, in args, in ctx, out var value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(10));
        }

        [Test]
        public void NumericValueRef_RequiredSourceDoesNotUseFallback()
        {
            var valueRef = NumericValueRef.Blackboard(100, 200)
                .WithFallback(5)
                .AsRequired();
            var args = new Ping();
            var ctx = default(ExecCtx<Ping>);

            var resolved = ActionSchemaRegistry.TryResolveNumericRef(in valueRef, in args, in ctx, out var value);

            Assert.That(resolved, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void NumericValueRef_ResolvesBlackboardAndAppliesPolicies()
        {
            var board = new DictionaryBlackboard();
            board.SetDouble(200, 7);
            var resolver = new DictionaryBlackboardResolver();
            resolver.Register(100, board);
            var ctx = new ExecCtx<Ping>(new Ping(), null, null, null, resolver, null, null, null, null, default, null);
            var args = new Ping();
            var valueRef = NumericValueRef.Blackboard(100, 200)
                .WithScale(3)
                .WithOffset(-1)
                .WithMax(15);

            var resolved = ActionSchemaRegistry.TryResolveNumericRef(in valueRef, in args, in ctx, out var value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(15));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_MapsNumericValueRefPolicies()
        {
            var triggerId = 3010;
            var actionId = StableStringId.Get("test:action_plan_validator:json_numeric_ref_policy_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_numeric_ref_policy_event"",
      ""Actions"": [
        {{
          ""ActionId"": {actionId},
          ""Arity"": 1,
          ""Arg0"": {{
            ""Kind"": ""Blackboard"",
            ""BoardId"": 100,
            ""KeyId"": 200,
            ""HasFallback"": true,
            ""FallbackValue"": 5,
            ""HasScale"": true,
            ""Scale"": 2,
            ""Offset"": 1,
            ""HasMin"": true,
            ""MinValue"": 0,
            ""HasMax"": true,
            ""MaxValue"": 10,
            ""Label"": ""damage.amount"",
            ""Scope"": ""skill:a""
          }}
        }}
      ]
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-numeric-ref-policy-test");

            Assert.That(database.TryGetPlanByTriggerId(triggerId, out var plan), Is.True);
            var valueRef = plan.Actions[0].Arg0;
            Assert.That(valueRef.Kind, Is.EqualTo(ENumericValueRefKind.Blackboard));
            Assert.That(valueRef.HasFallback, Is.True);
            Assert.That(valueRef.FallbackValue, Is.EqualTo(5));
            Assert.That(valueRef.HasScale, Is.True);
            Assert.That(valueRef.Scale, Is.EqualTo(2));
            Assert.That(valueRef.Offset, Is.EqualTo(1));
            Assert.That(valueRef.HasMin, Is.True);
            Assert.That(valueRef.MinValue, Is.EqualTo(0));
            Assert.That(valueRef.HasMax, Is.True);
            Assert.That(valueRef.MaxValue, Is.EqualTo(10));
            Assert.That(valueRef.Label, Is.EqualTo("damage.amount"));
            Assert.That(valueRef.Scope, Is.EqualTo("skill:a"));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_MapsActionScheduleAndExecutionSubPlans()
        {
            var triggerId = 3001;
            var actionId = StableStringId.Get("test:action_plan_validator:json_scheduled_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_event"",
      ""Actions"": [
        {{
          ""ActionId"": {actionId},
          ""Arity"": 1,
          ""Arg0"": {{ ""Kind"": ""Const"", ""ConstValue"": 5 }},
          ""Args"": {{
            ""amount"": {{ ""Kind"": ""Const"", ""ConstValue"": 9 }}
          }},
          ""ScheduleMode"": ""Periodic"",
          ""ScheduleParam"": 33,
          ""MaxExecutions"": 5,
          ""CanBeInterrupted"": false,
          ""ExecutionPolicy"": ""WithRetry"",
          ""RetryMaxRetries"": 2,
          ""RetryDelayMs"": 10
        }}
      ]
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-scheduled-action-test");

            Assert.That(database.TryGetPlanByTriggerId(triggerId, out var plan), Is.True);
            var call = plan.Actions[0];
            AssertSemanticProjectionMatchesRawFields(call);
            Assert.That(call.Arguments.Arity, Is.EqualTo(1));
            Assert.That(call.Arguments.NamedArgs.ContainsKey("amount"), Is.True);
            Assert.That(call.Schedule.Mode, Is.EqualTo(EActionScheduleMode.Periodic));
            Assert.That(call.Schedule.Param, Is.EqualTo(33f));
            Assert.That(call.Schedule.MaxExecutions, Is.EqualTo(5));
            Assert.That(call.Schedule.CanBeInterrupted, Is.False);
            Assert.That(call.Execution.Policy, Is.EqualTo(EActionExecutionPolicy.WithRetry));
            Assert.That(call.Execution.RetryMaxRetries, Is.EqualTo(2));
            Assert.That(call.Execution.RetryDelayMs, Is.EqualTo(10f));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_UsesImmediateScheduleDefaultsWhenOmitted()
        {
            var triggerId = 3002;
            var actionId = StableStringId.Get("test:action_plan_validator:json_default_schedule_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_default_schedule_event"",
      ""Actions"": [
        {{
          ""ActionId"": {actionId},
          ""Arity"": 0
        }}
      ]
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-default-schedule-test");

            Assert.That(database.TryGetPlanByTriggerId(triggerId, out var plan), Is.True);
            var call = plan.Actions[0];
            AssertSemanticProjectionMatchesRawFields(call);
            Assert.That(call.Schedule.Mode, Is.EqualTo(EActionScheduleMode.Immediate));
            Assert.That(call.Schedule.Param, Is.EqualTo(0f));
            Assert.That(call.Schedule.MaxExecutions, Is.EqualTo(-1));
            Assert.That(call.Schedule.CanBeInterrupted, Is.True);
            Assert.That(call.Execution.Policy, Is.EqualTo(EActionExecutionPolicy.Immediate));
            Assert.That(call.Execution.RetryMaxRetries, Is.EqualTo(3));
            Assert.That(call.Execution.RetryDelayMs, Is.EqualTo(0f));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ResolvesSharedBehaviorReferenceExecutionRoot()
        {
            var triggerId = 3003;
            var actionId = StableStringId.Get("test:action_plan_validator:json_behavior_ref_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Behaviors"": {{
    ""shared:sequence"": {{
      ""Kind"": ""Sequence"",
      ""Children"": [
        {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {actionId}, ""Arity"": 0 }} }}
      ]
    }}
  }},
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_behavior_ref_event"",
      ""ExecutionRoot"": {{ ""BehaviorRef"": ""shared:sequence"" }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-behavior-ref-test");

            Assert.That(database.TryGetExecutionRootByTriggerId(triggerId, out var root), Is.True);
            Assert.That(root.Kind, Is.EqualTo(ETriggerPlanExecutableKind.Sequence));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ResolvesSharedNodeReferenceExecutionRoot()
        {
            var triggerId = 3004;
            var actionId = StableStringId.Get("test:action_plan_validator:json_node_ref_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Nodes"": {{
    ""shared:node"": {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {actionId}, ""Arity"": 0 }} }}
  }},
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_node_ref_event"",
      ""ExecutionRoot"": {{ ""NodeRef"": ""shared:node"" }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-node-ref-test");

            Assert.That(database.TryGetExecutionRootByTriggerId(triggerId, out var root), Is.True);
            Assert.That(root.Kind, Is.EqualTo(ETriggerPlanExecutableKind.Action));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ConvertsMetadataExecutionRoot()
        {
            var triggerId = 3009;
            var actionId = StableStringId.Get("test:action_plan_validator:json_metadata_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_metadata_event"",
      ""ExecutionRoot"": {{
        ""Kind"": ""Tags"",
        ""Values"": {{
          ""tag0"": ""damage.fire"",
          ""tag1"": ""status.burning""
        }},
        ""Children"": [
          {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {actionId}, ""Arity"": 0 }} }}
        ]
      }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-metadata-execution-root-test");

            Assert.That(database.TryGetExecutionRootByTriggerId(triggerId, out var root), Is.True);
            var metadata = root as MetadataTriggerPlanExecutable;
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Kind, Is.EqualTo(ETriggerPlanExecutableKind.Metadata));
            Assert.That(metadata.MetadataKind, Is.EqualTo(ETriggerPlanMetadataKind.Tags));
            Assert.That(metadata.Values["tag0"], Is.EqualTo("damage.fire"));
            Assert.That(metadata.Values["tag1"], Is.EqualTo("status.burning"));
            Assert.That(metadata.Child, Is.TypeOf<ActionCallTriggerPlanExecutable>());
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ConvertsMetadataDurationAndContinuousAliases()
        {
            var durationTriggerId = 3010;
            var continuousTriggerId = 3011;
            var durationActionId = StableStringId.Get("test:action_plan_validator:json_duration_metadata_action");
            var continuousActionId = StableStringId.Get("test:action_plan_validator:json_continuous_metadata_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {durationTriggerId},
      ""EventName"": ""test:action_plan_validator:json_duration_metadata_event"",
      ""ExecutionRoot"": {{
        ""Kind"": ""duration"",
        ""MetadataKind"": ""duration"",
        ""Values"": {{
          ""durationMs"": ""1500"",
          ""autoStart"": ""false""
        }},
        ""Children"": [
          {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {durationActionId}, ""Arity"": 0 }} }}
        ]
      }}
    }},
    {{
      ""TriggerId"": {continuousTriggerId},
      ""EventName"": ""test:action_plan_validator:json_continuous_metadata_event"",
      ""ExecutionRoot"": {{
        ""Kind"": ""continuous"",
        ""MetadataKind"": ""continuous"",
        ""Values"": {{
          ""continuationId"": ""channel:burning""
        }},
        ""Children"": [
          {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {continuousActionId}, ""Arity"": 0 }} }}
        ]
      }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-metadata-alias-test");

            Assert.That(database.TryGetExecutionRootByTriggerId(durationTriggerId, out var durationRoot), Is.True);
            var duration = durationRoot as MetadataTriggerPlanExecutable;
            Assert.That(duration, Is.Not.Null);
            Assert.That(duration.MetadataKind, Is.EqualTo(ETriggerPlanMetadataKind.Duration));
            Assert.That(duration.Values["durationMs"], Is.EqualTo("1500"));
            Assert.That(duration.Values["autoStart"], Is.EqualTo("false"));

            Assert.That(database.TryGetExecutionRootByTriggerId(continuousTriggerId, out var continuousRoot), Is.True);
            var continuous = continuousRoot as MetadataTriggerPlanExecutable;
            Assert.That(continuous, Is.Not.Null);
            Assert.That(continuous.MetadataKind, Is.EqualTo(ETriggerPlanMetadataKind.Continuous));
            Assert.That(continuous.Values["continuationId"], Is.EqualTo("channel:burning"));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ExpandsModuleTemplateInstanceBindings()
        {
            var triggerId = 3006;
            var actionId = StableStringId.Get("test:action_plan_validator:json_module_template_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Templates"": {{
    ""damage-module"": {{
      ""Parameters"": [
        {{ ""Name"": ""amount"", ""Required"": true }},
        {{ ""Name"": ""scale"", ""Default"": {{ ""Kind"": ""Const"", ""ConstValue"": 2 }} }}
      ],
      ""Triggers"": [
        {{
          ""TriggerId"": {triggerId},
          ""EventName"": ""test:action_plan_validator:json_module_template_event"",
          ""Actions"": [
            {{
              ""ActionId"": {actionId},
              ""Arity"": 2,
              ""Arg0"": {{ ""Kind"": ""TemplateParam"", ""Key"": ""amount"" }},
              ""Arg1"": {{ ""Kind"": ""TemplateParam"", ""Key"": ""scale"" }}
            }}
          ]
        }}
      ]
    }}
  }},
  ""ModuleInstances"": [
    {{
      ""InstanceId"": ""skill-a"",
      ""TemplateId"": ""damage-module"",
      ""TriggerIdOffset"": 100,
      ""EventNameSuffix"": "":skill-a"",
      ""Bindings"": {{
        ""amount"": {{ ""Kind"": ""Const"", ""ConstValue"": 9 }}
      }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-module-template-test");

            Assert.That(database.TryGetPlanByTriggerId(triggerId + 100, out var plan), Is.True);
            Assert.That(plan.Actions[0].Arg0.ConstValue, Is.EqualTo(9));
            Assert.That(plan.Actions[0].Arg1.ConstValue, Is.EqualTo(2));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_ExpandsModuleScopedNodeReferences()
        {
            var triggerId = 3007;
            var actionId = StableStringId.Get("test:action_plan_validator:json_module_template_node_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Modules"": {{
    ""node-module"": {{
      ""Nodes"": {{
        ""leaf"": {{ ""Kind"": ""Action"", ""Action"": {{ ""ActionId"": {actionId}, ""Arity"": 0 }} }}
      }},
      ""Behaviors"": {{
        ""root"": {{ ""Kind"": ""Sequence"", ""Children"": [ {{ ""NodeRef"": ""leaf"" }} ] }}
      }},
      ""Triggers"": [
        {{
          ""TriggerId"": {triggerId},
          ""EventName"": ""test:action_plan_validator:json_module_node_event"",
          ""ExecutionRoot"": {{ ""BehaviorRef"": ""root"" }}
        }}
      ]
    }}
  }},
  ""ModuleInstances"": [
    {{ ""InstanceId"": ""skill-b"", ""ModuleId"": ""node-module"", ""TriggerIdOffset"": 100 }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-module-scoped-node-test");

            Assert.That(database.TryGetExecutionRootByTriggerId(triggerId + 100, out var root), Is.True);
            Assert.That(root.Kind, Is.EqualTo(ETriggerPlanExecutableKind.Sequence));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_RejectsMissingRequiredModuleBinding()
        {
            var triggerId = 3008;
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Templates"": {{
    ""required-module"": {{
      ""Parameters"": [ {{ ""Name"": ""amount"", ""Required"": true }} ],
      ""Triggers"": [
        {{
          ""TriggerId"": {triggerId},
          ""EventName"": ""test:action_plan_validator:json_required_module_event""
        }}
      ]
    }}
  }},
  ""TemplateInstances"": [
    {{ ""InstanceId"": ""skill-c"", ""TemplateId"": ""required-module"" }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            var ex = Assert.Throws<InvalidOperationException>(() => database.LoadFromJson(json, "json-module-required-test"));
            Assert.That(ex.Message, Does.Contain("Required module template parameter has no binding: amount"));
        }

        [Test]
        public void TriggerPlanJsonDatabase_LoadFromJson_RejectsCyclicBehaviorReferences()
        {
            var triggerId = 3005;
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Behaviors"": {{
    ""a"": {{ ""BehaviorRef"": ""b"" }},
    ""b"": {{ ""BehaviorRef"": ""a"" }}
  }},
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:action_plan_validator:json_cyclic_behavior_ref_event"",
      ""ExecutionRoot"": {{ ""BehaviorRef"": ""a"" }}
    }}
  ]
}}";
            var database = new TriggerPlanJsonDatabase();

            var ex = Assert.Throws<InvalidOperationException>(() => database.LoadFromJson(json, "json-cyclic-behavior-ref-test"));
            Assert.That(ex.Message, Does.Contain("Cyclic execution node reference detected"));
        }

        private static void AssertSemanticProjectionMatchesRawFields(ActionCallPlan call)
        {
            Assert.That(call.Arguments.Arity, Is.EqualTo(call.Arity));
            Assert.That(call.Arguments.Arg0, Is.EqualTo(call.Arg0));
            Assert.That(call.Arguments.Arg1, Is.EqualTo(call.Arg1));
            Assert.That(call.Arguments.NamedArgs, Is.SameAs(call.Args));
            Assert.That(call.Schedule.Mode, Is.EqualTo(call.ScheduleMode));
            Assert.That(call.Schedule.Param, Is.EqualTo(call.ScheduleParam));
            Assert.That(call.Schedule.MaxExecutions, Is.EqualTo(call.MaxExecutions));
            Assert.That(call.Schedule.CanBeInterrupted, Is.EqualTo(call.CanBeInterrupted));
            Assert.That(call.Execution.Policy, Is.EqualTo(call.ExecutionPolicy));
            Assert.That(call.Execution.RetryMaxRetries, Is.EqualTo(call.RetryMaxRetries));
            Assert.That(call.Execution.RetryDelayMs, Is.EqualTo(call.RetryDelayMs));
        }

        private static ValidationResult Validate(ActionCallPlan call)
        {
            var database = CreateDatabase(call);
            var validator = new ActionCallPlanValidator<Ping>();
            var context = ValidationContext<Ping>.CreateForDevelopment();
            return validator.Validate(in database, in context);
        }

        private static TriggerPlanDatabase<Ping> CreateDatabase(ActionCallPlan call)
        {
            var key = new EventKey<Ping>(StableStringId.Get("test:action_plan_validator:event"));
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 2000, actions: new[] { call });
            return new TriggerPlanDatabase<Ping>(new[]
            {
                new TriggerPlanEntry<Ping>(key, plan, id: "action-plan-validator")
            });
        }

        private sealed class Ping
        {
        }
    }
}
