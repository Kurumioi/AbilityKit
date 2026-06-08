using System;
using AbilityKit.Game.Battle.View;
using UnityEngine;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudInputUiFactory
    {
        public static BattleHudInputUi Create(RectTransform root, Canvas canvas, Transform cameraTransform, Action infoClicked)
        {
            var inputUiRoot = new GameObject("BattleHudInput", typeof(RectTransform));
            inputUiRoot.transform.SetParent(root, worldPositionStays: false);
            inputUiRoot.SetActive(false);

            BattleHudRectTransformLayout.StretchToParent(inputUiRoot.GetComponent<RectTransform>());

            var inputView = inputUiRoot.AddComponent<BattleHudInputView>();
            var moveJoystick = BattleHudInputControlFactory.CreateMoveJoystick(inputUiRoot.transform, canvas);
            var moveMapper = inputUiRoot.AddComponent<BattleHudMoveInputMapper>();
            var skillAimMapper = inputUiRoot.AddComponent<BattleHudSkillAimInputMapper>();

            var skill1 = BattleHudInputControlFactory.CreateSkillButton(
                inputUiRoot.transform,
                root,
                canvas,
                1,
                "Skill1",
                new Vector2(-260f, 200f));
            var skill2 = BattleHudInputControlFactory.CreateSkillButton(
                inputUiRoot.transform,
                root,
                canvas,
                2,
                "Skill2",
                new Vector2(-140f, 110f));
            var skill3 = BattleHudInputControlFactory.CreateSkillButton(
                inputUiRoot.transform,
                root,
                canvas,
                3,
                "Skill3",
                new Vector2(-120f, 260f));
            var infoButton = BattleHudInputControlFactory.CreateInfoButton(
                inputUiRoot.transform,
                new Vector2(-80f, -80f),
                infoClicked);

            inputView.Initialize(moveJoystick, skill1, skill2, skill3);
            moveMapper.Initialize(inputView, cameraTransform);
            skillAimMapper.Initialize(inputView, cameraTransform);

            inputUiRoot.SetActive(true);

            return new BattleHudInputUi(
                inputUiRoot,
                inputView,
                moveJoystick,
                moveMapper,
                skillAimMapper,
                skill1,
                skill2,
                skill3,
                infoButton);
        }
    }
}
