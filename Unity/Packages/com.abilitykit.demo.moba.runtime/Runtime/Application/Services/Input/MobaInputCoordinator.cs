using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.LogicWorld;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 逻辑世界输入协调器：负责玩法侧上下文构建和命令处理器分发。
    /// </summary>
    [WorldService(typeof(IWorldInputSink))]
    [WorldService(typeof(IMobaInputCoordinator))]
    [WorldService(typeof(MobaInputCoordinator))]
    public sealed class MobaInputCoordinator : LogicWorldInputCoordinatorBase<MobaInputCommandContext>, IMobaInputCoordinator, IWorldInputSink
    {
        private readonly MobaLogicWorldRunGateService _phase;
        private readonly MobaPlayerActorMapService _playerActorMap;
        private readonly MobaEntityManager _entities;
        private readonly MobaInputCommandContractRegistry _contracts;
        private readonly MobaInputCommandHandlerRegistry _handlers;

        private SkillExecutor _skills;

        public MobaInputCoordinator(MobaLogicWorldRunGateService phase, MobaPlayerActorMapService playerActorMap, MobaEntityManager entities, MobaInputCommandContractRegistry contracts)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _playerActorMap = playerActorMap ?? throw new ArgumentNullException(nameof(playerActorMap));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
            _handlers = _contracts.HandlerRegistry;
        }

        protected override void OnServicesReady(IWorldResolver services)
        {
            if (_skills != null) return;
            if (services == null) return;

            ResolveSkillExecutor(services);
        }

        protected override MobaInputCommandContext CreateContext(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            return new MobaInputCommandContext(_phase, _playerActorMap, _entities, _skills, Services);
        }

        protected override bool Dispatch(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command, out MobaInputCommandResult result)
        {
            return _handlers.TryHandle(context, frame, command, out result);
        }

        private void ResolveSkillExecutor(IWorldResolver services)
        {
            try
            {
                _skills = services.Resolve<SkillExecutor>();
                if (_skills == null)
                {
                    MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "SkillExecutor resolved as null.");
                }
            }
            catch (Exception ex)
            {
                MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Exception, nameof(MobaInputCoordinator), "Failed to resolve SkillExecutor.");
                LogResolveDiagnostics(services);
            }
        }

        private static void LogResolveDiagnostics(IWorldResolver services)
        {
            if (services is IWorldServiceContainer c)
            {
                MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), $"Registered: SkillExecutor={c.IsRegistered(typeof(SkillExecutor))}, IFrameTime={c.IsRegistered(typeof(IFrameTime))}, IUnitResolver={c.IsRegistered(typeof(AbilityKit.Ability.Share.ECS.IUnitResolver))}, IMobaSkillPipelineLibrary={c.IsRegistered(typeof(IMobaSkillPipelineLibrary))}, IWorldClock={c.IsRegistered(typeof(IWorldClock))}, IEventBus={c.IsRegistered(typeof(AbilityKit.Triggering.Eventing.IEventBus))}");

                if (services.TryResolve(typeof(IWorldClock), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: IWorldClock");
                if (services.TryResolve(typeof(IFrameTime), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: IFrameTime");
                if (services.TryResolve(typeof(AbilityKit.Triggering.Eventing.IEventBus), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: IEventBus");
                if (services.TryResolve(typeof(AbilityKit.Ability.Share.ECS.IUnitResolver), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: IUnitResolver");
                if (services.TryResolve(typeof(MobaSkillLoadoutService), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: MobaSkillLoadoutService");
                if (services.TryResolve(typeof(MobaActorLookupService), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: MobaActorLookupService");
                if (services.TryResolve(typeof(IMobaSkillPipelineLibrary), out _) == false) MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, nameof(MobaInputCoordinator), "Resolve check failed: IMobaSkillPipelineLibrary");
            }

            TryResolveForLog<IMobaSkillPipelineLibrary>(services, "IMobaSkillPipelineLibrary");
            TryResolveForLog<MobaConfigDatabase>(services, "MobaConfigDatabase");
            TryResolveForLog<MobaEffectExecutionService>(services, "MobaEffectExecutionService");
            TryResolveForLog<AbilityKit.Triggering.Eventing.IEventBus>(services, "IEventBus");
        }

        private static void TryResolveForLog<T>(IWorldResolver services, string name) where T : class
        {
            try
            {
                services.Resolve<T>();
            }
            catch (Exception ex)
            {
                MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Exception, nameof(MobaInputCoordinator), $"{name} resolve failed.");
            }
        }

    }
}
