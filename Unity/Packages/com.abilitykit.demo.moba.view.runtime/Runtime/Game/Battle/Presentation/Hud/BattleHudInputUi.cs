using AbilityKit.Game.Battle.View;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputUi
    {
        public BattleHudInputUi(
            GameObject root,
            BattleHudInputView inputView,
            JoystickAreaView moveJoystick,
            BattleHudMoveInputMapper moveMapper,
            BattleHudSkillAimInputMapper skillAimMapper,
            SkillButtonView skill1View,
            SkillButtonView skill2View,
            SkillButtonView skill3View,
            Button infoButton)
        {
            Root = root;
            InputView = inputView;
            MoveJoystick = moveJoystick;
            MoveMapper = moveMapper;
            SkillAimMapper = skillAimMapper;
            Skill1View = skill1View;
            Skill2View = skill2View;
            Skill3View = skill3View;
            InfoButton = infoButton;
        }

        public GameObject Root { get; }
        public BattleHudInputView InputView { get; }
        public JoystickAreaView MoveJoystick { get; }
        public BattleHudMoveInputMapper MoveMapper { get; }
        public BattleHudSkillAimInputMapper SkillAimMapper { get; }
        public SkillButtonView Skill1View { get; }
        public SkillButtonView Skill2View { get; }
        public SkillButtonView Skill3View { get; }
        public Button InfoButton { get; }

        public void Destroy()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
            }
        }
    }
}
