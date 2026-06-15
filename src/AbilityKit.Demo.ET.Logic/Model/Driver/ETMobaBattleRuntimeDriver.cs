using System;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Platform-independent battle driver adapter owned by the ET host component.
    /// The ET entity keeps lifecycle and scene context; this object exposes the formal IBattleDriver port.
    /// </summary>
    public sealed class ETMobaBattleRuntimeDriver : IBattleDriver
    {
        private readonly ETMobaBattleDriver _host;

        public ETMobaBattleRuntimeDriver(ETMobaBattleDriver host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public ETMobaBattleDriver Host => _host;

        public int CurrentFrame => _host.CurrentFrame;

        public double LogicTimeSeconds => _host.LogicTimeSeconds;

        public int TickRate => _host.TickRate;

        public bool IsRunning => _host.IsRunning;

        public IBattleViewEventSink ViewEventSink
        {
            get => _host.ViewSink;
            set => _host.ViewSink = value;
        }

        public BattleStartPlan Plan => _host.Plan;

        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            _host.Initialize(plan, viewSink);
        }

        public void Start()
        {
            _host.Start();
        }

        public void Stop()
        {
            _host.Stop();
        }

        public void Destroy()
        {
            _host.Destroy();
        }

        public void Tick(float deltaTime)
        {
            _host.Tick(deltaTime);
        }
    }
}
