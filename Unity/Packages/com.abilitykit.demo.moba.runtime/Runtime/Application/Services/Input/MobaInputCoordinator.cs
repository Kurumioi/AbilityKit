using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.LogicWorld;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 逻辑世界输入协调器：保留玩法侧上下文构建、热更路由和命令处理器分发。
    /// </summary>
    [WorldService(typeof(IWorldInputSink))]
    [WorldService(typeof(IMobaInputCoordinator))]
    [WorldService(typeof(MobaInputCoordinator))]
    public sealed class MobaInputCoordinator : LogicWorldInputCoordinatorBase<MobaInputCommandContext>, IMobaInputCoordinator, IWorldInputSink
    {
        private readonly MobaGamePhaseService _phase;
        private readonly MobaPlayerActorMapService _playerActorMap;
        private readonly MobaEntityManager _entities;
        private readonly MobaInputCommandHandlerRegistry _handlers;

        private SkillExecutor _skills;

        public MobaInputCoordinator(MobaGamePhaseService phase, MobaPlayerActorMapService playerActorMap, MobaEntityManager entities)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _playerActorMap = playerActorMap ?? throw new ArgumentNullException(nameof(playerActorMap));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _handlers = MobaInputCommandHandlerRegistry.CreateDefault();
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

        protected override bool TryHandleBeforeDispatch(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command)
        {
            if (Services == null) return false;
            if (!Services.TryResolve<IMobaLobbyInputHotfixRouter>(out var router) || router == null) return false;

            try
            {
                return router.TryHandle(Services, frame, command);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaInputCoordinator] Hotfix router TryHandle failed.");
                return false;
            }
        }

        protected override bool Dispatch(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command, out string failureReason)
        {
            bool handled = _handlers.TryHandle(context, frame, command, out failureReason);
            if (!handled)
            {
                Log.Warning($"[MobaInputCoordinator] Dispatch rejected. Frame={frame.Value}, Player={command.Player.Value}, OpCode={command.OpCode}, Payload={command.Payload?.Length ?? 0}, HandlerCount={_handlers.HandlerCount}, Reason={failureReason ?? string.Empty}");
            }

            return handled;
        }

        private void ResolveSkillExecutor(IWorldResolver services)
        {
            try
            {
                _skills = services.Resolve<SkillExecutor>();
                if (_skills == null)
                {
                    Log.Error("[MobaInputCoordinator] SkillExecutor resolved as null.");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaInputCoordinator] Failed to resolve SkillExecutor.");
                LogResolveDiagnostics(services);
            }
        }

        private static void LogResolveDiagnostics(IWorldResolver services)
        {
            if (services is IWorldServiceContainer c)
            {
                Log.Error($"[MobaInputCoordinator] Registered: SkillExecutor={c.IsRegistered(typeof(SkillExecutor))}, IFrameTime={c.IsRegistered(typeof(IFrameTime))}, IUnitResolver={c.IsRegistered(typeof(AbilityKit.Ability.Share.ECS.IUnitResolver))}, IMobaSkillPipelineLibrary={c.IsRegistered(typeof(IMobaSkillPipelineLibrary))}, IWorldClock={c.IsRegistered(typeof(IWorldClock))}, IEventBus={c.IsRegistered(typeof(AbilityKit.Triggering.Eventing.IEventBus))}");

                if (services.TryResolve(typeof(IWorldClock), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: IWorldClock");
                if (services.TryResolve(typeof(IFrameTime), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: IFrameTime");
                if (services.TryResolve(typeof(AbilityKit.Triggering.Eventing.IEventBus), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: IEventBus");
                if (services.TryResolve(typeof(AbilityKit.Ability.Share.ECS.IUnitResolver), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: IUnitResolver");
                if (services.TryResolve(typeof(MobaSkillLoadoutService), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: MobaSkillLoadoutService");
                if (services.TryResolve(typeof(MobaActorLookupService), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: MobaActorLookupService");
                if (services.TryResolve(typeof(IMobaSkillPipelineLibrary), out _) == false) Log.Error("[MobaInputCoordinator] Resolve check failed: IMobaSkillPipelineLibrary");
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
                Log.Exception(ex, $"[MobaInputCoordinator] {name} resolve failed.");
            }
        }

    }
}
