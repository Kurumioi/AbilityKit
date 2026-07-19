using System;
using AbilityKit.Game.Battle.Shared.Assets;
using AbilityKit.Game.Flow;
using UnityEngine;

namespace AbilityKit.Game.Battle.Presentation.Features.Loading
{
    /// <summary>
    /// 战斗加载界面 Feature。挂载在 <c>Battle.LoadAssets</c> 阶段：
    /// - 在 OnAttach 时构建并启动 <see cref="BattleAssetLoadCoordinator"/>
    /// - 订阅 <see cref="BattleAssetLoadProgressHub"/> 接收进度
    /// - OnGUI 渲染居中加载卡片 + 进度条 + 当前资源名 + 取消/重试/返回大厅按钮
    /// - 加载成功时由上层流程自动推进到 Battle.InMatch；失败/取消时显示错误并允许重试/返回大厅
    ///
    /// 依赖注入约定：
    /// - 外部可以通过 <see cref="IBattleAssetLoadCoordinator"/> 的环境注册或直接调用
    ///   <see cref="BattleAssetLoadProgressHub.NotifyProgressed"/> 推动 UI。
    /// - 若 OnAttach 时本 feature 还没拿到 coordinator，会进入"等待驱动"模式（显示提示文字），
    ///   直到 hub 被调用或外部调用 <see cref="InjectCoordinator"/>。
    /// </summary>
    public sealed class BattleLoadingScreenFeature : IGamePhaseFeature, IOnGUIFeature, IBattleAssetLoadProgressObserver
    {
        private readonly BattleAssetLoadProgressSnapshot _snapshot = new BattleAssetLoadProgressSnapshot();

        private IBattleAssetLoadCoordinator _coordinator;
        private IFlowCommandSink _flowSink;
        private bool _started;
        private string _statusLine = "Initializing...";
        private bool _show = true;

        public BattleLoadingScreenFeature()
        {
        }

