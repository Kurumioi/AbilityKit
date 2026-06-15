using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Requests;
using AbilityKit.World.ECS;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleDebugOnGUIFeature : IGamePhaseFeature, IOnGUIFeature
    {
        private BattleContext _ctx;

        private Vector2 _scroll;

        private bool _showFrameSyncStats;

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Root.TryGetRef(out _ctx);
            BattleFlowDebugProvider.Current = _ctx;
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            if (ReferenceEquals(BattleFlowDebugProvider.Current, _ctx))
            {
                BattleFlowDebugProvider.Current = null;
            }
            _ctx = null;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }

        public void OnGUI(in GamePhaseContext ctx)
        {
#if UNITY_EDITOR
            if (!ctx.Entry.DebugEnabled) return;

            var sink = ctx.Entry.Get<IFlowCommandSink>();
            if (sink == null || sink.CurrentRootPhase != MobaRootState.Battle) return;

            GUILayout.BeginArea(new Rect(10, 10, 170, 110), GUI.skin.window);
            if (GUILayout.Button("Exit Battle", GUILayout.Height(34)))
            {
                sink.RequestReturnLobby();
            }

            if (GUILayout.Button("Rebind Views", GUILayout.Height(34)))
            {
                if (ctx.Root.IsValid)
                {
                    if (ctx.Root.TryGetRef(out BattleViewFeature view) && view != null)
                    {
                        view.RebindAll();
                    }
                    if (ctx.Root.TryGetRef(out ConfirmedBattleViewFeature confirmed) && confirmed != null)
                    {
                        confirmed.RebindAll();
                    }
                }
            }
            GUILayout.EndArea();
#endif
        }
    }
}
