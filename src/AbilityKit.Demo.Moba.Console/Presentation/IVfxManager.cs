using System;
using AbilityKit.Core.Math;
using Color4 = AbilityKit.Demo.Moba.Console.Presentation.Color4;

namespace AbilityKit.Demo.Moba.Console.Presentation
{
    /// <summary>
    /// VFX 管理器接口 (Console 平台实现)
    /// 定义特效管理的契约
    /// </summary>
    public interface IVfxManager
    {
        bool TryCreateVfx(int vfxId, int followId, in Vec3 position, out IVfxHandle vfxHandle);
        void DestroyVfx(IVfxHandle vfxHandle);
        void UpdateVfxPosition(IVfxHandle vfxHandle, in Vec3 position);
        void SetVfxFollowTarget(IVfxHandle vfxHandle, int followId);
        void PlayVfx(IVfxHandle vfxHandle);
        void StopVfx(IVfxHandle vfxHandle);
        void DestroyAll();
    }

    public interface IVfxHandle : IDisposable
    {
        int VfxId { get; }
        bool IsValid { get; }
        int HandleId { get; }
    }

    public interface IFloatingTextManager
    {
        void Spawn(int parentId, string text, in Vec3 position, in Color4 color);
        void Spawn(int parentId, string text, in Vec3 position, bool isHeal);
        void ClearAll();
    }
}
