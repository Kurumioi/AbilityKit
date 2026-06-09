using System;
using AbilityKit.Game.Battle.View;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputUiFactory
    {
        private readonly BattleHudInputControlFactory _controls;

        public BattleHudInputUiFactory()
            : this(new BattleHudInputControlFactory())
        {
        }

        public BattleHudInputUiFactory(BattleHudInputControlFactory controls)
        {
            _controls = controls ?? new BattleHudInputControlFactory();
        }

        public BattleHudInputUi Create(RectTransform root, Canvas canvas, Transform cameraTransform, Action infoClicked)
        {
            var inputUiRoot = new GameObject("BattleHudInput", typeof(RectTransform));
            inputUiRoot.transform.SetParent(root, worldPositionStays: false);
            inputUiRoot.SetActive(false);

            BattleHudRectTransformLayout.StretchToParent(inputUiRoot.GetComponent<RectTransform>());

            var inputView = inputUiRoot.AddComponent<BattleHudInputView>();
            var moveJoystick = _controls.CreateMoveJoystick(inputUiRoot.transform, canvas);
            var moveMapper = inputUiRoot.AddComponent<BattleHudMoveInputMapper>();
            var skillAimMapper = inputUiRoot.AddComponent<BattleHudSkillAimInputMapper>();

            var skill1 = CreateSkillButton(inputUiRoot.transform, root, canvas, BattleHudInputLayout.Skill1);
            var skill2 = CreateSkillButton(inputUiRoot.transform, root, canvas, BattleHudInputLayout.Skill2);
            var skill3 = CreateSkillButton(inputUiRoot.transform, root, canvas, BattleHudInputLayout.Skill3);
            var infoButton = _controls.CreateInfoButton(
                inputUiRoot.transform,
                BattleHudInputLayout.InfoButtonPosition,
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

        private SkillButtonView CreateSkillButton(
            Transform parent,
            RectTransform root,
            Canvas canvas,
            BattleHudSkillButtonLayoutSpec layout)
        {
            return _controls.CreateSkillButton(parent, root, canvas, layout.Slot, layout.Name, layout.AnchoredPos);
        }
    }
}
