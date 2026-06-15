#nullable enable

using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.PlayMode;
using AbilityKit.Protocol.Shooter;
using UnityEditor;
using UnityEngine;

namespace AbilityKit.Demo.Shooter.Editor.Sink
{
    /// <summary>
    /// Implements <see cref="IShooterSnapshotViewSink"/> for the Editor SceneView.
    /// Caches the latest <see cref="ShooterSnapshotViewBatch"/> from both client and
    /// authoritative worlds, then draws Gizmos during <c>SceneView.duringSceneGui</c>.
    /// </summary>
    public sealed class ShooterEditorSceneViewSink : IShooterSnapshotViewSink, IShooterPlayViewSink
    {
        private ShooterSnapshotViewBatch _clientBatch;
        private ShooterSnapshotViewBatch _authorityBatch;
        private ShooterLagCompensationTelemetry? _lagCompensationTelemetry;
        private bool _hasAuthorityBatch;
        private bool _showDivergence;

        // Cached entity data for efficient Gizmo drawing
        private readonly List<EntityDrawData> _clientEntities = new(32);
        private readonly List<EntityDrawData> _authorityEntities = new(32);
        private readonly List<ShooterEventSnapshot> _pendingEvents = new(16);

        /// <summary>Whether to draw the authoritative world overlay.</summary>
        public bool ShowAuthorityWorld
        {
            get => _hasAuthorityBatch;
            set => _hasAuthorityBatch = value;
        }

        /// <summary>Whether to draw divergence lines between client and authority entities.</summary>
        public bool ShowDivergence
        {
            get => _showDivergence;
            set => _showDivergence = value;
        }

        public void Render(in ShooterPlayPresentationFrame frame)
        {
            _lagCompensationTelemetry = frame.LagCompensationTelemetry;
            var clientBatch = frame.ClientBatch;
            ApplySnapshot(in clientBatch);
            if (frame.HasAuthorityBatch)
            {
                var authorityBatch = frame.AuthorityBatch;
                ApplyAuthoritySnapshot(in authorityBatch);
                _hasAuthorityBatch = true;
            }
            else
            {
                _authorityBatch = default;
                _authorityEntities.Clear();
                _hasAuthorityBatch = false;
            }
        }

        public void ApplySnapshot(in ShooterSnapshotViewBatch batch)
        {
            _clientBatch = batch;
            ExtractEntities(in batch, _clientEntities);
            CacheEvents(in batch);
        }

        /// <summary>
        /// Applies the authoritative world snapshot for overlay rendering.
        /// Separate from <see cref="ApplySnapshot"/> to keep client and authority data independent.
        /// </summary>
        public void ApplyAuthoritySnapshot(in ShooterSnapshotViewBatch batch)
        {
            _authorityBatch = batch;
            ExtractEntities(in batch, _authorityEntities);
        }

        public void Clear()
        {
            _clientBatch = default;
            _authorityBatch = default;
            _lagCompensationTelemetry = null;
            _hasAuthorityBatch = false;
            _clientEntities.Clear();
            _authorityEntities.Clear();
            _pendingEvents.Clear();
        }

        /// <summary>
        /// Draws all cached entities into the SceneView. Called from
        /// <c>SceneView.duringSceneGui</c> by the Editor window.
        /// </summary>
        public void DrawSceneView()
        {
            DrawGrid();

            // Draw client world entities (solid)
            for (int i = 0; i < _clientEntities.Count; i++)
            {
                var entity = _clientEntities[i];
                DrawEntity(in entity, isAuthority: false);
            }

            // Draw events (hit flashes, fire effects)
            DrawEvents();

            // Draw authority world entities (transparent overlay)
            if (_hasAuthorityBatch)
            {
                for (int i = 0; i < _authorityEntities.Count; i++)
                {
                    var entity = _authorityEntities[i];
                    DrawEntity(in entity, isAuthority: true);
                }

                // Draw divergence lines
                if (_showDivergence)
                {
                    DrawDivergenceLines();
                }
            }

            DrawTelemetryOverlay();
        }

        /// <summary>Gets the cached client entity data for external diagnostics.</summary>
        public IReadOnlyList<EntityDrawData> ClientEntities => _clientEntities;

        /// <summary>Gets the cached authority entity data for external diagnostics.</summary>
        public IReadOnlyList<EntityDrawData> AuthorityEntities => _authorityEntities;

        /// <summary>Gets the latest lag compensation telemetry cached from PlayMode frames.</summary>
        public ShooterLagCompensationTelemetry? LagCompensationTelemetry => _lagCompensationTelemetry;

