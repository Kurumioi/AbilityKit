using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionEditorHooksSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_editor_hooks";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
#if UNITY_EDITOR
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionEditorHooksRuntime>(ctx, out var runtime)) return;
            runtime.TryInstallEditorPlayModeStopHook();
#endif
        }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
#if UNITY_EDITOR
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionEditorHooksRuntime>(ctx, out var runtime)) return;
            runtime.TryUninstallEditorPlayModeStopHook();
#endif
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
