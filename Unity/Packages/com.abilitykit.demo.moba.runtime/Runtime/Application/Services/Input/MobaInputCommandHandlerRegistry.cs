using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Marker;

/// <summary>
/// 文件名称: MobaInputCommandHandlerRegistry.cs
/// 
/// 功能描述: 扫描并管理基于 Attribute 注册的输入命令处理器。
/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 输入命令处理器注册表。
    /// </summary>
    public sealed class MobaInputCommandHandlerRegistry : MobaMarkerRegistryBase<IMobaInputCommandHandler>
    {
        private readonly Dictionary<int, IMobaInputCommandHandler> _handlers = new Dictionary<int, IMobaInputCommandHandler>();

        public MobaInputCommandHandlerRegistry() : base(4)
        {
        }

        /// <summary>
        /// 创建默认注册表并扫描已加载程序集。
        /// </summary>
        public static MobaInputCommandHandlerRegistry CreateDefault()
        {
            MobaInputCommandHandlerRegistry registry = new MobaInputCommandHandlerRegistry();
            MarkerScanner<MobaInputCommandHandlerAttribute>.ScanAll(registry);
            registry.RegisterBuiltInHandlers();
            return registry;
        }

        private void RegisterBuiltInHandlers()
        {
            Register((int)MobaOpCode.Move, typeof(MobaMoveInputCommandHandler));
            Register((int)MobaOpCode.SkillInput, typeof(MobaSkillInputCommandHandler));
        }

        /// <summary>
        /// 注册指定 OpCode 的处理器类型。
        /// </summary>
        public void Register(int opCode, Type implType)
        {
            if (!TryRegister(key: opCode, implType)) return;
            IMobaInputCommandHandler handler = Activator.CreateInstance(implType) as IMobaInputCommandHandler;
            if (handler == null) return;
            _handlers[opCode] = handler;
        }

        /// <summary>
        /// 尝试处理输入命令。
        /// </summary>
        public bool TryHandle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command)
        {
            if (!_handlers.TryGetValue(command.OpCode, out IMobaInputCommandHandler handler))
            {
                Log.Warning($"[MobaInputCommandHandlerRegistry] Missing input handler: OpCode={command.OpCode}");
                return false;
            }

            handler.Handle(context, frame, command);
            return true;
        }
    }
}