        private void ExtractEntities(in ShooterSnapshotViewBatch batch, List<EntityDrawData> target)
        {
            target.Clear();

            // Build a lookup from entity changes for alive state
            var aliveMap = new Dictionary<int, bool>();
            for (int i = 0; i < batch.EntityChangeCount; i++)
            {
                var change = batch.EntityChanges[i];
                aliveMap[change.EntityId] = change.Alive;
            }

            // Extract transform data
            for (int i = 0; i < batch.TransformChanges.Count; i++)
            {
                var t = batch.TransformChanges[i];
                if (!aliveMap.TryGetValue(t.Key.EntityId, out var alive) || !alive)
                    continue;

                var data = new EntityDrawData
                {
                    EntityId = t.Key.EntityId,
                    Kind = t.Key.Kind,
                    X = t.X,
                    Y = t.Y,
                    FacingX = t.FacingX,
                    FacingY = t.FacingY,
                    VelocityX = t.VelocityX,
                    VelocityY = t.VelocityY,
                };

                // Attach health if available
                for (int j = 0; j < batch.HealthChanges.Count; j++)
                {
                    var h = batch.HealthChanges[j];
                    if (h.Key.EntityId == t.Key.EntityId)
                    {
                        data.Hp = h.Hp;
                        break;
                    }
                }

                // Attach score if available
                for (int j = 0; j < batch.ScoreChanges.Count; j++)
                {
                    var s = batch.ScoreChanges[j];
                    if (s.Key.EntityId == t.Key.EntityId)
                    {
                        data.Score = s.Score;
                        break;
                    }
                }

                // Attach remaining frames if available
                for (int j = 0; j < batch.ProjectileLifetimeChanges.Count; j++)
                {
                    var p = batch.ProjectileLifetimeChanges[j];
                    if (p.Key.EntityId == t.Key.EntityId)
                    {
                        data.RemainingFrames = p.RemainingFrames;
                        break;
                    }
                }

                target.Add(data);
            }
        }

        private void CacheEvents(in ShooterSnapshotViewBatch batch)
        {
            _pendingEvents.Clear();
            if (batch.Events == null) return;
            for (int i = 0; i < batch.Events.Count; i++)
            {
                _pendingEvents.Add(batch.Events[i]);
            }
        }

        private static void DrawGrid()
        {
            var prevColor = HandlesColor;
            HandlesColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            // Draw battlefield boundary (-10 to 10)
            const float size = 10f;
            DrawLine(-size, -size, size, -size); // bottom
            DrawLine(size, -size, size, size);    // right
            DrawLine(size, size, -size, size);    // top
            DrawLine(-size, size, -size, -size);  // left

            // Draw grid lines every 2 units
            HandlesColor = new Color(0.2f, 0.2f, 0.2f, 0.15f);
            for (float x = -size; x <= size; x += 2f)
            {
                DrawLine(x, -size, x, size);
            }
            for (float y = -size; y <= size; y += 2f)
            {
                DrawLine(-size, y, size, y);
            }

            HandlesColor = prevColor;
        }

        private static void DrawEntity(in EntityDrawData data, bool isAuthority)
        {
            // Map 2D game coords to SceneView: X→X, Y→Z (top-down view)
            var pos = new Vector3(data.X, 0f, data.Y);

            if (data.Kind == ShooterViewEntityKind.Player)
            {
                DrawPlayer(pos, data.FacingX, data.FacingY, data.Hp, data.Score, data.EntityId, isAuthority);
            }
            else if (data.Kind == ShooterViewEntityKind.Bullet)
            {
                DrawBullet(pos, data.VelocityX, data.VelocityY, data.OwnerEntityId, data.RemainingFrames, isAuthority);
            }
        }

