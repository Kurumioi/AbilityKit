using System;
using AbilityKit.Core.Logging;
using AbilityKit.Game.EntityCreation;

namespace AbilityKit.Game
{
    public sealed class GameEntryBootstrap : IGameEntryModule
    {
        public string Id => "game.entry.bootstrap";

        public void OnAttach(in GameEntryModuleContext ctx)
        {
            if (!ctx.Root.IsValid) return;

            TryInstallUnityLogSink();

            var entry = ctx.Entry;

            if (!entry.TryGet(out GameManager gm))
            {
                gm = new GameManager();
                entry.Set(gm);
            }

            gm.EnterGame();

            const int SystemsNodeId = 1;
            var systems = entry.GetNode(SystemsNodeId);
            if (!systems.IsValid)
            {
                systems = EntityGenerator.CreateChild(entry.Root, SystemsNodeId, "SystemsNode");
            }

            systems.WithRef(new SystemsTag());
            systems.WithRef(new SystemsInfo());
        }

        public void OnDetach(in GameEntryModuleContext ctx)
        {
            if (ctx.Root.IsValid && ctx.Entry.TryGet(out GameManager gm))
            {
                gm.LeaveGame();
            }
        }

        private static void TryInstallUnityLogSink()
        {
            try
            {
                var type = Type.GetType("AbilityKit.Examples.Common.Log.UnityLogSink, AbilityKit.Demo.Moba.View.Runtime");
                if (type == null) return;
                if (!typeof(ILogSink).IsAssignableFrom(type)) return;

                var sink = Activator.CreateInstance(type) as ILogSink;
                if (sink == null) return;
                Log.SetSink(sink);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private sealed class SystemsTag
        {
        }
        private sealed class SystemsInfo
        {
        }
    }
}
