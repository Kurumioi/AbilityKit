using System;
using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// 事件总线测试
    /// </summary>
    public static class EventBusTests
    {
        public static TestResult Run()
        {
            var result = new TestResult { Name = "EventBus" };

            try
            {
                BattleEventBus.Clear();

                result = TestBasicSubscribeAndPublish();
                if (!result.Passed) return result;

                result = TestUnsubscribe();
                if (!result.Passed) return result;

                result = TestMultipleHandlers();
                if (!result.Passed) return result;

                result = TestClear();
                if (!result.Passed) return result;

                BattleEventBus.Clear();
                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private static TestResult TestBasicSubscribeAndPublish()
        {
            var result = new TestResult { Name = "EventBus.BasicSubscribeAndPublish" };
            BattleEventBus.Clear();

            bool eventReceived = false;
            BattleEventBus.Subscribe<FrameSyncEvent>(evt =>
            {
                eventReceived = true;
            });

            BattleEventBus.Publish(new FrameSyncEvent { Frame = 100, ActorCount = 5 });

            if (!eventReceived)
            {
                result.Fail("Event was not received");
                return result;
            }

            BattleEventBus.Unsubscribe<FrameSyncEvent>(null);
            BattleEventBus.Clear();
            result.Pass();
            return result;
        }

        private static TestResult TestUnsubscribe()
        {
            var result = new TestResult { Name = "EventBus.Unsubscribe" };
            BattleEventBus.Clear();

            int callCount = 0;
            Action<FrameSyncEvent> handler = evt => { callCount++; };

            BattleEventBus.Subscribe(handler);
            BattleEventBus.Publish(new FrameSyncEvent { Frame = 1 });
            BattleEventBus.Unsubscribe(handler);
            BattleEventBus.Publish(new FrameSyncEvent { Frame = 2 });

            if (callCount != 1)
            {
                result.Fail($"Expected 1 call, got {callCount}");
                return result;
            }

            BattleEventBus.Clear();
            result.Pass();
            return result;
        }

        private static TestResult TestMultipleHandlers()
        {
            var result = new TestResult { Name = "EventBus.MultipleHandlers" };
            BattleEventBus.Clear();

            int count1 = 0, count2 = 0;
            BattleEventBus.Subscribe<FrameSyncEvent>(evt => { count1++; });
            BattleEventBus.Subscribe<FrameSyncEvent>(evt => { count2++; });

            BattleEventBus.Publish(new FrameSyncEvent { Frame = 1 });

            if (count1 != 1 || count2 != 1)
            {
                result.Fail($"Expected both 1, got {count1} and {count2}");
                return result;
            }

            BattleEventBus.Clear();
            result.Pass();
            return result;
        }

        private static TestResult TestClear()
        {
            var result = new TestResult { Name = "EventBus.Clear" };
            BattleEventBus.Clear();

            bool received = false;
            BattleEventBus.Subscribe<FrameSyncEvent>(evt => { received = true; });
            BattleEventBus.Clear();
            BattleEventBus.Publish(new FrameSyncEvent { Frame = 1 });

            if (received)
            {
                result.Fail("Event received after Clear");
                return result;
            }

            result.Pass();
            return result;
        }
    }
}