        private static void DrawPlayer(
            Vector3 pos, float facingX, float facingY, int hp, int score, int playerId, bool isAuthority)
        {
            var prevColor = HandlesColor;

            if (isAuthority)
            {
                // Authority: blue transparent disc
                HandlesColor = new Color(0.3f, 0.5f, 1f, 0.4f);
                DrawDisc(pos, 0.4f);
                HandlesColor = new Color(0.3f, 0.5f, 1f, 0.6f);
                DrawDiscOutline(pos, 0.4f);
            }
            else
            {
                // Client: green disc
                HandlesColor = hp > 0 ? new Color(0.2f, 0.8f, 0.2f, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                DrawDisc(pos, 0.4f);
                HandlesColor = Color.white;
                DrawDiscOutline(pos, 0.4f);

                // Facing direction arrow
                var facingDir = new Vector3(facingX, 0f, facingY).normalized;
                if (facingDir.magnitude > 0.01f)
                {
                    HandlesColor = Color.white;
                    DrawLine(pos, pos + facingDir * 0.6f);
                }

                // HP label
                HandlesColor = Color.white;
                DrawLabel(pos + Vector3.up * 0.8f, $"P{playerId} HP:{hp} S:{score}");
            }

            HandlesColor = prevColor;
        }

        private static void DrawBullet(
            Vector3 pos, float velX, float velY, int ownerId, int remainingFrames, bool isAuthority)
        {
            var prevColor = HandlesColor;

            if (isAuthority)
            {
                // Authority: blue transparent
                HandlesColor = new Color(0.3f, 0.5f, 1f, 0.3f);
            }
            else
            {
                // Client: yellow
                HandlesColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            }

            // Draw bullet as a small sphere
            DrawDisc(pos, 0.1f);

            // Draw velocity direction
            var vel = new Vector3(velX, 0f, velY);
            if (vel.magnitude > 0.01f)
            {
                DrawLine(pos, pos + vel.normalized * 0.4f);
            }

            if (!isAuthority)
            {
                HandlesColor = new Color(1f, 0.9f, 0.2f, 0.6f);
                DrawLabel(pos + Vector3.up * 0.3f, $"B(owner:{ownerId} f:{remainingFrames})");
            }

            HandlesColor = prevColor;
        }

        private void DrawEvents()
        {
            var prevColor = HandlesColor;
            for (int i = 0; i < _pendingEvents.Count; i++)
            {
                var evt = _pendingEvents[i];
                var pos = new Vector3(evt.X, 0f, evt.Y);

                if (evt.EventType == 1) // Hit
                {
                    HandlesColor = new Color(1f, 0.2f, 0.2f, 0.8f);
                    DrawDiscOutline(pos, 0.5f);
                    DrawDiscOutline(pos, 0.3f);
                }
                else if (evt.EventType == 2) // Fire
                {
                    HandlesColor = new Color(1f, 0.6f, 0.1f, 0.6f);
                    DrawDisc(pos, 0.08f);
                }
            }
            HandlesColor = prevColor;
        }

        private void DrawTelemetryOverlay()
        {
            if (!_lagCompensationTelemetry.HasValue) return;

            var telemetry = _lagCompensationTelemetry.Value;
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12f, 12f, 260f, 82f), EditorStyles.helpBox);
            GUILayout.Label("Lag Compensation", EditorStyles.boldLabel);
            GUILayout.Label($"History: {telemetry.CapturedFrameCount} frames");
            GUILayout.Label($"Range: {telemetry.OldestFrame} → {telemetry.LatestFrame}");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawDivergenceLines()
        {
            var prevColor = HandlesColor;
            HandlesColor = new Color(1f, 0.2f, 0.2f, 0.6f);

            // Match client and authority entities by EntityId
            for (int i = 0; i < _clientEntities.Count; i++)
            {
                var client = _clientEntities[i];
                for (int j = 0; j < _authorityEntities.Count; j++)
                {
                    var auth = _authorityEntities[j];
                    if (auth.EntityId == client.EntityId && auth.Kind == client.Kind)
                    {
                        var clientPos = new Vector3(client.X, 0f, client.Y);
                        var authPos = new Vector3(auth.X, 0f, auth.Y);
                        var distance = Vector3.Distance(clientPos, authPos);

                        if (distance > 0.01f)
                        {
                            DrawDashedLine(clientPos, authPos);
                            DrawLabel(
                                (clientPos + authPos) * 0.5f + Vector3.up * 0.5f,
                                $"{distance:F2}");
                        }
                        break;
                    }
                }
            }

            HandlesColor = prevColor;
        }

        // --- Handle drawing utilities ---

        private static UnityEngine.Color HandlesColor
        {
            get => Handles.color;
            set => Handles.color = value;
        }

        private static void DrawLine(Vector3 a, Vector3 b)
        {
            Handles.DrawLine(a, b);
        }

        private static void DrawLine(float x1, float y1, float x2, float y2)
        {
            Handles.DrawLine(new Vector3(x1, 0f, y1), new Vector3(x2, 0f, y2));
        }

        private static void DrawDisc(Vector3 center, float radius)
        {
            Handles.DrawSolidDisc(center, Vector3.up, radius);
        }

        private static void DrawDiscOutline(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
        }

        private static void DrawLabel(Vector3 position, string text)
        {
            Handles.Label(position, text);
        }

        private static void DrawDashedLine(Vector3 a, Vector3 b)
        {
            // Simple dashed line using short segments
            var dir = (b - a);
            var length = dir.magnitude;
            if (length < 0.01f) return;
            dir /= length;

            const float dashLen = 0.15f;
            const float gapLen = 0.1f;
            var drawn = 0f;
            var on = true;

            while (drawn < length)
            {
                var segLen = on ? dashLen : gapLen;
                if (drawn + segLen > length) segLen = length - drawn;

                if (on)
                {
                    var start = a + dir * drawn;
                    var end = a + dir * (drawn + segLen);
                    Handles.DrawLine(start, end);
                }

                drawn += segLen;
                on = !on;
            }
        }

        /// <summary>
        /// Cached entity data extracted from a <see cref="ShooterSnapshotViewBatch"/>
        /// for efficient Gizmo drawing.
        /// </summary>
        public struct EntityDrawData
        {
            public int EntityId;
            public ShooterViewEntityKind Kind;
            public float X;
            public float Y;
            public float FacingX;
            public float FacingY;
            public float VelocityX;
            public float VelocityY;
            public int Hp;
            public int Score;
            public int OwnerEntityId;
            public int RemainingFrames;
        }
    }
}
