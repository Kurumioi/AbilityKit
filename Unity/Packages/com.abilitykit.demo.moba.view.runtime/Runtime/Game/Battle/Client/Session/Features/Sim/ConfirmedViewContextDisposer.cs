using System;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal static class ConfirmedViewContextDisposer
    {
        public static void Dispose(BattleContext ctx, Action<IEntity> destroyEntityTree)
        {
            if (ctx == null) return;

            ctx.ClearSnapshotRouting();

            if (ctx.EntityNode.IsValid)
            {
                destroyEntityTree?.Invoke(ctx.EntityNode);
            }

            ctx.EntityLookup?.Clear();
            BattleContext.Return(ctx);
        }
    }
}
