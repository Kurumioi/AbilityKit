using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudCanvasController : IDisposable
    {
        private readonly BattleHudEventSystemController _eventSystem;

        public BattleHudCanvasController(BattleHudEventSystemController eventSystem = null)
        {
            _eventSystem = eventSystem ?? new BattleHudEventSystemController();
        }

        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }

        public void Create(string name)
        {
            Destroy();

            var go = new GameObject(name);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            Root = Canvas.GetComponent<RectTransform>();

            _eventSystem.Ensure();
        }

        public void Dispose()
        {
            Destroy();
        }

        private void Destroy()
        {
            if (Canvas != null)
            {
                UnityEngine.Object.Destroy(Canvas.gameObject);
            }

            Canvas = null;
            Root = null;
        }

    }

    internal sealed class BattleHudEventSystemController
    {
        public void Ensure()
        {
            if (EventSystem.current != null) return;
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.hideFlags = HideFlags.DontSave;
        }
    }
}
