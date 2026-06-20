using System;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Services.Triggering
{
    /// <summary>
    /// owner-bound 触发器执行门控：用于执行前必须检查的运行时状态，以及触发器成功返回后才提交的状态变更。
    /// </summary>
    public interface IMobaOwnerBoundTriggerGate
    {
        bool IsMatch(long ownerKey, int triggerId);
        bool CanExecute(long ownerKey, int triggerId);
        void Complete(long ownerKey, int triggerId);
    }
}
