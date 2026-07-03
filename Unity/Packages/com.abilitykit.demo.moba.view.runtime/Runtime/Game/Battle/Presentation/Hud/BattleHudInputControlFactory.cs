using System;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputControlFactory
    {
        private readonly BattleHudImageElementFactory _images;

        public BattleHudInputControlFactory()
            : this(new BattleHudImageElementFactory())
        {
        }

        public BattleHudInputControlFactory(BattleHudImageElementFactory images)
        {
            _images = images ?? new BattleHudImageElementFactory();
        }

        public JoystickAreaView CreateMoveJoystick(Transform parent, Canvas canvas)
        {
            var area = _images.Create(
                "MoveJoystick",
                parent,
                Vector2.zero,
                Vector2.zero,
                new Vector2(180f, 180f),
                new Vector2(360f, 360f),
                new Color(1f, 1f, 1f, 0.001f),
                raycastTarget: true);

            var outerRt = CreateJoystickPart(
                area.GameObject.transform,
                "Outer",
                new Vector2(220f, 220f),
                new Color(1f, 1f, 1f, 0.15f),
                raycastTarget: true);
            var innerRt = CreateJoystickPart(
                area.GameObject.transform,
                "Inner",
                new Vector2(90f, 90f),
                new Color(1f, 1f, 1f, 0.25f),
                raycastTarget: false);

            var joystick = area.GameObject.AddComponent<JoystickAreaView>();
            joystick.Initialize(area.Rect, outerRt, innerRt, canvas, JoystickConfig.Default);
            return joystick;
        }

        public SkillButtonView CreateSkillButton(
            Transform parent,
            RectTransform root,
            Canvas canvas,
            int slot,
            string name,
            Vector2 anchoredPos)
        {
            var element = _images.Create(
                name,
                parent,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                anchoredPos,
                new Vector2(110f, 110f),
                new Color(1f, 1f, 1f, 0.2f),
                raycastTarget: true);

            var cfg = SkillButtonConfig.Default;
            cfg.EnableAim = true;
            cfg.AimMaxRadius = 220f;
            cfg.AimMode = slot == 1 ? SkillAimMode.Direction : SkillAimMode.Point;

            var indicator = CreateSkillAimIndicator(parent, name + "AimIndicator");
            var view = element.GameObject.AddComponent<SkillButtonView>();
            view.Initialize(element.Rect, root, canvas, cfg, indicator);
            return view;
        }

        public Button CreateInfoButton(Transform parent, Vector2 anchoredPos, Action clicked)
        {
            var element = _images.Create(
                "Info",
                parent,
                Vector2.one,
                Vector2.one,
                anchoredPos,
                new Vector2(90f, 45f),
                new Color(1f, 1f, 1f, 0.18f),
                raycastTarget: true,
                typeof(Button));

            var btn = element.GameObject.GetComponent<Button>();
            if (clicked != null)
            {
                btn.onClick.AddListener(() => clicked());
            }

            return btn;
        }

        private SkillAimIndicatorView CreateSkillAimIndicator(Transform parent, string name)
        {
            var indicatorGo = new GameObject(name, typeof(RectTransform));
            indicatorGo.transform.SetParent(parent, worldPositionStays: false);
            indicatorGo.SetActive(false);

            var indicatorRt = indicatorGo.GetComponent<RectTransform>();
            BattleHudRectTransformLayout.StretchToParent(indicatorRt);

            var ring = _images.Create(
                "Ring",
                indicatorGo.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(160f, 160f),
                new Color(0.2f, 0.75f, 1f, 0.18f),
                raycastTarget: false).Rect;
            var dot = _images.Create(
                "Dot",
                indicatorGo.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(42f, 42f),
                new Color(0.2f, 0.75f, 1f, 0.65f),
                raycastTarget: false).Rect;

            var indicator = indicatorGo.AddComponent<SkillAimIndicatorView>();
            indicator.Initialize(ring, dot);
            return indicator;
        }

        private RectTransform CreateJoystickPart(
            Transform parent,
            string name,
            Vector2 size,
            Color color,
            bool raycastTarget)
        {
            return _images.Create(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                size,
                color,
                raycastTarget).Rect;
        }
    }
}
