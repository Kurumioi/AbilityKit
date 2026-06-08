using System;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudInputControlFactory
    {
        public static JoystickAreaView CreateMoveJoystick(Transform parent, Canvas canvas)
        {
            var joystickArea = new GameObject("MoveJoystick", typeof(RectTransform), typeof(Image));
            joystickArea.transform.SetParent(parent, worldPositionStays: false);

            var joystickAreaRt = joystickArea.GetComponent<RectTransform>();
            BattleHudRectTransformLayout.SetAnchored(
                joystickAreaRt,
                Vector2.zero,
                Vector2.zero,
                new Vector2(180f, 180f),
                new Vector2(360f, 360f));

            var areaImg = joystickArea.GetComponent<Image>();
            areaImg.color = new Color(1f, 1f, 1f, 0.001f);
            areaImg.raycastTarget = true;

            var outerRt = CreateJoystickPart(
                joystickArea.transform,
                "Outer",
                new Vector2(220f, 220f),
                new Color(1f, 1f, 1f, 0.15f),
                raycastTarget: true);
            var innerRt = CreateJoystickPart(
                joystickArea.transform,
                "Inner",
                new Vector2(90f, 90f),
                new Color(1f, 1f, 1f, 0.25f),
                raycastTarget: false);

            var joystick = joystickArea.AddComponent<JoystickAreaView>();
            joystick.Initialize(joystickAreaRt, outerRt, innerRt, canvas, JoystickConfig.Default);
            return joystick;
        }

        public static SkillButtonView CreateSkillButton(
            Transform parent,
            RectTransform root,
            Canvas canvas,
            int slot,
            string name,
            Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);

            var rt = go.GetComponent<RectTransform>();
            BattleHudRectTransformLayout.SetAnchored(
                rt,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                anchoredPos,
                new Vector2(110f, 110f));

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.2f);
            img.raycastTarget = true;

            var cfg = SkillButtonConfig.Default;
            cfg.EnableAim = true;
            cfg.AimMaxRadius = 220f;
            cfg.AimMode = slot == 1 ? SkillAimMode.Direction : SkillAimMode.Point;

            var view = go.AddComponent<SkillButtonView>();
            view.Initialize(rt, root, canvas, cfg);
            return view;
        }

        public static Button CreateInfoButton(Transform parent, Vector2 anchoredPos, Action clicked)
        {
            var go = new GameObject("Info", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, worldPositionStays: false);

            var rt = go.GetComponent<RectTransform>();
            BattleHudRectTransformLayout.SetAnchored(
                rt,
                Vector2.one,
                Vector2.one,
                anchoredPos,
                new Vector2(90f, 45f));

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.18f);
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            if (clicked != null)
            {
                btn.onClick.AddListener(() => clicked());
            }

            return btn;
        }

        private static RectTransform CreateJoystickPart(
            Transform parent,
            string name,
            Vector2 size,
            Color color,
            bool raycastTarget)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);

            var rt = go.GetComponent<RectTransform>();
            BattleHudRectTransformLayout.SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycastTarget;
            return rt;
        }
    }
}
