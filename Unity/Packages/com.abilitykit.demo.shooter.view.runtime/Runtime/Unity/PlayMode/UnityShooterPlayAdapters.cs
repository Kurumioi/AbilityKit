#nullable enable

using System.Collections.Generic;
using AbilityKit.Demo.Shooter.View.Hosting;
using UnityEngine;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    internal sealed class UnityShooterPlayInputSource : IShooterHostInputSource
    {
        public ShooterHostFrameInput ReadInput(int controlledPlayerId)
        {
            return new ShooterHostFrameInput(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                0f,
                1f,
                Input.GetKey(KeyCode.Space));
        }
    }

    internal sealed class UnityShooterGameObjectViewSink : IShooterHostViewSink
    {
        private readonly Dictionary<int, GameObject> _playerViews = new();
        private readonly Dictionary<int, GameObject> _bulletViews = new();
        private readonly Dictionary<int, GameObject> _authorityPlayerViews = new();
        private readonly Dictionary<int, GameObject> _authorityBulletViews = new();
        private readonly HashSet<int> _seenPlayers = new();
        private readonly HashSet<int> _seenBullets = new();
        private Transform? _viewRoot;
        private Transform? _clientRoot;
        private Transform? _authorityRoot;

        public void Render(in ShooterHostPresentationFrame frame)
        {
            EnsureViewRoot();
            var clientBatch = frame.ClientBatch;
            RenderBatch(
                in clientBatch,
                frame.ControlledPlayerId,
                frame.WorldScale,
                _playerViews,
                _bulletViews,
                _clientRoot,
                isAuthority: false);

            if (frame.HasAuthorityBatch)
            {
                var authorityBatch = frame.AuthorityBatch;
                RenderBatch(
                    in authorityBatch,
                    frame.ControlledPlayerId,
                    frame.WorldScale,
                    _authorityPlayerViews,
                    _authorityBulletViews,
                    _authorityRoot,
                    isAuthority: true);
            }
            else
            {
                ClearViews(_authorityPlayerViews);
                ClearViews(_authorityBulletViews);
            }
        }

        public void Clear()
        {
            ClearViews(_playerViews);
            ClearViews(_bulletViews);
            ClearViews(_authorityPlayerViews);
            ClearViews(_authorityBulletViews);

            if (_viewRoot != null)
            {
                Object.Destroy(_viewRoot.gameObject);
                _viewRoot = null;
            }
        }

        private void RenderBatch(
            in ShooterSnapshotViewBatch batch,
            int controlledPlayerId,
            float worldScale,
            Dictionary<int, GameObject> playerViews,
            Dictionary<int, GameObject> bulletViews,
            Transform? parent,
            bool isAuthority)
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
                    var view = GetOrCreatePlayerView(playerViews, parent, transform.Key.EntityId, controlledPlayerId, isAuthority);
                    view.transform.localPosition = new Vector3(transform.X * worldScale, isAuthority ? 0.15f : 0f, transform.Y * worldScale);
                }
                else if (transform.Key.Kind == ShooterViewEntityKind.Bullet && _seenBullets.Contains(transform.Key.EntityId))
                {
                    var view = GetOrCreateBulletView(bulletViews, parent, transform.Key.EntityId, isAuthority);
                    view.transform.localPosition = new Vector3(transform.X * worldScale, isAuthority ? 0.15f : 0f, transform.Y * worldScale);
                }
            }

            PruneViews(playerViews, _seenPlayers);
            PruneViews(bulletViews, _seenBullets);
        }

        private GameObject GetOrCreatePlayerView(
            Dictionary<int, GameObject> views,
            Transform? parent,
            int id,
            int controlledPlayerId,
            bool isAuthority)
        {
            if (views.TryGetValue(id, out var existing) && existing != null)
            {
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = isAuthority ? $"ShooterAuthorityPlayer_{id}" : $"ShooterPlayer_{id}";
            go.transform.SetParent(parent, false);
            TintRenderer(go, isAuthority ? new Color(1f, 0.35f, 0.35f, 0.55f) : id == controlledPlayerId ? Color.green : Color.cyan);
            views[id] = go;
            return go;
        }

        private GameObject GetOrCreateBulletView(Dictionary<int, GameObject> views, Transform? parent, int id, bool isAuthority)
        {
            if (views.TryGetValue(id, out var existing) && existing != null)
            {
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = isAuthority ? $"ShooterAuthorityBullet_{id}" : $"ShooterBullet_{id}";
            go.transform.localScale = Vector3.one * (isAuthority ? 0.45f : 0.35f);
            go.transform.SetParent(parent, false);
            TintRenderer(go, isAuthority ? new Color(1f, 0.65f, 0.15f, 0.55f) : Color.yellow);
            views[id] = go;
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
            _clientRoot = new GameObject("Client").transform;
            _clientRoot.SetParent(_viewRoot, false);
            _authorityRoot = new GameObject("Authority").transform;
            _authorityRoot.SetParent(_viewRoot, false);
        }

        private static void ClearViews(Dictionary<int, GameObject> views)
        {
            foreach (var kvp in views)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }

            views.Clear();
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
