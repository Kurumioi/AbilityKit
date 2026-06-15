#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Editor.Diagnostics;
using AbilityKit.Demo.Shooter.Editor.Input;
using AbilityKit.Demo.Shooter.Editor.Sink;
using AbilityKit.Demo.Shooter.View.Hosting;
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
    /// Shooter 网络同步演示主窗口。
    /// 可通过 <see cref="EditorApplication.update"/> 驱动模拟，
    /// 通过 <see cref="ShooterEditorSceneViewSink"/> 在 SceneView 中渲染实体，
    /// 并提供同步模式、网络环境与运行诊断配置。
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
        private IShooterSessionHost? _attachedHost;

        // --- Run state ---
        private bool _running;
        private bool _paused;
        private bool _showingSpecBaseline;
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

        // --- 缓存目录数据 ---
        private static readonly IReadOnlyList<ShooterAcceptanceSyncOption> SyncModes =
            ShooterAcceptanceCatalog.SyncModes;
        private static readonly IReadOnlyList<ShooterAcceptanceNetworkOption> NetworkPresets =
            ShooterAcceptanceCatalog.NetworkEnvironments;

        // --- 跨 PlayMode 切换保留的宿主启动配置 ---
        private const string PendingHostAttachKey = "AbilityKit.ShooterDemo.PendingPlayModeAttach";
        private const string HasSavedConfigKey = "AbilityKit.ShooterDemo.HasSavedConfig";
        private const string SyncIndexKey = "AbilityKit.ShooterDemo.SyncIndex";
        private const string NetworkProviderIndexKey = "AbilityKit.ShooterDemo.NetworkProviderIndex";
        private const string NetworkPresetIndexKey = "AbilityKit.ShooterDemo.NetworkPresetIndex";
        private const string AuthoritativeWorldKey = "AbilityKit.ShooterDemo.AuthoritativeWorld";
        private const string ShowDivergenceKey = "AbilityKit.ShooterDemo.ShowDivergence";
        private const string PlayerCountKey = "AbilityKit.ShooterDemo.PlayerCount";
        private const string InitialHpKey = "AbilityKit.ShooterDemo.InitialHp";
        private const string RandomSeedKey = "AbilityKit.ShooterDemo.RandomSeed";
        private const string ControlledPlayerIdKey = "AbilityKit.ShooterDemo.ControlledPlayerId";
        private const string LatencyMsKey = "AbilityKit.ShooterDemo.LatencyMs";
        private const string JitterMsKey = "AbilityKit.ShooterDemo.JitterMs";
        private const string PacketLossRateKey = "AbilityKit.ShooterDemo.PacketLossRate";
        private const string ReorderRateKey = "AbilityKit.ShooterDemo.ReorderRate";
        private const string BandwidthKbpsKey = "AbilityKit.ShooterDemo.BandwidthKbps";

        [MenuItem("Tools/AbilityKit/Shooter Demo")]
        private static void Open()
        {
            GetWindow<ShooterDemoWindow>("Shooter Demo");
        }

        private void OnEnable()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            if (SessionState.GetBool(HasSavedConfigKey, false))
            {
                RestoreConfigFromSessionState();
                ApplyCustomNetwork();
            }
            else
            {
                ApplyNetworkPreset(0);
            }

            // 宿主生命周期独立于窗口挂接状态，订阅后状态栏可以持续显示最新运行状态。
            ShooterHostSessionRegistry.HostsChanged += OnHostLifecycleChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (SessionState.GetBool(PendingHostAttachKey, false) && Application.isPlaying)
            {
                EditorApplication.delayCall += TryStartPendingHostSession;
            }
        }

        private void OnDisable()
        {
            StopInternal();
            ShooterHostSessionRegistry.HostsChanged -= OnHostLifecycleChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnHostLifecycleChanged()
        {
            // 宿主可能由 PlayMode 生命周期或其他窗口改变，这里只负责刷新可见状态。
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingHostAttachKey, false))
            {
                EditorApplication.delayCall += TryStartPendingHostSession;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetBool(PendingHostAttachKey, false);
            }

            // 进入或退出 Play 模式会改变宿主状态与按钮可用性。
            Repaint();
        }

        // =====================================================================
        // 主界面
        // =====================================================================

        private void OnGUI()
        {
            CaptureWindowKeyboardInput();
            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // 左侧：运行配置
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            DrawConfigPanel();
            EditorGUILayout.EndVertical();

            // 右侧：运行诊断
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

            // 会话运行中不允许切换驱动模式。
            EditorGUI.BeginDisabledGroup(_running);
            var newMode = (ShooterDemoDriveMode)EditorGUILayout.EnumPopup(
                _driveMode, EditorStyles.toolbarPopup, GUILayout.Width(130));
            if (newMode != _driveMode)
            {
                _driveMode = newMode;
            }
            EditorGUI.EndDisabledGroup();

            var attachMode = _driveMode == ShooterDemoDriveMode.HostAttach;

            EditorGUI.BeginDisabledGroup(_running);
            var startLabel = attachMode ? "🔗 启动并挂接" : "▶ 启动";
            if (GUILayout.Button(startLabel, EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                StartInternal();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!_running);
            var stopLabel = attachMode ? "✂ 断开" : "■ 停止";
            if (GUILayout.Button(stopLabel, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                StopInternal();
            }
            EditorGUI.EndDisabledGroup();

            if (attachMode)
            {
                // 只依赖发布接口停止宿主，避免窗口绑定具体 PlayMode 宿主实现。
                var hasStoppableHost = ShooterHostSessionRegistry.Active != null;
                EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasStoppableHost);
                if (GUILayout.Button("■ 停止宿主", EditorStyles.toolbarButton, GUILayout.Width(95)))
                {
                    StopPlayModeHostInternal();
                }
                EditorGUI.EndDisabledGroup();
            }

            // 暂停与单步只适用于窗口自驱的 Editor Direct 模式。
            EditorGUI.BeginDisabledGroup(!_running || attachMode);
            _paused = GUILayout.Toggle(_paused, "‖ 暂停", EditorStyles.toolbarButton, GUILayout.Width(70));

            if (GUILayout.Button("▶ 单步", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                StepInternal();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // 速度只影响 Editor 自驱循环。
            EditorGUI.BeginDisabledGroup(attachMode);
            GUILayout.Label("速度", GUILayout.Width(35));
            _timeScale = EditorGUILayout.Slider(_timeScale, 0f, 4f, GUILayout.Width(160));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        // 配置面板
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
            EditorGUILayout.Space(4);
            DrawAcceptanceSpecSection();
        }

        private void DrawSyncModeSection()
        {
            EditorGUILayout.LabelField("同步模式", EditorStyles.boldLabel);

            var syncNames = new string[SyncModes.Count];
            for (int i = 0; i < SyncModes.Count; i++)
            {
                syncNames[i] = SyncModes[i].Implemented
                    ? SyncModes[i].DisplayName
                    : SyncModes[i].DisplayName + " (未接入)";
            }

            EditorGUI.BeginDisabledGroup(_running);
            var newSyncIdx = EditorGUILayout.Popup("模式", _selectedSyncIndex, syncNames);
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
                    EditorGUILayout.HelpBox($"'{mode.DisplayName}' 尚未完成接入。", MessageType.Warning);
                }
            }
        }

        private void DrawNetworkSection()
        {
            EditorGUILayout.LabelField("网络环境", EditorStyles.boldLabel);

            // 网络配置来源下拉框。
            var providers = ShooterNetworkConditionRegistry.All;
            var providerNames = new string[providers.Count];
            for (int i = 0; i < providers.Count; i++)
            {
                providerNames[i] = providers[i].DisplayName;
                if (!providers[i].IsActive) providerNames[i] += " (未激活)";
            }

            var newProviderIdx = EditorGUILayout.Popup("来源", _selectedNetworkProviderIndex, providerNames);
            if (newProviderIdx != _selectedNetworkProviderIndex)
            {
                _selectedNetworkProviderIndex = newProviderIdx;
            }

            // 只有内置来源才显示可调网络参数。
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
                        $"外部网络配置来源 '{provider.DisplayName}' 未激活，请确认对应工具已运行。",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("配置", DescribeProfile(provider.Profile));
                }
            }
        }

        private void DrawBuiltinNetworkSliders()
        {
            // 预设按钮。
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("预设", GUILayout.Width(45));
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

            // 网络参数滑条。
            EditorGUI.BeginChangeCheck();
            _latencyMs = EditorGUILayout.IntSlider("延迟 (ms)", _latencyMs, 0, 500);
            _jitterMs = EditorGUILayout.IntSlider("抖动 (ms)", _jitterMs, 0, 200);
            _packetLossRate = EditorGUILayout.Slider("丢包率", (float)_packetLossRate, 0f, 0.5f);
            _reorderRate = EditorGUILayout.Slider("乱序率", (float)_reorderRate, 0f, 0.5f);
            _bandwidthKbps = EditorGUILayout.IntSlider("带宽 (kbps)", _bandwidthKbps, 0, 10000);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyCustomNetwork();
            }
        }

        private void DrawPlayerConfigSection()
        {
            EditorGUILayout.LabelField("玩家", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_running);
            _playerCount = EditorGUILayout.IntSlider("数量", _playerCount, 1, ShooterGameplay.DefaultMaxPlayers);
            _initialHp = EditorGUILayout.IntField("初始 HP", _initialHp);
            _randomSeed = EditorGUILayout.IntField("随机种子", _randomSeed);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(2);

            _enableAuthoritativeWorld = EditorGUILayout.ToggleLeft(
                "显示权威世界（对比）", _enableAuthoritativeWorld);

            if (_enableAuthoritativeWorld)
            {
                _showDivergence = EditorGUILayout.ToggleLeft(
                    "  显示偏差连线", _showDivergence);
            }
        }

        private void DrawInputSection()
        {
            EditorGUILayout.LabelField("输入", EditorStyles.boldLabel);

            _inputProvider.EnableKeyboardInput = EditorGUILayout.ToggleLeft(
                "启用键盘（WASD + Space）", _inputProvider.EnableKeyboardInput);

            EditorGUI.BeginDisabledGroup(!_inputProvider.EnableKeyboardInput);
            EditorGUILayout.LabelField("按键", _inputProvider.GetDebugKeyString());
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(2);

            var playerOptions = new string[_playerCount];
            for (int i = 0; i < _playerCount; i++)
            {
                playerOptions[i] = $"玩家 {i + 1}";
            }
            _controlledPlayerId = EditorGUILayout.Popup("控制", _controlledPlayerId - 1, playerOptions) + 1;
            _inputProvider.ControlledPlayerId = _controlledPlayerId;
        }

        private void DrawAcceptanceSpecSection()
        {
            EditorGUILayout.LabelField("验收规格", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "运行纯 C# BasicCombat 规格，并把结果写入当前诊断面板。该结果与自动化测试使用同一份规格。",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(_running);
            if (GUILayout.Button("运行 BasicCombat 基线", GUILayout.Height(24)))
            {
                RunBasicCombatSpecBaseline();
            }
            EditorGUI.EndDisabledGroup();
        }

        // =====================================================================
        // 诊断面板
        // =====================================================================

        private void DrawDiagnosticsPanel()
        {
            EditorGUILayout.LabelField("运行诊断", EditorStyles.boldLabel);

            if (!_running && !_showingSpecBaseline)
            {
                EditorGUILayout.HelpBox(
                    "选择同步模式与网络环境后，点击 '▶ 启动' 开始 Shooter 演示。\n" +
                    "Host 挂接模式会自动进入 Play 模式并启动宿主。\n" +
                    "也可以在左侧运行验收规格，直接查看与自动化测试一致的纯 C# 基线结果。",
                    MessageType.Info);
                return;
            }

            // 关键运行统计。
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"帧: {_diagnostics.Frame}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"玩家: {_diagnostics.PlayerCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"子弹: {_diagnostics.BulletCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"回滚: {_diagnostics.TotalRollbacks}", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // 客户端与权威世界偏差。
            if (_enableAuthoritativeWorld)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"最大偏差: {_diagnostics.MaxDivergence:F4}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"偏差数: {_diagnostics.Divergences.Count}", GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }

            // 实体列表。
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("实体", EditorStyles.miniBoldLabel);

            _diagnosticsScroll = EditorGUILayout.BeginScrollView(_diagnosticsScroll, GUILayout.Height(140));
            var clientEntities = _sink.ClientEntities;
            for (int i = 0; i < clientEntities.Count; i++)
            {
                var e = clientEntities[i];
                string label;
                if (e.Kind == ShooterViewEntityKind.Player)
                {
                    label = $"  玩家 #{e.EntityId}  HP:{e.Hp}  分数:{e.Score}  ({e.X:F1}, {e.Y:F1})";
                }
                else
                {
                    label = $"  子弹 #{e.EntityId}  归属:{e.OwnerEntityId}  ({e.X:F1}, {e.Y:F1})  速度:({e.VelocityX:F1}, {e.VelocityY:F1})  剩余帧:{e.RemainingFrames}";
                }
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            // 事件日志。
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"事件（总计: {_diagnostics.TotalEvents}）", EditorStyles.miniBoldLabel);

            _eventsScroll = EditorGUILayout.BeginScrollView(_eventsScroll, GUILayout.Height(120));
            var events = _diagnostics.RecentEvents;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var evt = events[i];
                string label;
                if (evt.EventType == (int)ShooterEventType.Hit)
                {
                    label = $"  [命中] P{evt.SourcePlayerId}→P{evt.TargetPlayerId} 子弹#{evt.BulletId} 位置({evt.X:F1},{evt.Y:F1}) 伤害:{evt.Value}";
                }
                else if (evt.EventType == (int)ShooterEventType.Fire)
                {
                    label = $"  [开火] P{evt.SourcePlayerId} 子弹#{evt.BulletId} 位置({evt.X:F1},{evt.Y:F1})";
                }
                else
                {
                    label = $"  [事件{evt.EventType}] 来源:{evt.SourcePlayerId} 目标:{evt.TargetPlayerId} 值:{evt.Value}";
                }
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        // =====================================================================
        // 状态栏
        // =====================================================================

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(_diagnostics.StatusText, EditorStyles.miniLabel, GUILayout.Width(120));

            if (_running && _session != null)
            {
                EditorGUILayout.LabelField($"同步: {_session.SyncModel}", EditorStyles.miniLabel, GUILayout.Width(160));
                EditorGUILayout.LabelField($"网络: {_session.NetworkName}", EditorStyles.miniLabel, GUILayout.Width(140));
            }

            GUILayout.FlexibleSpace();

            // PlayMode 宿主生命周期独立于窗口挂接状态，这里直接暴露给使用者。
            if (_driveMode == ShooterDemoDriveMode.HostAttach)
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
        /// 生成 PlayMode 宿主生命周期提示；宿主运行状态与当前窗口是否已挂接是两个独立概念。
        /// </summary>
        private string GetPlayModeHostStatusText()
        {
            if (SessionState.GetBool(PendingHostAttachKey, false))
            {
                return "宿主: 等待进入 Play";
            }

            if (!Application.isPlaying)
            {
                return "宿主: 未进入 Play";
            }

            if (ShooterPlayModeSessionHost.IsRunning)
            {
                return _attachedHost?.IsRunning == true
                    ? "宿主: 运行中 / 已挂接"
                    : "宿主: 运行中 / 可挂接";
            }

            if (ShooterPlayModeSessionHost.IsInstalled)
            {
                return "宿主: 已安装 / 未启动";
            }

            return "宿主: 空闲";
        }

        // =====================================================================
        // 键盘输入捕获
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

            if (_driveMode == ShooterDemoDriveMode.HostAttach)
            {
                AttachInternal();
                return;
            }

            try
            {
                _lastError = string.Empty;
                _showingSpecBaseline = false;

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

            if (_running || _showingSpecBaseline)
            {
                EditorApplication.update -= OnEditorUpdate;
                SceneView.duringSceneGui -= OnSceneViewGUI;
            }

            _running = false;
            _paused = false;
            _showingSpecBaseline = false;
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
                var host = ShooterHostSessionRegistry.Active;
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

        private void RunBasicCombatSpecBaseline()
        {
            try
            {
                var runner = new ShooterAcceptanceSpecRunner();
                var result = runner.Run(ShooterAcceptanceSpecs.BasicCombat);

                _sink.Clear();
                _sink.ShowAuthorityWorld = false;
                _sink.ShowDivergence = false;

                using var session = ShooterAcceptanceLab.Create(
                    NetworkSyncModel.PredictRollback,
                    NetworkConditionProfile.Ideal,
                    players: ShooterAcceptanceSpecs.BasicCombat.Start.Players,
                    randomSeed: ShooterAcceptanceSpecs.BasicCombat.Start.RandomSeed,
                    enableAuthoritativeWorld: false);
                var snapshot = result.Snapshot;
                session.Presentation.ApplyLocalPredictionSnapshot(in snapshot);
                var clientBatch = session.Presentation.ViewModel.Current;
                ShooterHostPresentationFrame frame = new ShooterHostPresentationFrame(
                    clientBatch,
                    ShooterSnapshotViewBatch.Empty,
                    false,
                    1,
                    1f,
                    null,
                    null,
                    default,
                    null);
                _sink.Render(in frame);

                _diagnostics.Reset();
                _diagnostics.Frame = result.Frame;
                _diagnostics.PlayerCount = result.Snapshot.Players.Length;
                _diagnostics.BulletCount = result.Snapshot.Bullets.Length;
                _diagnostics.RecentEvents = result.Events;
                _diagnostics.TotalEvents = result.Events.Count;

                _showingSpecBaseline = true;
                SceneView.duringSceneGui -= OnSceneViewGUI;
                SceneView.duringSceneGui += OnSceneViewGUI;

                _lastError = $"BasicCombat 规格通过：Frame={result.Frame}, Hash={result.StateHash:X8}";
                SceneView.RepaintAll();
                Repaint();
            }
            catch (Exception ex)
            {
                _lastError = $"BasicCombat spec failed: {ex.Message}";
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
                if (!TryBuildSessionOptions(out _))
                {
                    Repaint();
                    return;
                }

                SaveConfigToSessionState();
                SessionState.SetBool(PendingHostAttachKey, true);
                _lastError = "已保存当前配置，正在进入 Play 模式并启动 Shooter 宿主。";
                EditorApplication.isPlaying = true;
                Repaint();
                return;
            }

            if (!TryBuildSessionOptions(out var options))
            {
                Repaint();
                return;
            }

            var host = EnsurePlayModeHost(options);
            if (host == null)
            {
                _lastError = "未能发布 PlayMode 会话宿主。";
                Repaint();
                return;
            }

            _attachedHost = host;
            _session = host.Session;
            _running = true;
            _paused = false;
            _diagnostics.Reset();
            _diagnostics.IsRunning = host.IsRunning;

            // 窗口只观察 PlayMode 宿主，不再自行推进逻辑；网络调参通过共享 registry 热更新。
            EditorApplication.update -= OnAttachUpdate;
            ShooterHostSessionRegistry.HostsChanged -= OnRegistryHostsChanged;
            EditorApplication.update += OnAttachUpdate;
            ShooterHostSessionRegistry.HostsChanged += OnRegistryHostsChanged;

            Repaint();
        }

        private void DetachInternal()
        {
            EditorApplication.update -= OnAttachUpdate;
            ShooterHostSessionRegistry.HostsChanged -= OnRegistryHostsChanged;

            _attachedHost = null;
            _session = null;
            _running = false;
            _paused = false;
            _diagnostics.Reset();

            Repaint();
        }

        private void OnRegistryHostsChanged()
        {
            // 宿主随 Play 模式退出而消失时，窗口自动解除挂接。
            if (_attachedHost != null && !_attachedHost.IsRunning &&
                ShooterHostSessionRegistry.Active == null)
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

            // 宿主已销毁或 Play 模式退出时，窗口自动回到未运行状态。
            if (!_attachedHost.IsRunning && _attachedHost.Session == null)
            {
                _lastError = "PlayMode 会话已结束。";
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
                _lastError = "当前选择的同步模式尚未实现。";
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

        private void TryStartPendingHostSession()
        {
            if (!SessionState.GetBool(PendingHostAttachKey, false) || !Application.isPlaying)
            {
                return;
            }

            RestoreConfigFromSessionState();
            ApplyCustomNetwork();
            SessionState.SetBool(PendingHostAttachKey, false);

            if (!TryBuildSessionOptions(out var options))
            {
                Repaint();
                return;
            }

            var host = EnsurePlayModeHost(options);
            if (host == null)
            {
                _lastError = "进入 Play 模式后未能启动 Shooter 演示宿主。";
                Repaint();
                return;
            }

            _driveMode = ShooterDemoDriveMode.HostAttach;
            _attachedHost = host;
            _session = host.Session;
            _running = true;
            _paused = false;
            _diagnostics.Reset();
            _diagnostics.IsRunning = host.IsRunning;

            EditorApplication.update -= OnAttachUpdate;
            ShooterHostSessionRegistry.HostsChanged -= OnRegistryHostsChanged;
            EditorApplication.update += OnAttachUpdate;
            ShooterHostSessionRegistry.HostsChanged += OnRegistryHostsChanged;

            _lastError = string.Empty;
            Repaint();
        }

        private static IShooterSessionHost? EnsurePlayModeHost(ShooterPlayModeSessionOptions options)
        {
            var host = ShooterHostSessionRegistry.Active;
            if (host == null || !host.IsRunning)
            {
                ShooterPlayModeSessionHost.Start(options);
                host = ShooterHostSessionRegistry.Active;
            }

            return host;
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

            // 限制单帧推进量，避免编辑器卡顿后一次性追帧过多。
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
            if (!_running && !_showingSpecBaseline) return;
            _sink.DrawSceneView();
        }

        // =====================================================================
        // Diagnostics Update
        // =====================================================================

        private void UpdateDiagnostics(in ShooterStateSnapshotPayload snapshot, object tickResult)
        {
            if (_session == null) return;

            var diagnostics = ShooterHostDiagnosticsProjector.ProjectFromSession(
                _session,
                in snapshot,
                _diagnostics.TotalEvents);
            _diagnostics.Apply(in diagnostics);
        }

        // =====================================================================
        // Network Helpers
        // =====================================================================

        private void SaveConfigToSessionState()
        {
            SessionState.SetBool(HasSavedConfigKey, true);
            SessionState.SetInt(SyncIndexKey, _selectedSyncIndex);
            SessionState.SetInt(NetworkProviderIndexKey, _selectedNetworkProviderIndex);
            SessionState.SetInt(NetworkPresetIndexKey, _selectedNetworkPresetIndex);
            SessionState.SetBool(AuthoritativeWorldKey, _enableAuthoritativeWorld);
            SessionState.SetBool(ShowDivergenceKey, _showDivergence);
            SessionState.SetInt(PlayerCountKey, _playerCount);
            SessionState.SetInt(InitialHpKey, _initialHp);
            SessionState.SetInt(RandomSeedKey, _randomSeed);
            SessionState.SetInt(ControlledPlayerIdKey, _controlledPlayerId);
            SessionState.SetInt(LatencyMsKey, _latencyMs);
            SessionState.SetInt(JitterMsKey, _jitterMs);
            SessionState.SetFloat(PacketLossRateKey, (float)_packetLossRate);
            SessionState.SetFloat(ReorderRateKey, (float)_reorderRate);
            SessionState.SetInt(BandwidthKbpsKey, _bandwidthKbps);
        }

        private void RestoreConfigFromSessionState()
        {
            _selectedSyncIndex = ClampIndex(SessionState.GetInt(SyncIndexKey, _selectedSyncIndex), SyncModes.Count);
            _selectedNetworkProviderIndex = Math.Max(0, SessionState.GetInt(NetworkProviderIndexKey, _selectedNetworkProviderIndex));
            _selectedNetworkPresetIndex = ClampIndex(SessionState.GetInt(NetworkPresetIndexKey, _selectedNetworkPresetIndex), NetworkPresets.Count);
            _enableAuthoritativeWorld = SessionState.GetBool(AuthoritativeWorldKey, _enableAuthoritativeWorld);
            _showDivergence = SessionState.GetBool(ShowDivergenceKey, _showDivergence);
            _playerCount = Math.Max(1, Math.Min(ShooterGameplay.DefaultMaxPlayers, SessionState.GetInt(PlayerCountKey, _playerCount)));
            _initialHp = Math.Max(1, SessionState.GetInt(InitialHpKey, _initialHp));
            _randomSeed = SessionState.GetInt(RandomSeedKey, _randomSeed);
            _controlledPlayerId = Math.Max(1, Math.Min(_playerCount, SessionState.GetInt(ControlledPlayerIdKey, _controlledPlayerId)));
            _latencyMs = Math.Max(0, SessionState.GetInt(LatencyMsKey, _latencyMs));
            _jitterMs = Math.Max(0, SessionState.GetInt(JitterMsKey, _jitterMs));
            _packetLossRate = Clamp01(SessionState.GetFloat(PacketLossRateKey, (float)_packetLossRate));
            _reorderRate = Clamp01(SessionState.GetFloat(ReorderRateKey, (float)_reorderRate));
            _bandwidthKbps = Math.Max(0, SessionState.GetInt(BandwidthKbpsKey, _bandwidthKbps));
            _inputProvider.ControlledPlayerId = _controlledPlayerId;
        }

        private static int ClampIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(count - 1, value));
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }

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

            // 会话运行中立即通过共享 runner 路径热更新网络配置。
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

            // 优先显示匹配的预设名称，否则显示自定义参数摘要。
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

            return $"自定义 ({_latencyMs}ms/{_jitterMs}ms)";
        }

        private static string DescribeProfile(NetworkConditionProfile p)
        {
            return $"{p.BaseLatencyMs}ms ±{p.JitterMs}ms  丢包:{p.PacketLossRate:P1}  乱序:{p.ReorderRate:P1}  带宽:{p.BandwidthKbps}kbps";
        }

        private static string GetShortPresetName(string displayName)
        {
            // 从预设显示名里取括号前的短名，保持按钮宽度稳定。
            var paren = displayName.IndexOf('(');
            if (paren > 0) return displayName.Substring(0, paren).Trim();
            return displayName;
        }
    }
}
