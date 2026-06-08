using AbilityKit.Core.Common.Record.Lockstep;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleContext
    {
        private readonly BattleHudInputState _hudInput = new BattleHudInputState();

        public ILockstepInputRecordWriter InputRecordWriter;
        public BattleLocalInputQueue LocalInputQueue;

        internal bool TryReadHudMove(out float dx, out float dz)
        {
            return _hudInput.TryReadMove(out dx, out dz);
        }

        internal bool TryConsumeHudSkillClick(out int slot)
        {
            return _hudInput.TryConsumeSkillClick(out slot);
        }

        internal bool TryConsumeHudSkillAimSubmit(out int slot, out float dx, out float dz)
        {
            return _hudInput.TryConsumeSkillAimSubmit(out slot, out dx, out dz);
        }

        public void BeginHudMove()
        {
            _hudInput.BeginMove();
        }

        public void EndHudMove()
        {
            _hudInput.EndMove();
        }

        public void SetHudMove(float dx, float dz)
        {
            _hudInput.SetMove(dx, dz);
        }

        public void SubmitHudSkillClick(int slot)
        {
            _hudInput.SubmitSkillClick(slot);
        }

        public void SetHudSkillAim(int slot, float dx, float dz, bool aiming)
        {
            _hudInput.SetSkillAim(slot, dx, dz, aiming);
        }

        public void SubmitHudSkillAim(int slot, float dx, float dz)
        {
            _hudInput.SubmitSkillAim(slot, dx, dz);
        }

        internal bool TryReadHudSkillAim(out int slot, out float dx, out float dz)
        {
            return _hudInput.TryReadSkillAim(out slot, out dx, out dz);
        }

        private void ResetHudInput()
        {
            _hudInput.Reset();
        }
    }
}
