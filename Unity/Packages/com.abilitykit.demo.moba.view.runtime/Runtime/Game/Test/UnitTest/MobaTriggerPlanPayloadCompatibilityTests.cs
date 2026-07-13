using System;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config.Plans;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MobaTriggerPlanPayloadCompatibilityTests
    {
        private const string EventName = "test.payload.compatibility";
        private const int TriggerId = 990001;

        [Test]
        public void ValidateDatabase_RejectsFieldUnsupportedByConcreteEventArgs()
        {
            var report = ValidatePayloadField<AttackInfo>(
                MobaBattlePayloadAccessor.SupportsAttackInfoField,
                MobaBattlePayloadFields.DamageValue);

            AssertReportContainsCode(report, "moba.trigger.plan.payload_field_incompatible");
            Assert.IsTrue(report.ShouldBlockStartup);
        }

        [Test]
        public void ValidateDatabase_AcceptsFieldSupportedByConcreteEventArgs()
        {
            var report = ValidatePayloadField<DamageResult>(
                MobaBattlePayloadAccessor.SupportsDamageResultField,
                MobaBattlePayloadFields.DamageValue);

            AssertReportDoesNotContainCode(report, "moba.trigger.plan.payload_field_incompatible");
            Assert.AreEqual(0, report.ErrorCount, report.FormatAllEntries());
        }

        private static MobaRuntimeValidationReport ValidatePayloadField<TArgs>(
            Func<int, bool> supportsField,
            string fieldName)
        {
            var fieldId = MobaBattlePayloadFields.FieldId(fieldName);
            var plan = new TriggerPlan<object>(
                phase: 0,
                priority: 0,
                triggerId: TriggerId,
                predicateId: new FunctionId(1),
                predicateArgs: new[] { NumericValueRef.PayloadField(fieldId) },
                actions: Array.Empty<ActionCallPlan>());

            var database = new TriggerPlanJsonDatabase();
            var record = new TriggerPlanJsonDatabase.Record(
                TriggerId,
                EventName,
                StableStringId.Get("event:" + EventName),
                TriggerPlanScope.OwnerBound,
                in plan);
            database.AddRecord(in record);

            var eventRegistry = new MobaEventSubscriptionRegistry();
            eventRegistry.RegisterExact<TArgs>(EventName);

            var payloadRegistry = new PayloadAccessorRegistry();
            var battleAccessor = new MobaBattlePayloadAccessor();
            if (typeof(TArgs) == typeof(AttackInfo))
            {
                payloadRegistry.RegisterIntAccessor<AttackInfo>(battleAccessor, supportsField);
            }
            else if (typeof(TArgs) == typeof(DamageResult))
            {
                payloadRegistry.RegisterIntAccessor<DamageResult>(battleAccessor, supportsField);
                payloadRegistry.RegisterDoubleAccessor<DamageResult>(battleAccessor, supportsField);
            }
            else
            {
                Assert.Fail("Unsupported test event args type: " + typeof(TArgs).Name);
            }

            var report = new MobaRuntimeValidationReport();
            MobaTriggerPlanIntegrityValidator.ValidateDatabase(
                database,
                eventRegistry,
                payloadRegistry,
                report);
            return report;
        }

        private static void AssertReportContainsCode(MobaRuntimeValidationReport report, string code)
        {
            for (var i = 0; i < report.Entries.Count; i++)
            {
                if (string.Equals(report.Entries[i].Code, code, StringComparison.Ordinal)) return;
            }

            Assert.Fail("Expected validation code was not reported: " + code + Environment.NewLine + report.FormatAllEntries());
        }

        private static void AssertReportDoesNotContainCode(MobaRuntimeValidationReport report, string code)
        {
            for (var i = 0; i < report.Entries.Count; i++)
            {
                Assert.AreNotEqual(code, report.Entries[i].Code, report.FormatAllEntries());
            }
        }
    }
}