        // 测试 / DI 入口
        internal BattleLoadingScreenFeature(IBattleAssetLoadCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void OnAttach(in GamePhaseContext ctx)
        {
            BattleAssetLoadProgressHub.Register(this);
            _flowSink = ctx.Entry.Get<IFlowCommandSink>();

            // 默认行为：若没有外部注入 coordinator，feature 进入 idle 等驱动模式。
            // 这样即使没有 DI 链也能挂上去不崩，UI 会显示"等待驱动方"。
            if (_coordinator == null)
            {
                _statusLine = "Waiting for loader...";
            }
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            BattleAssetLoadProgressHub.Unregister(this);

            if (_coordinator != null && _coordinator.IsLoading)
            {
                try
                {
                    _coordinator.Cancel();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[BattleLoadingScreen] Cancel failed: " + ex.Message);
                }
            }
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }

        public void OnGUI(in GamePhaseContext ctx)
        {
            if (!_show) return;
            DrawLoadingCard();
        }

        // ===== Progress observer (called by hub) =====

        void IBattleAssetLoadProgressObserver.OnLoadStarted(BattleAssetLoadProgressSnapshot snapshot)
        {
            CopyFrom(snapshot);
            _statusLine = $"Loading {snapshot.TotalCount} asset(s)...";
        }

        void IBattleAssetLoadProgressObserver.OnLoadProgressed(BattleAssetLoadProgressSnapshot snapshot)
        {
            CopyFrom(snapshot);
            if (!string.IsNullOrEmpty(snapshot.CurrentAssetKey))
            {
                _statusLine = $"[{snapshot.LoadedCount}/{snapshot.TotalCount}] {snapshot.CurrentAssetKey}";
            }
            else
            {
                _statusLine = $"[{snapshot.LoadedCount}/{snapshot.TotalCount}]";
            }
        }

        void IBattleAssetLoadProgressObserver.OnLoadCompleted(BattleAssetLoadProgressSnapshot snapshot)
        {
            CopyFrom(snapshot);
            _statusLine = snapshot.Success
                ? "Load complete"
                : "Load failed: " + (snapshot.ErrorMessage ?? "unknown");
        }

        void IBattleAssetLoadProgressObserver.OnLoadCancelled(BattleAssetLoadProgressSnapshot snapshot)
        {
            CopyFrom(snapshot);
            _statusLine = "Cancelled";
        }

        // ===== Public hooks =====

        public BattleAssetLoadProgressSnapshot CurrentSnapshot => _snapshot;

        /// <summary>
        /// 注入 coordinator 并立即启动加载。供 GameFlow/Bootstrap 阶段使用。
        /// </summary>
        internal void InjectCoordinator(IBattleAssetLoadCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            StartWith(_coordinator);
        }

        // ===== Internal =====

        private void CopyFrom(BattleAssetLoadProgressSnapshot src)
        {
            _snapshot.IsLoading = src.IsLoading;
            _snapshot.LoadedCount = src.LoadedCount;
            _snapshot.TotalCount = src.TotalCount;
            _snapshot.CurrentAssetKey = src.CurrentAssetKey;
            _snapshot.Completed = src.Completed;
            _snapshot.Success = src.Success;
            _snapshot.ErrorMessage = src.ErrorMessage;
            _snapshot.Errors = src.Errors;
        }

        private void StartWith(IBattleAssetLoadCoordinator coordinator)
        {
            if (_started) return;
            _started = true;

            var snap = new BattleAssetLoadProgressSnapshot { IsLoading = true };
            try
            {
                coordinator.StartLoading(success =>
                {
                    snap.Completed = true;
                    snap.IsLoading = false;
                    snap.Success = success;
                    if (!success)
                    {
                        snap.ErrorMessage = "Load failed";
                    }
                    BattleAssetLoadProgressHub.NotifyCompleted(snap);
                });

                BattleAssetLoadProgressHub.NotifyStarted(snap);
            }
            catch (Exception ex)
            {
                snap.Completed = true;
                snap.IsLoading = false;
                snap.Success = false;
                snap.ErrorMessage = ex.Message;
                BattleAssetLoadProgressHub.NotifyCompleted(snap);
                Debug.LogWarning("[BattleLoadingScreen] Start failed: " + ex.Message);
            }
        }

        // ===== Rendering =====

        private void DrawLoadingCard()
        {
            const float cardWidth = 480f;
            const float cardHeight = 200f;
            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.5f;
            var rect = new Rect(cx - cardWidth * 0.5f, cy - cardHeight * 0.5f, cardWidth, cardHeight);

            // dim background
            var dim = new Rect(0f, 0f, Screen.width, Screen.height);
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(dim, Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Loading Battle Assets", BoldHeaderStyle());
            GUILayout.Space(8f);

            GUILayout.Label(_statusLine);

            var progress = Mathf.Clamp01(_snapshot.Progress01);
            DrawProgressBar(progress);

            GUILayout.Space(8f);
            GUILayout.Label($"{_snapshot.LoadedCount} / {_snapshot.TotalCount}  ({Mathf.RoundToInt(progress * 100f)}%)");

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (_snapshot.IsLoading)
            {
                if (GUILayout.Button("Cancel", GUILayout.Height(32f)))
                {
                    try { _coordinator?.Cancel(); }
                    catch (Exception ex) { Debug.LogWarning(ex.Message); }
                }
            }
            else if (_snapshot.Completed && !_snapshot.Success)
            {
                if (GUILayout.Button("Retry", GUILayout.Height(32f)))
                {
                    _started = false;
                    if (_coordinator != null) StartWith(_coordinator);
                }
                if (GUILayout.Button("Back to Lobby", GUILayout.Height(32f)))
                {
                    _flowSink?.RequestReturnLobby();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static GUIStyle _boldHeaderCache;
        private static GUIStyle BoldHeaderStyle()
        {
            if (_boldHeaderCache != null) return _boldHeaderCache;
            _boldHeaderCache = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 18 };
            return _boldHeaderCache;
        }

        private static void DrawProgressBar(float progress01)
        {
            const float barHeight = 22f;
            var rect = GUILayoutUtility.GetRect(0f, barHeight, GUILayout.ExpandWidth(true));
            var prev = GUI.color;

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            var fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress01), rect.height);
            GUI.color = new Color(0.25f, 0.75f, 0.95f, 1f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            GUI.color = prev;
        }
    }
}