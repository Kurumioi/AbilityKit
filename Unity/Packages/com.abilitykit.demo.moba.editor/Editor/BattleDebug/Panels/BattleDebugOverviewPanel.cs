using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Game.Editor.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AbilityKit.Game.Editor
{
    internal sealed class BattleDebugOverviewPanel : IBattleDebugPanel
    {
        public string Name => "总览";
        public int Order => 0;

        private readonly BattleDebugDiagnosticOverviewViewModel _viewModel =
            new BattleDebugDiagnosticOverviewViewModel();

        public bool IsVisible(in BattleDebugContext ctx) => true;

        public void Draw(in BattleDebugContext ctx)
        {
            if (!ctx.HasSelection)
            {
                EditorGUILayout.HelpBox("请先选择一个实体。", MessageType.Info);
                return;
            }

            if (!BattleDebugDiagnosticSessionResolver.TryResolve(in ctx, out var session))
            {
                EditorGUILayout.HelpBox(
                    "诊断会话不可用。请确认战斗已启动且诊断 Local Session 已注册。",
                    MessageType.Info);
                return;
            }

            const BattleDiagnosticCapabilities requiredCapabilities =
                BattleDiagnosticCapabilities.ActorState |
                BattleDiagnosticCapabilities.ActorTags |
                BattleDiagnosticCapabilities.ActorEffects;
            if ((session.SessionInfo.Capabilities & requiredCapabilities) != requiredCapabilities)
            {
                EditorGUILayout.HelpBox(
                    "当前诊断会话不支持总览所需的 Actor、标签或 Effect 查询。",
                    MessageType.Info);
                return;
            }

            var actorId = ctx.SelectedId.ActorId;
            _viewModel.RefreshIfNeeded(session, actorId);

            if (!string.IsNullOrEmpty(_viewModel.StatusMessage))
            {
                EditorGUILayout.HelpBox(_viewModel.StatusMessage, MessageType.None);
            }

            EditorGUILayout.LabelField("实体", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ID", actorId.ToString());
            if (_viewModel.Actor.HasValue)
            {
                var actor = _viewModel.Actor.Value;
                EditorGUILayout.LabelField("类型", actor.Kind.ToString());
                EditorGUILayout.LabelField("名称", actor.DisplayName);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("汇总", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("标签数", _viewModel.TagCount.ToString());
            EditorGUILayout.LabelField("效果数", _viewModel.EffectCount.ToString());
            EditorGUILayout.LabelField(
                $"State={_viewModel.StateStoreRevision} Tag={_viewModel.TagStoreRevision} " +
                $"Effect={_viewModel.EffectStoreRevision}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("复制 ID", GUILayout.Width(100)))
            {
                EditorGUIUtility.systemCopyBuffer = actorId.ToString();
            }

            if (GUILayout.Button("复制标签", GUILayout.Width(100)))
            {
                EditorGUIUtility.systemCopyBuffer = _viewModel.BuildTagList();
            }

            if (GUILayout.Button("刷新", GUILayout.Width(100)))
            {
                _viewModel.InvalidateCache();
                ctx.RequestRepaint?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
