#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    internal sealed class UnityShooterPlayInputSource : IShooterPlayInputSource
    {
        public ShooterPlayFrameInput ReadInput(int controlledPlayerId)
        {
            return new ShooterPlayFrameInput(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                0f,
                1f,
                Input.GetKey(KeyCode.Space));
        }
    }

    internal sealed class UnityShooterGameObjectViewSink : IShooterPlayViewSink
    {
        private readonly Dictionary<int, GameObject> _playerViews = new();
        private readonly Dictionary<int, GameObject> _bulletViews = new();
        private readonly HashSet<int> _seenPlayers = new();
        private readonly HashSet<int> _seenBullets = new();
        private Transform? _viewRoot;

        public void Render(in ShooterPlayPresentationFrame frame)
        {
            EnsureViewRoot();
            var clientBatch = frame.ClientBatch;
            RenderBatch(in clientBatch, frame.ControlledPlayerId, frame.WorldScale);
        }

        public void Clear()
        {
            foreach (var kvp in _playerViews)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }

            foreach (var kvp in _bulletViews)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }

            _playerViews.Clear();
            _bulletViews.Clear();

            if (_viewRoot != null)
            {
                Object.Destroy(_viewRoot.gameObject);
                _viewRoot = null;
            }
        }

        private void RenderBatch(in ShooterSnapshotViewBatch batch, int controlledPlayerId, float worldScale)
        {
            _seenPlayers.Clear();
            _seenBullets.Clear();

            for (var i = 0; i < batch.EntityChangeCount; i++)
            {
                var entity = batch.EntityChanges[i];
                if (!entity.Alive)
                {
                    continue;
                }

                if (entity.Kind == ShooterViewEntityKind.Player)
                {
                    _seenPlayers.Add(entity.EntityId);
                }
                else if (entity.Kind == ShooterViewEntityKind.Bullet)
                {
                    _seenBullets.Add(entity.EntityId);
                }
            }

            for (var i = 0; i < batch.TransformChanges.Count; i++)
            {
                var transform = batch.TransformChanges[i];
                if (transform.Key.Kind == ShooterViewEntityKind.Player && _seenPlayers.Contains(transform.Key.EntityId))
                {
                    var view = GetOrCreatePlayerView(transform.Key.EntityId, controlledPlayerId);
                    view.transform.localPosition = new Vector3(transform.X * worldScale, 0f, transform.Y * worldScale);
                }
                else if (transform.Key.Kind == ShooterViewEntityKind.Bullet && _seenBullets.Contains(transform.Key.EntityId))
                {
                    var view = GetOrCreateBulletView(transform.Key.EntityId);
                    view.transform.localPosition = new Vector3(transform.X * worldScale, 0f, transform.Y * worldScale);
                }
            }

            PruneViews(_playerViews, _seenPlayers);
            PruneViews(_bulletViews, _seenBullets);
        }

        private GameObject GetOrCreatePlayerView(int id, int controlledPlayerId)
        {
            if (_playerViews.TryGetValue(id, out var existing) && existing != null)
            {
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"ShooterPlayer_{id}";
            go.transform.SetParent(_viewRoot, false);
            TintRenderer(go, id == controlledPlayerId ? Color.green : Color.cyan);
            _playerViews[id] = go;
            return go;
        }

        private GameObject GetOrCreateBulletView(int id)
        {
            if (_bulletViews.TryGetValue(id, out var existing) && existing != null)
            {
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"ShooterBullet_{id}";
            go.transform.localScale = Vector3.one * 0.35f;
            go.transform.SetParent(_viewRoot, false);
            TintRenderer(go, Color.yellow);
            _bulletViews[id] = go;
            return go;
        }

        private void EnsureViewRoot()
        {
            if (_viewRoot != null)
            {
                return;
            }

            var root = new GameObject("ShooterPlayModeViews");
            Object.DontDestroyOnLoad(root);
            _viewRoot = root.transform;
        }

        private static void PruneViews(Dictionary<int, GameObject> views, HashSet<int> alive)
        {
            if (views.Count == 0)
            {
                return;
            }

            var stale = new List<int>();
            foreach (var kvp in views)
            {
                if (!alive.Contains(kvp.Key))
                {
                    stale.Add(kvp.Key);
                }
            }

            for (var i = 0; i < stale.Count; i++)
            {
                var go = views[stale[i]];
                if (go != null)
                {
                    Object.Destroy(go);
                }

                views.Remove(stale[i]);
            }
        }

        private static void TintRenderer(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
