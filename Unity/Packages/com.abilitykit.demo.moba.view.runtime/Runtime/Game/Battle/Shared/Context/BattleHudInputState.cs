namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudInputState
    {
        private float _moveDx;
        private float _moveDz;
        private bool _hasMove;

        private int _skillClickSlot;

        private bool _skillAiming;
        private int _skillAimSlot;
        private float _skillAimDx;
        private float _skillAimDz;

        private bool _skillAimSubmit;
        private int _skillAimSubmitSlot;
        private float _skillAimSubmitDx;
        private float _skillAimSubmitDz;

        public bool TryReadMove(out float dx, out float dz)
        {
            if (_hasMove)
            {
                dx = _moveDx;
                dz = _moveDz;
                return true;
            }

            dx = 0f;
            dz = 0f;
            return false;
        }

        public bool TryConsumeSkillClick(out int slot)
        {
            slot = _skillClickSlot;
            if (slot <= 0) return false;

            _skillClickSlot = 0;
            return true;
        }

        public bool TryConsumeSkillAimSubmit(out int slot, out float dx, out float dz)
        {
            if (_skillAimSubmit && _skillAimSubmitSlot > 0)
            {
                slot = _skillAimSubmitSlot;
                dx = _skillAimSubmitDx;
                dz = _skillAimSubmitDz;

                _skillAimSubmit = false;
                _skillAimSubmitSlot = 0;
                _skillAimSubmitDx = 0f;
                _skillAimSubmitDz = 0f;
                return true;
            }

            slot = 0;
            dx = 0f;
            dz = 0f;
            return false;
        }

        public void BeginMove()
        {
            _hasMove = true;
        }

        public void EndMove()
        {
            _hasMove = false;
            _moveDx = 0f;
            _moveDz = 0f;
        }

        public void SetMove(float dx, float dz)
        {
            _moveDx = dx;
            _moveDz = dz;
        }

        public void SubmitSkillClick(int slot)
        {
            _skillClickSlot = slot;
        }

        public void SetSkillAim(int slot, float dx, float dz, bool aiming)
        {
            _skillAiming = aiming;
            _skillAimSlot = slot;
            _skillAimDx = dx;
            _skillAimDz = dz;
        }

        public void SubmitSkillAim(int slot, float dx, float dz)
        {
            SetSkillAim(slot, dx, dz, aiming: false);
            _skillAimSubmit = true;
            _skillAimSubmitSlot = slot;
            _skillAimSubmitDx = dx;
            _skillAimSubmitDz = dz;
        }

        public bool TryReadSkillAim(out int slot, out float dx, out float dz)
        {
            if (_skillAiming)
            {
                slot = _skillAimSlot;
                dx = _skillAimDx;
                dz = _skillAimDz;
                return true;
            }

            slot = 0;
            dx = 0f;
            dz = 0f;
            return false;
        }

        public void Reset()
        {
            _moveDx = 0f;
            _moveDz = 0f;
            _hasMove = false;
            _skillClickSlot = 0;

            _skillAiming = false;
            _skillAimSlot = 0;
            _skillAimDx = 0f;
            _skillAimDz = 0f;

            _skillAimSubmit = false;
            _skillAimSubmitSlot = 0;
            _skillAimSubmitDx = 0f;
            _skillAimSubmitDz = 0f;
        }
    }
}
