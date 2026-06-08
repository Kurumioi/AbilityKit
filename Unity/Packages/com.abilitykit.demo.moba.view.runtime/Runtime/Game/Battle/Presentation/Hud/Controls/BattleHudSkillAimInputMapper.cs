using System;
using UnityEngine;

namespace AbilityKit.Game.Battle.View
{
    public sealed class BattleHudSkillAimInputMapper : MonoBehaviour
    {
        [SerializeField] private BattleHudInputView _hud;
        [SerializeField] private Transform _cameraTransform;
        private bool _isHooked;

        public event Action<int, Vector2> SkillAimStart;
        public event Action<int, Vector2> SkillAimUpdate;
        public event Action<int, Vector2> SkillAimEnd;

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

            _hud.SkillAimStart += OnAimStart;
            _hud.SkillAimUpdate += OnAimUpdate;
            _hud.SkillAimEnd += OnAimEnd;
            _isHooked = true;
        }

        private void UnhookHud()
        {
            if (!_isHooked) return;
            if (_hud == null)
            {
                _isHooked = false;
                return;
            }

            _hud.SkillAimStart -= OnAimStart;
            _hud.SkillAimUpdate -= OnAimUpdate;
            _hud.SkillAimEnd -= OnAimEnd;
            _isHooked = false;
        }

        private void OnAimStart(int slot, Vector2 dir)
        {
            SkillAimStart?.Invoke(slot, TransformAim(dir));
        }

        private void OnAimUpdate(int slot, Vector2 dir)
        {
            SkillAimUpdate?.Invoke(slot, TransformAim(dir));
        }

        private void OnAimEnd(int slot, Vector2 dir)
        {
            SkillAimEnd?.Invoke(slot, TransformAim(dir));
        }

        private Vector2 TransformAim(Vector2 dir)
        {
            return BattleHudInputProjection.ToCameraPlane(dir, _cameraTransform);
        }
    }
}
