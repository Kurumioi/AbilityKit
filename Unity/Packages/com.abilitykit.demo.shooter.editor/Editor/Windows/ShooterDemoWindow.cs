#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Editor.Diagnostics;
using AbilityKit.Demo.Shooter.Editor.Input;
using AbilityKit.Demo.Shooter.Editor.Sink;
using AbilityKit.Demo.Shooter.View.Network;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.PlayMode;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Protocol.Shooter;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbilityKit.Demo.Shooter.Editor.Windows
{
    /// <summary>
    /// Main Editor window for the Shooter network sync demo.
    /// Drives the simulation via <see cref="EditorApplication.update"/>,
    /// renders entities in SceneView via <see cref="ShooterEditorSceneViewSink"/>,
    /// and provides controls for sync mode, network environment, and diagnostics.
    /// </summary>
    public sealed partial class ShooterDemoWindow : EditorWindow
    {
        // --- Session state ---
        private ShooterAcceptanceSession? _session;
        private ShooterPlaySessionRunner? _editorRunner;
        private ShooterEditorSceneViewSink _sink = new();
        private ShooterEditorInputProvider _inputProvider = new();
        private ShooterDemoDiagnostics _diagnostics = new();

        // --- Drive mode ---
        private ShooterDemoDriveMode _driveMode = ShooterDemoDriveMode.EditorDirect;
        private IShooterPlayModeSessionHost? _attachedHost;

        // --- Run state ---
        private bool _running;
        private bool _paused;
        private float _timeScale = 1f;
        private double _lastTime;

        // --- Configuration ---
        private int _selectedSyncIndex;
        private int _selectedNetworkPresetIndex;
        private int _selectedNetworkProviderIndex;
        private bool _enableAuthoritativeWorld = true;
        private bool _showDivergence = true;
        private int _playerCount = 2;
        private int _initialHp = ShooterGameplay.DefaultPlayerHp;
        private int _randomSeed = 3901;
        private int _controlledPlayerId = 1;

        // --- Network parameters (built-in provider) ---
        private int _latencyMs;
        private int _jitterMs;
        private double _packetLossRate;
        private double _reorderRate;
        private int _bandwidthKbps;

        // --- UI state ---
        private Vector2 _diagnosticsScroll;
        private Vector2 _eventsScroll;
        private string _lastError = string.Empty;

        // --- Cached catalog data ---
        private static readonly IReadOnlyList<ShooterAcceptanceSyncOption> SyncModes =
            ShooterAcceptanceCatalog.SyncModes;
        private static readonly IReadOnlyList<ShooterAcceptanceNetworkOption> NetworkPresets =
            ShooterAcceptanceCatalog.NetworkEnvironments;

        [MenuItem("Tools/AbilityKit/Shooter Demo")]
        private static void Open()
        {
            GetWindow<ShooterDemoWindow>("Shooter Demo");
        }

        private void OnEnable()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            ApplyNetworkPreset(0);
            // Keep the status bar's host-lifecycle indicator fresh even when the window
            // is not attached to the PlayMode host (the host lives independently of us).
            ShooterPlayModeSessionRegistry.HostsChanged += OnHostLifecycleChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            StopInternal();
            ShooterPlayModeSessionRegistry.HostsChanged -= OnHostLifecycleChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnHostLifecycleChanged()
        {
            // The host lifecycle can change independently of this window; refresh the
            // status bar so the Idle/Installed/Running indicator stays accurate.
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Entering/exiting Play mode changes the host indicator and control availability.
            Repaint();
        }

        // =====================================================================
        // Main OnGUI
        // =====================================================================

        private void OnGUI()
        {
            CaptureWindowKeyboardInput();
            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // Left panel: Configuration
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            DrawConfigPanel();
            EditorGUILayout.EndVertical();

            // Right panel: Diagnostics
            EditorGUILayout.BeginVertical();
            DrawDiagnosticsPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawStatusBar();
        }

        // =====================================================================
        // Toolbar
        // =====================================================================

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Drive mode selector (cannot change while a session is live)
            EditorGUI.BeginDisabledGroup(_running);
            var newMode = (ShooterDemoDriveMode)EditorGUILayout.EnumPopup(
                _driveMode, EditorStyles.toolbarPopup, GUILayout.Width(130));
            if (newMode != _driveMode)
            {
                _driveMode = newMode;
            }
            EditorGUI.EndDisabledGroup();

            var attachMode = _driveMode == ShooterDemoDriveMode.PlayModeAttach;

            EditorGUI.BeginDisabledGroup(_running);
            var startLabel = attachMode ? "🔗 Attach" : "▶ Start";
            if (GUILayout.Button(startLabel, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                StartInternal();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!_running);
            var stopLabel = attachMode ? "✂ Detach" : "■ Stop";
            if (GUILayout.Button(stopLabel, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                StopInternal();
            }
            EditorGUI.EndDisabledGroup();

            if (attachMode)
            {
                // The button is enabled whenever there is a published host that can be
                // stopped — we do not reference the concrete PlayMode host type here so
                // the toolbar stays decoupled from the active host implementation.
                var hasStoppableHost = ShooterPlayModeSessionRegistry.Active != null;
                EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasStoppableHost);
                if (GUILayout.Button("■ Stop Host", EditorStyles.toolbarButton, GUILayout.Width(95)))
                {
                    StopPlayModeHostInternal();
                }
                EditorGUI.EndDisabledGroup();
            }

            // Pause/Step only make sense when the window owns the loop (Editor Direct).
            EditorGUI.BeginDisabledGroup(!_running || attachMode);
            _paused = GUILayout.Toggle(_paused, "‖ Pause", EditorStyles.toolbarButton, GUILayout.Width(70));

            if (GUILayout.Button("▶ Step", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                StepInternal();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // Speed only affects the Editor-driven loop.
            EditorGUI.BeginDisabledGroup(attachMode);
            GUILayout.Label("Speed", GUILayout.Width(35));
            _timeScale = EditorGUILayout.Slider(_timeScale, 0f, 4f, GUILayout.Width(160));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        // Config Panel
        // =====================================================================

        private void DrawConfigPanel()
        {
            DrawSyncModeSection();
            EditorGUILayout.Space(4);
            DrawNetworkSection();
            EditorGUILayout.Space(4);
            DrawPlayerConfigSection();
            EditorGUILayout.Space(4);
            DrawInputSection();
        }

        private void DrawSyncModeSection()
        {
            EditorGUILayout.LabelField("Sync Mode", EditorStyles.boldLabel);

            var syncNames = new string[SyncModes.Count];
            for (int i = 0; i < SyncModes.Count; i++)
            {
                syncNames[i] = SyncModes[i].Implemented
                    ? SyncModes[i].DisplayName
                    : SyncModes[i].DisplayName + " (N/A)";
            }

            EditorGUI.BeginDisabledGroup(_running);
            var newSyncIdx = EditorGUILayout.Popup("Mode", _selectedSyncIndex, syncNames);
            if (newSyncIdx != _selectedSyncIndex)
            {
                _selectedSyncIndex = newSyncIdx;
            }
            EditorGUI.EndDisabledGroup();

            if (_selectedSyncIndex < SyncModes.Count)
            {
                var mode = SyncModes[_selectedSyncIndex];
                if (!mode.Implemented)
                {
                    EditorGUILayout.HelpBox($"'{mode.DisplayName}' is not yet implemented.", MessageType.Warning);
                }
            }
        }

        private void DrawNetworkSection()
        {
            EditorGUILayout.LabelField("Network Environment", EditorStyles.boldLabel);

            // Network provider source dropdown
            var providers = ShooterNetworkConditionRegistry.All;
            var providerNames = new string[providers.Count];
            for (int i = 0; i < providers.Count; i++)
            {
                providerNames[i] = providers[i].DisplayName;
                if (!providers[i].IsActive) providerNames[i] += " (Inactive)";
            }

            var newProviderIdx = EditorGUILayout.Popup("Source", _selectedNetworkProviderIndex, providerNames);
            if (newProviderIdx != _selectedNetworkProviderIndex)
            {
                _selectedNetworkProviderIndex = newProviderIdx;
            }

            // Only show built-in sliders when the built-in provider is selected
            if (_selectedNetworkProviderIndex == 0)
            {
                DrawBuiltinNetworkSliders();
            }
            else if (_selectedNetworkProviderIndex < providers.Count)
            {
                var provider = providers[_selectedNetworkProviderIndex];
                if (!provider.IsActive)
                {
                    EditorGUILayout.HelpBox(
                        $"External provider '{provider.DisplayName}' is not active. Check that the tool is running.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("Profile", DescribeProfile(provider.Profile));
                }
            }
        }

        private void DrawBuiltinNetworkSliders()
        {
            // Preset buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preset", GUILayout.Width(45));
            for (int i = 0; i < NetworkPresets.Count; i++)
            {
                var preset = NetworkPresets[i];
                var shortName = GetShortPresetName(preset.DisplayName);
                if (GUILayout.Button(shortName, EditorStyles.miniButton, GUILayout.Width(52)))
                {
                    ApplyNetworkPreset(i);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Parameter sliders
            EditorGUI.BeginChangeCheck();
            _latencyMs = EditorGUILayout.IntSlider("Latency (ms)", _latencyMs, 0, 500);
            _jitterMs = EditorGUILayout.IntSlider("Jitter (ms)", _jitterMs, 0, 200);
            _packetLossRate = EditorGUILayout.Slider("Packet Loss", (float)_packetLossRate, 0f, 0.5f);
            _reorderRate = EditorGUILayout.Slider("Reorder Rate", (float)_reorderRate, 0f, 0.5f);
            _bandwidthKbps = EditorGUILayout.IntSlider("Bandwidth (kbps)", _bandwidthKbps, 0, 10000);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyCustomNetwork();
            }
        }

        private void DrawPlayerConfigSection()
        {
            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_running);
            _playerCount = EditorGUILayout.IntSlider("Count", _playerCount, 1, ShooterGameplay.DefaultMaxPlayers);
            _initialHp = EditorGUILayout.IntField("Initial HP", _initialHp);
            _randomSeed = EditorGUILayout.IntField("Random Seed", _randomSeed);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(2);

            _enableAuthoritativeWorld = EditorGUILayout.ToggleLeft(
                "Show Authority World (Compare)", _enableAuthoritativeWorld);

            if (_enableAuthoritativeWorld)
            {
                _showDivergence = EditorGUILayout.ToggleLeft(
                    "  Show Divergence Lines", _showDivergence);
            }
        }

        private void DrawInputSection()
        {
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

            _inputProvider.EnableKeyboardInput = EditorGUILayout.ToggleLeft(
                "Enable Keyboard (WASD + Space)", _inputProvider.EnableKeyboardInput);

            EditorGUI.BeginDisabledGroup(!_inputProvider.EnableKeyboardInput);
            EditorGUILayout.LabelField("Keys", _inputProvider.GetDebugKeyString());
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(2);

            var playerOptions = new string[_playerCount];
            for (int i = 0; i < _playerCount; i++)
            {
                playerOptions[i] = $"Player {i + 1}";
            }
            _controlledPlayerId = EditorGUILayout.Popup("Control", _controlledPlayerId - 1, playerOptions) + 1;
            _inputProvider.ControlledPlayerId = _controlledPlayerId;
        }

        // =====================================================================
        // Diagnostics Panel
        // =====================================================================

        private void DrawDiagnosticsPanel()
        {
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

            if (!_running)
            {
                EditorGUILayout.HelpBox(
                    "Click '▶ Start' to begin the Shooter demo session.\n" +
                    "Use WASD to move, Space to fire.\n" +
                    "Entities render in the SceneView window.",
                    MessageType.Info);
                return;
            }

            // Stats row
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Frame: {_diagnostics.Frame}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Players: {_diagnostics.PlayerCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"Bullets: {_diagnostics.BulletCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"Rollbacks: {_diagnostics.TotalRollbacks}", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Divergence
            if (_enableAuthoritativeWorld)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Max Divergence: {_diagnostics.MaxDivergence:F4}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"Divergences: {_diagnostics.Divergences.Count}", GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }

            // Entity list
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Entities", EditorStyles.miniBoldLabel);

            _diagnosticsScroll = EditorGUILayout.BeginScrollView(_diagnosticsScroll, GUILayout.Height(140));
            var clientEntities = _sink.ClientEntities;
            for (int i = 0; i < clientEntities.Count; i++)
            {
                var e = clientEntities[i];
                string label;
                if (e.Kind == ShooterViewEntityKind.Player)
                {
                    label = $"  Player #{e.EntityId}  HP:{e.Hp}  Score:{e.Score}  ({e.X:F1}, {e.Y:F1})";
                }
                else
                {
                    label = $"  Bullet #{e.EntityId}  Owner:{e.OwnerEntityId}  ({e.X:F1}, {e.Y:F1})  vel:({e.VelocityX:F1}, {e.VelocityY:F1})  f:{e.RemainingFrames}";
                }
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            // Events log
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Events (Total: {_diagnostics.TotalEvents})", EditorStyles.miniBoldLabel);

            _eventsScroll = EditorGUILayout.BeginScrollView(_eventsScroll, GUILayout.Height(120));
            var events = _diagnostics.RecentEvents;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var evt = events[i];
                string label;
                if (evt.EventType == 1)
                {
                    label = $"  [Hit] P{evt.SourcePlayerId}→P{evt.TargetPlayerId} Bullet#{evt.BulletId} at ({evt.X:F1},{evt.Y:F1}) dmg:{evt.Value}";
                }
                else if (evt.EventType == 2)
                {
                    label = $"  [Fire] P{evt.SourcePlayerId} Bullet#{evt.BulletId} at ({evt.X:F1},{evt.Y:F1})";
                }
                else
                {
                    label = $"  [Event{evt.EventType}] src:{evt.SourcePlayerId} tgt:{evt.TargetPlayerId} val:{evt.Value}";
                }
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        // =====================================================================
        // Status Bar
        // =====================================================================

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(_diagnostics.StatusText, EditorStyles.miniLabel, GUILayout.Width(120));

            if (_running && _session != null)
            {
                EditorGUILayout.LabelField($"Sync: {_session.SyncModel}", EditorStyles.miniLabel, GUILayout.Width(160));
                EditorGUILayout.LabelField($"Network: {_session.NetworkName}", EditorStyles.miniLabel, GUILayout.Width(140));
            }

            GUILayout.FlexibleSpace();

            // PlayMode host lifecycle is independent of the window's attachment state.
            // Surface it so the "Unity as host" lifecycle is observable at a glance.
            if (_driveMode == ShooterDemoDriveMode.PlayModeAttach)
            {
                EditorGUILayout.LabelField(GetPlayModeHostStatusText(), EditorStyles.miniLabel, GUILayout.Width(150));
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.LabelField(_lastError, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Builds a human-readable label for the PlayMode host lifecycle, which exists
        /// independently of whether this Editor window is currently attached to it.
        /// </summary>
        private static string GetPlayModeHostStatusText()
        {
            if (!Application.isPlaying)
            {
                return "Host: Idle (not in Play mode)";
            }
            if (ShooterPlayModeSessionHost.IsRunning)
            {
                return "Host: Running";
            }
            if (ShooterPlayModeSessionHost.IsInstalled)
            {
                return "Host: Installed (no session)";
            }
            return "Host: Idle";
        }

        // =====================================================================
        // Keyboard Input Capture
        // =====================================================================

        private void CaptureWindowKeyboardInput()
        {
            var e = Event.current;
            if (e == null) return;

            if (e.type == EventType.KeyDown)
            {
                if (_inputProvider.OnKeyDown(e))
                {
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.KeyUp)
            {
                if (_inputProvider.OnKeyUp(e))
                {
                    e.Use();
                    Repaint();
                }
            }
        }

        // =====================================================================
        // Session Lifecycle
        // =====================================================================

        private void StartInternal()
        {
            if (_running) return;

            if (_driveMode == ShooterDemoDriveMode.PlayModeAttach)
            {
                AttachInternal();
                return;
            }

            try
            {
                _lastError = string.Empty;

                if (!TryBuildSessionOptions(out var options))
                {
                    Repaint();
                    return;
                }

                _editorRunner = new ShooterPlaySessionRunner(_inputProvider, _sink);
                _session = _editorRunner.Start(options);

                // Configure sink
                _sink.Clear();
                _sink.ShowAuthorityWorld = _enableAuthoritativeWorld;
                _sink.ShowDivergence = _showDivergence;

                // Configure diagnostics
                _diagnostics.Reset();
                _diagnostics.IsRunning = true;

                // Apply initial snapshot to sink
                var initialSnapshot = _session.Runtime.GetSnapshot();
                _session.Presentation.ApplyLocalPredictionSnapshot(in initialSnapshot);

                // Register SceneView callback
                SceneView.duringSceneGui += OnSceneViewGUI;

                // Start editor update loop
                _lastTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += OnEditorUpdate;

                _running = true;
                _paused = false;

                // Focus SceneView for best experience
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                {
                    sv.LookAt(new Vector3(2f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f), 20f);
                }

                Repaint();
            }
            catch (Exception ex)
            {
                _lastError = $"Start failed: {ex.Message}";
                Debug.LogException(ex);
                StopInternal();
            }
        }

        private void StopInternal()
        {
            if (_attachedHost != null)
            {
                DetachInternal();
                return;
            }

            if (_running)
            {
                EditorApplication.update -= OnEditorUpdate;
                SceneView.duringSceneGui -= OnSceneViewGUI;
            }

            _running = false;
            _paused = false;
            _editorRunner?.Dispose();
            _editorRunner = null;
            _session = null;
            _sink.Clear();
            _inputProvider.Reset();
            _diagnostics.Reset();

            SceneView.RepaintAll();
            Repaint();
        }

        private void StopPlayModeHostInternal()
        {
            try
            {
                if (_attachedHost != null)
                {
                    DetachInternal();
                }

                // Stop the host through the published interface so this window does not
                // depend on the concrete PlayMode host implementation. Any registered
                // host (now or in the future) can be stopped uniformly.
                var host = ShooterPlayModeSessionRegistry.Active;
                if (host != null)
                {
                    host.Stop();
                }

                _lastError = string.Empty;
                Repaint();
            }
            catch (Exception ex)
            {
                _lastError = $"Stop Play-mode host failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        // =====================================================================
        // Play Mode Attach Lifecycle
        // =====================================================================

        private void AttachInternal()
        {
            _lastError = string.Empty;

            if (!Application.isPlaying)
            {
                _lastError = "Enter Play mode before attaching to the Unity host.";
                Repaint();
                return;
            }

            if (!TryBuildSessionOptions(out var options))
            {
                Repaint();
                return;
            }

            var host = ShooterPlayModeSessionRegistry.Active;
            if (host == null || !host.IsRunning)
            {
                ShooterPlayModeSessionHost.Start(options);
                host = ShooterPlayModeSessionRegistry.Active;
            }

            if (host == null)
            {
                _lastError = "Failed to publish the Play-mode session host.";
                Repaint();
                return;
            }

            _attachedHost = host;
            _session = host.Session;
            _running = true;
            _paused = false;
            _diagnostics.Reset();
            _diagnostics.IsRunning = host.IsRunning;

            // The window observes the live host; it does not pump logic. We only poll for
            // diagnostics and push network changes through the shared registry.
            EditorApplication.update += OnAttachUpdate;
            ShooterPlayModeSessionRegistry.HostsChanged += OnRegistryHostsChanged;

            Repaint();
        }

        private void DetachInternal()
        {
            EditorApplication.update -= OnAttachUpdate;
            ShooterPlayModeSessionRegistry.HostsChanged -= OnRegistryHostsChanged;

            _attachedHost = null;
            _session = null;
            _running = false;
            _paused = false;
            _diagnostics.Reset();

            Repaint();
        }

        private void OnRegistryHostsChanged()
        {
            // If the host we were attached to went away (Play mode exited), detach gracefully.
            if (_attachedHost != null && !_attachedHost.IsRunning &&
                ShooterPlayModeSessionRegistry.Active == null)
            {
                DetachInternal();
            }
        }

        private void OnAttachUpdate()
        {
            if (_attachedHost == null)
            {
                return;
            }

            // Host destroyed (e.g. exited Play mode) — tear down the attachment.
            if (!_attachedHost.IsRunning && _attachedHost.Session == null)
            {
                _lastError = "Play-mode session ended.";
                DetachInternal();
                return;
            }

            var session = _attachedHost.Session;
            if (!ReferenceEquals(_session, session))
            {
                _session = session;
                _diagnostics.Reset();
            }

            _diagnostics.IsRunning = _attachedHost.IsRunning;

            if (session != null)
            {
                var snapshot = session.Runtime.GetSnapshot();
                UpdateDiagnostics(in snapshot, null!);
            }

            Repaint();
        }

        private bool TryBuildSessionOptions(out ShooterPlayModeSessionOptions options)
        {
            options = default;

            if (_selectedSyncIndex >= SyncModes.Count || !SyncModes[_selectedSyncIndex].Implemented)
            {
                _lastError = "Selected sync mode is not implemented.";
                return false;
            }

            var syncMode = SyncModes[_selectedSyncIndex];
            var networkProfile = GetCurrentNetworkProfile();
            options = new ShooterPlayModeSessionOptions(
                syncMode.Model,
                ShooterGameplay.DefaultTickRate,
                _playerCount,
                _randomSeed,
                _controlledPlayerId,
                _enableAuthoritativeWorld,
                networkProfile.BaseLatencyMs,
                networkProfile.JitterMs,
                (float)networkProfile.PacketLossRate,
                (float)networkProfile.ReorderRate,
                networkProfile.BandwidthKbps,
                worldScale: 1f,
                networkName: GetCurrentNetworkName());
            return true;
        }

        private void StepInternal()
        {
            if (!_running || _session == null) return;
            TickSession(1f / ShooterGameplay.DefaultTickRate);
        }

        // =====================================================================
        // Editor Update Loop
        // =====================================================================

        private void OnEditorUpdate()
        {
            if (!_running || _session == null) return;
            if (_paused) return;

            var now = EditorApplication.timeSinceStartup;
            var delta = (float)((now - _lastTime) * _timeScale);
            _lastTime = now;

            // Clamp delta to avoid spiral of death
            if (delta > 0.1f) delta = 0.1f;

            TickSession(delta);

            SceneView.RepaintAll();
            Repaint();
        }

        private void TickSession(float deltaSeconds)
        {
            if (_session == null || _editorRunner == null) return;

            _editorRunner.Tick(deltaSeconds);

            var snapshot = _session.Runtime.GetSnapshot();
            UpdateDiagnostics(in snapshot, null!);
        }

        // =====================================================================
        // SceneView Rendering
        // =====================================================================

        private void OnSceneViewGUI(SceneView sceneView)
        {
            if (!_running) return;
            _sink.DrawSceneView();
        }

        // =====================================================================
        // Diagnostics Update
        // =====================================================================

        private void UpdateDiagnostics(in ShooterStateSnapshotPayload snapshot, object tickResult)
        {
            _diagnostics.Frame = snapshot.Frame;
            _diagnostics.PlayerCount = snapshot.Players?.Length ?? 0;
            _diagnostics.BulletCount = snapshot.Bullets?.Length ?? 0;

            // Events
            if (snapshot.Events != null && snapshot.Events.Length > 0)
            {
                _diagnostics.RecentEvents = snapshot.Events;
                _diagnostics.TotalEvents += snapshot.Events.Length;
            }

            // Divergence
            if (_session != null && _session.HasAuthoritativeWorld)
            {
                var comparison = _session.CompareWorlds();
                _diagnostics.MaxDivergence = comparison.MaxDistance;
                _diagnostics.Divergences = comparison.Divergences;
            }
        }

        // =====================================================================
        // Network Helpers
        // =====================================================================

        private void ApplyNetworkPreset(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= NetworkPresets.Count) return;
            _selectedNetworkPresetIndex = presetIndex;

            var preset = NetworkPresets[presetIndex];
            _latencyMs = preset.Profile.BaseLatencyMs;
            _jitterMs = preset.Profile.JitterMs;
            _packetLossRate = preset.Profile.PacketLossRate;
            _reorderRate = preset.Profile.ReorderRate;
            _bandwidthKbps = preset.Profile.BandwidthKbps;

            ApplyCustomNetwork();
        }

        private void ApplyCustomNetwork()
        {
            var profile = new NetworkConditionProfile(
                _latencyMs, _jitterMs, _packetLossRate, _reorderRate, _bandwidthKbps);

            ShooterNetworkConditionRegistry.Builtin.ApplyProfile(profile);

            // If session is running, apply immediately through the shared runner path.
            if (_editorRunner != null)
            {
                _editorRunner.ApplyNetwork(profile);
            }
            else if (_session != null)
            {
                _session.ApplyNetwork(profile, GetCurrentNetworkName());
            }
        }

        private NetworkConditionProfile GetCurrentNetworkProfile()
        {
            var providers = ShooterNetworkConditionRegistry.All;
            if (_selectedNetworkProviderIndex > 0 && _selectedNetworkProviderIndex < providers.Count)
            {
                var provider = providers[_selectedNetworkProviderIndex];
                if (provider.IsActive) return provider.Profile;
            }

            return new NetworkConditionProfile(
                _latencyMs, _jitterMs, _packetLossRate, _reorderRate, _bandwidthKbps);
        }

        private string GetCurrentNetworkName()
        {
            var providers = ShooterNetworkConditionRegistry.All;
            if (_selectedNetworkProviderIndex > 0 && _selectedNetworkProviderIndex < providers.Count)
            {
                return providers[_selectedNetworkProviderIndex].DisplayName;
            }

            // Check if matches a preset
            foreach (var preset in NetworkPresets)
            {
                if (preset.Profile.BaseLatencyMs == _latencyMs
                    && preset.Profile.JitterMs == _jitterMs
                    && Math.Abs(preset.Profile.PacketLossRate - _packetLossRate) < 0.0001d
                    && Math.Abs(preset.Profile.ReorderRate - _reorderRate) < 0.0001d
                    && preset.Profile.BandwidthKbps == _bandwidthKbps)
                {
                    return preset.DisplayName;
                }
            }

            return $"Custom ({_latencyMs}ms/{_jitterMs}ms)";
        }

        private static string DescribeProfile(NetworkConditionProfile p)
        {
            return $"{p.BaseLatencyMs}ms ±{p.JitterMs}ms  loss:{p.PacketLossRate:P1}  reorder:{p.ReorderRate:P1}  bw:{p.BandwidthKbps}kbps";
        }

        private static string GetShortPresetName(string displayName)
        {
            // Extract short name: "Ideal (0ms)" → "Ideal", "Mobile 4G (60ms)" → "4G"
            var paren = displayName.IndexOf('(');
            if (paren > 0) return displayName.Substring(0, paren).Trim();
            return displayName;
        }
    }
}
