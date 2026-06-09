using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputEventDispatcher
    {
        private readonly IBattleHudInputSink _hudInput;

        public BattleHudInputEventDispatcher(IBattleHudInputSink hudInput)
        {
            _hudInput = hudInput;
        }

        public void ResetHudAim()
        {
            _hudInput?.SetHudSkillAim(0, 0f, 0f, aiming: false);
        }

        public void OnMoveBegin()
        {
            _hudInput?.BeginHudMove();
        }

        public void OnMoveEnd()
        {
            _hudInput?.EndHudMove();
        }

        public void OnMoveDxDzChanged(float dx, float dz)
        {
            _hudInput?.SetHudMove(dx, dz);
        }

        public void OnSkillClick(int slot)
        {
            _hudInput?.SubmitHudSkillClick(slot);
        }

        public void OnSkillAimStart(int slot, Vector2 aim)
        {
            _hudInput?.SetHudSkillAim(slot, aim.x, aim.y, aiming: true);
        }

        public void OnSkillAimUpdate(int slot, Vector2 aim)
        {
            _hudInput?.SetHudSkillAim(slot, aim.x, aim.y, aiming: true);
        }

        public void OnSkillAimEnd(int slot, Vector2 aim)
        {
            _hudInput?.SubmitHudSkillAim(slot, aim.x, aim.y);
        }
    }
}
