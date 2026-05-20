using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Battle.Features;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// 视图 SubFeature - 管理视图组件生命周期
    /// </summary>
    public sealed class ConsoleViewFeatureSubFeature : ConsoleSubFeatureBase
    {
        public override string Id => "console_view_feature";
        public override string[] Dependencies => Array.Empty<string>();

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            Log.System("[ViewFeature] Attached");
        }

        public override void OnDetach(ConsoleBattleContext ctx)
        {
            Log.System("[ViewFeature] Detached");
        }
    }

    /// <summary>
    /// 插值 SubFeature - 管理视图插值Tick
    /// </summary>
    public sealed class ConsoleInterpolationFeatureSubFeature : ConsoleSubFeatureBase
    {
        public override string Id => "console_interpolation_feature";
        public override string[] Dependencies => new[] { "console_view_feature" };

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            Log.System("[InterpolationFeature] Attached");
        }
    }

    /// <summary>
    /// 飘字 SubFeature - 管理飘字系统Tick
    /// </summary>
    public sealed class ConsoleFloatingTextFeatureSubFeature : ConsoleSubFeatureBase
    {
        public override string Id => "console_floating_text_feature";
        public override string[] Dependencies => new[] { "console_view_feature" };

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            Log.System("[FloatingTextFeature] Attached");
        }
    }
}
