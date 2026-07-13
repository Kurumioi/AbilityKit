using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Triggering.Validation;
using NUnit.Framework;

namespace AbilityKit.Triggering.Tests
{
    public sealed class ValidatingTriggerPlanJsonDatabaseExecutionRootTests
    {
        [Test]
        public void Validate_RejectsTimelineActionInsideJsonExecutionRoot()
        {
            var triggerId = 4101;
            var actionId = StableStringId.Get("test:validating_json_execution_root:timeline_action");
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:validating_json_execution_root:event:timeline_action"",
      ""ExecutionRoot"": {{
        ""Kind"": ""Action"",
        ""Action"": {{
          ""ActionId"": {actionId},
          ""ScheduleMode"": ""Timeline"",
          ""MaxExecutions"": -1
        }}
      }}
    }}
  ]
}}";
            var database = new ValidatingTriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-execution-root-timeline-test");
            var result = database.Validate();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationIssue>(issue => issue.Code == ValidationErrorCodes.UNSUPPORTED_ACTION_SCHEDULE));
        }

        [Test]
        public void Validate_WarnsForEmptyCompositeJsonExecutionRoot()
        {
            var triggerId = 4102;
            var json = $@"
{{
  ""FormatVersion"": 1,
  ""Triggers"": [
    {{
      ""TriggerId"": {triggerId},
      ""EventName"": ""test:validating_json_execution_root:event:empty_sequence"",
      ""ExecutionRoot"": {{
        ""Kind"": ""Sequence"",
        ""Children"": []
      }}
    }}
  ]
}}";
            var database = new ValidatingTriggerPlanJsonDatabase();

            database.LoadFromJson(json, "json-execution-root-empty-sequence-test");
            var result = database.Validate();

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Matches<ValidationIssue>(issue => issue.Code == ValidationErrorCodes.EMPTY_EXECUTION_NODE));
        }
        [Test]
        public void LoadFromJson_AcceptsSourceTriggerArrayRoot()
        {
            var actionId = StableStringId.Get("test:array_source_root:debug");
            var json = $@"
[
  {{
    ""id"": 4201,
    ""event"": ""test:array_source_root:event"",
    ""enabled"": true,
    ""actions"": [
      {{ ""type"": ""debug_log"", ""id"": {actionId} }}
    ]
  }}
]";
            var database = new ValidatingTriggerPlanJsonDatabase();

            database.LoadFromJson(json, "array-source-root-test");

            Assert.That(database.InnerDatabase.TryGetPlanByTriggerId(4201, out var plan), Is.True);
            Assert.That(plan.Actions, Has.Length.EqualTo(1));
        }
    }
}
