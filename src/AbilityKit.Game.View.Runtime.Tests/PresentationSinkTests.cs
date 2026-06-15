using System.Collections.Generic;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// Step 4.6 验证：IPresentationSink 事件桥接契约。
    /// - RecordingSink 正确记录所有推送的事件；
    /// - 接口方法签名与 MobaRootState / MobaBattleState 枚举匹配；
    /// - 双向通信契约成立（IFlowCommandSink View→Logic，IPresentationSink Logic→View）。
    /// </summary>
    public sealed class PresentationSinkTests
    {
        [Fact]
        public void RecordingSink_CapturesAllEvents()
        {
            var sink = new RecordingSink();

            sink.OnPhaseChanged(MobaRootState.Boot, default);
            sink.OnPhaseChanged(MobaRootState.Battle, MobaBattleState.Prepare);
            sink.OnBattleStart();
            sink.OnBattleEnd();
            sink.OnError("test error");

            Assert.Equal(5, sink.Events.Count);
            Assert.Equal("PhaseChanged:Root=Boot,Battle=Prepare", sink.Events[0]);
            Assert.Equal("PhaseChanged:Root=Battle,Battle=Prepare", sink.Events[1]);
            Assert.Equal("BattleStart", sink.Events[2]);
            Assert.Equal("BattleEnd", sink.Events[3]);
            Assert.Equal("Error:test error", sink.Events[4]);
        }

        [Fact]
        public void PresentationSink_Interface_HasFourMethods()
        {
            var interfaceType = typeof(IPresentationSink);

            Assert.NotNull(interfaceType.GetMethod(nameof(IPresentationSink.OnPhaseChanged)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPresentationSink.OnBattleStart)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPresentationSink.OnBattleEnd)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPresentationSink.OnError)));
        }

        [Fact]
        public void FlowCommandSink_And_PresentationSink_FormBidirectionalContract()
        {
            // IFlowCommandSink: View → Logic（命令方向）
            var commandSinkType = typeof(IFlowCommandSink);
            Assert.NotNull(commandSinkType.GetMethod(nameof(IFlowCommandSink.RequestEnterBattle)));
            Assert.NotNull(commandSinkType.GetMethod(nameof(IFlowCommandSink.RequestBattleEnd)));
            Assert.NotNull(commandSinkType.GetMethod(nameof(IFlowCommandSink.RequestReturnLobby)));

            // IPresentationSink: Logic → View（事件方向）
            var presentationSinkType = typeof(IPresentationSink);
            Assert.NotNull(presentationSinkType.GetMethod(nameof(IPresentationSink.OnPhaseChanged)));
            Assert.NotNull(presentationSinkType.GetMethod(nameof(IPresentationSink.OnBattleStart)));

            // 两个接口共存于同一命名空间，构成双向通信契约
            Assert.Equal("AbilityKit.Game.Flow", commandSinkType.Namespace);
            Assert.Equal("AbilityKit.Game.Flow", presentationSinkType.Namespace);
        }

        /// <summary>
        /// 测试用 IPresentationSink 实现，记录所有调用用于断言。
        /// </summary>
        private sealed class RecordingSink : IPresentationSink
        {
            public List<string> Events { get; } = new List<string>();

            public void OnPhaseChanged(MobaRootState root, MobaBattleState battle)
            {
                Events.Add($"PhaseChanged:Root={root},Battle={battle}");
            }

            public void OnBattleStart()
            {
                Events.Add("BattleStart");
            }

            public void OnBattleEnd()
            {
                Events.Add("BattleEnd");
            }

            public void OnError(string message)
            {
                Events.Add($"Error:{message}");
            }
        }
    }
}
