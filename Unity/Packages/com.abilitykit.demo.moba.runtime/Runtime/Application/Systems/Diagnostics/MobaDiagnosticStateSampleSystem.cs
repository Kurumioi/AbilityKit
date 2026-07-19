using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Diagnostics
{
    /// <summary>
    /// 诊断状态采样系统：在每帧 PostExecute/Late 阶段（所有业务系统和清理系统之后）
    /// 调用 <see cref="MobaBattleDiagnosticStateSampler.Sample"/>，将当前帧 World/Actor 状态
    /// 写入平台无关的 <see cref="IBattleDiagnosticStateStore"/>。
    ///
    /// 采样器或依赖缺失时静默跳过，不传播异常，不影响战斗主循环。
    /// </summary>
    [WorldSystem(order: MobaSystemOrder.DiagnosticStateSample, Phase = WorldSystemPhase.PostExecute)]
    public sealed class MobaDiagnosticStateSampleSystem : WorldSystemBase
    {
        private MobaBattleDiagnosticStateSampler _sampler;

        public MobaDiagnosticStateSampleSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            // 采样器是可选依赖：诊断未启用或 Scope 未建立时跳过。
            Services.TryResolve(out _sampler);
        }

        protected override void OnExecute()
        {
            if (_sampler == null) return;

            try
            {
                _sampler.Sample();
            }
            catch
            {
                // 诊断采样异常不得影响战斗主循环。
            }
        }
    }
}
