using System;
using AbilityKit.Game.Battle.View.Lib.Joystick;
using UnityEngine;

namespace AbilityKit.Game.Battle.View
{
    public sealed class BattleHudMoveInputMapper : MonoBehaviour
    {
        [SerializeField] private BattleHudInputView _hud;
        [SerializeField] private Transform _cameraTransform;
        private bool _isHooked;

        public event Action<float, float> MoveDxDzChanged;

        public void Initialize(BattleHudInputView hud, Transform cameraTransform)
        {
            UnhookHud();

            _hud = hud;
            _cameraTransform = cameraTransform;

            ResolveCameraTransform();
            if (isActiveAndEnabled)
            {
                HookHud();
            }
        }

        private void Awake()
        {
            ResolveCameraTransform();
        }

        private void OnEnable()
        {
            HookHud();
        }

        private void OnDisable()
        {
            UnhookHud();
        }

        private void ResolveCameraTransform()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void HookHud()
        {
            if (_isHooked) return;
            if (_hud == null) return;

            _hud.MoveChanged += OnMoveChanged;
            _isHooked = true;
        }

        private void UnhookHud()
        {
            if (!_isHooked) return;
            if (_hud != null)
            {
                _hud.MoveChanged -= OnMoveChanged;
            }

            _isHooked = false;
        }

        private void OnMoveChanged(JoystickOutput output)
        {
            var world = BattleHudInputProjection.ToCameraPlane(output.Value, _cameraTransform);
            MoveDxDzChanged?.Invoke(world.x, world.y);
        }
    }
}
