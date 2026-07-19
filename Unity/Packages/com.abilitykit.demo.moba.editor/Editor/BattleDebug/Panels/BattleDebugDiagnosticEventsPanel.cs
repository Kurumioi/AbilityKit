using System.Collections.Generic;
using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Game.Editor.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AbilityKit.Game.Editor
{
    /// <summary>
    /// 诊断事件面板（IMGUI 绘制层）：通过 <see cref="BattleDebugDiagnosticEventsViewModel"/>
    /// 持有查询/状态逻辑，本类只负责将 ViewModel 暴露的 DTO 渲染为 IMGUI。
    /// 支持按选中实体 ActorId 过滤、仅看失败、文本搜索。
    /// 只消费已定义的诊断查询契约，不建立旁路数据源。
    /// </summary>
    internal sealed class BattleDebugDiagnosticEventsPanel : IBattleDebugPanel
    {
        public string Name => "诊断事件";
        public int Order => 400;

        private readonly BattleDebugDiagnosticEventsViewModel _viewModel = new BattleDebugDiagnosticEventsViewModel();
        private Vector2 _scroll;

        public bool IsVisible(in BattleDebugContext ctx) => true;

        public void Draw(in BattleDebugContext ctx)
        {
            if (!BattleDebugDiagnosticSessionResolver.TryResolve(in ctx, out var session))
            {
                EditorGUILayout.HelpBox(
                    "诊断会话不可用。请确认战斗已启动且诊断 Local Session 已注册。",
                    MessageType.Info);
                return;
            }

            DrawFilterBar(ctx, session);
            EditorGUILayout.Space(4);

            var selectedActorId = ctx.HasSelection ? ctx.SelectedId.ActorId : 0;
            var items = _viewModel.RefreshIfNeeded(session, selectedActorId, ctx.HasSelection);
            DrawEventList(items);
        }

        private void DrawFilterBar(in BattleDebugContext ctx, IBattleDiagnosticReadOnlySession session)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var newFilterActor = GUILayout.Toggle(_viewModel.FilterBySelectedActor, "仅选中实体", EditorStyles.toolbarButton);
            if (newFilterActor != _viewModel.FilterBySelectedActor)
            {
                _viewModel.FilterBySelectedActor = newFilterActor;
                _viewModel.InvalidateCache();
            }

            if (_viewModel.FilterBySelectedActor && !ctx.HasSelection)
            {
                GUILayout.Label("（未选中）", EditorStyles.miniLabel, GUILayout.Width(60));
            }

            var newFailures = GUILayout.Toggle(_viewModel.FailuresOnly, "仅失败", EditorStyles.toolbarButton);
            if (newFailures != _viewModel.FailuresOnly)
            {
                _viewModel.FailuresOnly = newFailures;
                _viewModel.InvalidateCache();
            }

            GUILayout.Label("搜索", GUILayout.Width(35));
            var newSearch = GUILayout.TextField(_viewModel.SearchText ?? string.Empty, GUI.skin.textField, GUILayout.MinWidth(80));
            if (!string.Equals(newSearch, _viewModel.SearchText, System.StringComparison.Ordinal))
            {
                _viewModel.SearchText = newSearch;
                _viewModel.InvalidateCache();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _viewModel.InvalidateCache();
                ctx.RequestRepaint?.Invoke();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"StoreRevision={_viewModel.StoreRevision}  事件数={_viewModel.Items?.Count ?? 0}",
                EditorStyles.miniLabel);
        }

        private void DrawEventList(IReadOnlyList<BattleDiagnosticEvent> items)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (!string.IsNullOrEmpty(_viewModel.StatusMessage))
            {
                EditorGUILayout.HelpBox(_viewModel.StatusMessage, MessageType.None);
            }

            if (items == null || items.Count == 0)
            {
                EditorGUILayout.LabelField("（无事件）", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    DrawEventRow(items[i]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawEventRow(in BattleDiagnosticEvent evt)
        {
            var outcomeColor = GetOutcomeColor(evt.Outcome);
            var oldColor = GUI.color;

            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            GUI.color = outcomeColor;
            GUILayout.Label($"#{evt.Sequence}", GUILayout.Width(70));
            GUI.color = oldColor;

            GUILayout.Label($"F{evt.Frame}", GUILayout.Width(50));
            GUILayout.Label(evt.Kind.ToString(), GUILayout.Width(120));
            GUILayout.Label(evt.Outcome.ToString(), GUILayout.Width(70));

            if (evt.SourceActorId != 0)
            {
                GUILayout.Label($"src={evt.SourceActorId}", EditorStyles.miniLabel, GUILayout.Width(70));
            }

            if (evt.TargetActorId != 0)
            {
                GUILayout.Label($"tgt={evt.TargetActorId}", EditorStyles.miniLabel, GUILayout.Width(70));
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(evt.Summary, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private static Color GetOutcomeColor(BattleDiagnosticEventOutcome outcome)
        {
            switch (outcome)
            {
                case BattleDiagnosticEventOutcome.Failed:
                    return new Color(1f, 0.6f, 0.6f);
                case BattleDiagnosticEventOutcome.Cancelled:
                case BattleDiagnosticEventOutcome.Interrupted:
                    return new Color(1f, 0.85f, 0.5f);
                case BattleDiagnosticEventOutcome.None:
                    return new Color(0.85f, 0.85f, 0.85f);
                default:
                    return Color.white;
            }
        }
    }
}
