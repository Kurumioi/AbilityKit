using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Game.Battle;

namespace AbilityKit.Game.Editor.Diagnostics
{
    /// <summary>
    /// 共享辅助：从 <see cref="BattleDebugContext.Facade"/> 解析出
    /// <see cref="IBattleDiagnosticReadOnlySession"/>，供诊断面板复用。
    /// 遵循现有 Facade → Session → World → Services 解析路径，
    /// 不建立旁路数据源，只消费已注册的 Local Session 查询表面。
    /// </summary>
    internal static class BattleDebugDiagnosticSessionResolver
    {
        public static bool TryResolve(in BattleDebugContext ctx, out IBattleDiagnosticReadOnlySession session)
        {
            session = null;
            var facade = ctx.Facade;
            if (facade == null) return false;
            if (!facade.TryGetSession(out var logicSession)) return false;
            if (!logicSession.TryGetWorld(out var world) || world == null) return false;

            var services = world.Services;
            if (services == null) return false;

            return services.TryResolve(out session) && session != null;
        }
    }
}
