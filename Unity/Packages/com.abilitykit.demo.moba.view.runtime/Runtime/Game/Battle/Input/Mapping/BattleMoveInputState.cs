using System;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleMoveInputState
    {
        private float _lastDx;
        private float _lastDz;
        private int _stopRepeatTicks;

        public bool TryGetMoveToSubmit(float dx, float dz, out float submitDx, out float submitDz)
        {
            var wasMoving = Math.Abs(_lastDx) > 0.0001f || Math.Abs(_lastDz) > 0.0001f;
            var isMoving = Math.Abs(dx) > 0.0001f || Math.Abs(dz) > 0.0001f;

            if (isMoving || (wasMoving && !isMoving))
            {
                if (!isMoving && wasMoving)
                {
                    _stopRepeatTicks = 2;
                }

                _lastDx = dx;
                _lastDz = dz;
                submitDx = dx;
                submitDz = dz;
                return true;
            }

            _lastDx = dx;
            _lastDz = dz;

            if (_stopRepeatTicks > 0)
            {
                _stopRepeatTicks--;
                submitDx = 0f;
                submitDz = 0f;
                return true;
            }

            submitDx = 0f;
            submitDz = 0f;
            return false;
        }
    }
}
