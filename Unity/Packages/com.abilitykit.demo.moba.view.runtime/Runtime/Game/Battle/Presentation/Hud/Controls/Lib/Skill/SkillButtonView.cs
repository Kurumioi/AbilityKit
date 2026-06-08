using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AbilityKit.Game.Battle.View.Lib.Skill
{
    public sealed class SkillButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private RectTransform _uiRootRect;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private SkillAimIndicatorView _aimIndicator;
        [SerializeField] private SkillButtonConfig _config = default;

        private readonly SkillButtonAimController _aim = new SkillButtonAimController();
        private Camera _uiCamera;
        private SkillButtonPointerState _pointer = SkillButtonPointerState.Create();

        public SkillButtonConfig Config
        {
            get => _config;
            set
            {
                _config = value.LongPressSeconds > 0f ? value : SkillButtonConfig.Default;
                ConfigureAim();
            }
        }

        public event Action OnClick;
        public event Action OnLongPress;
        public event Action<Vector2> OnAimStart;
        public event Action<Vector2> OnAimUpdate;
        public event Action<Vector2> OnAimEnd;

        public void Initialize(
            RectTransform buttonRect,
            RectTransform uiRootRect,
            Canvas canvas,
            SkillButtonConfig config,
            SkillAimIndicatorView aimIndicator = null)
        {
            _buttonRect = buttonRect;
            _uiRootRect = uiRootRect;
            _canvas = canvas;
            _aimIndicator = aimIndicator;
            _config = config.LongPressSeconds > 0f ? config : SkillButtonConfig.Default;

            RefreshReferences();
            _aim.Hide();
        }

        private void Awake()
        {
            if (_config.LongPressSeconds <= 0f) _config = SkillButtonConfig.Default;
            RefreshReferences();

            _aim.Hide();
        }

        private void RefreshReferences()
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_buttonRect == null) _buttonRect = transform as RectTransform;
            if (_uiRootRect == null) _uiRootRect = _buttonRect != null ? _buttonRect.root as RectTransform : null;
            _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            ConfigureAim();
        }

        private void ConfigureAim()
        {
            _aim.Configure(_buttonRect, _uiRootRect, _uiCamera, _aimIndicator, _config);
        }

        private void OnDisable()
        {
            _pointer = SkillButtonPointerState.Create();
            _aim.Hide();
        }

        private void Update()
        {
            if (!_pointer.Pressed) return;
            if (_pointer.LongPressFired) return;

            var now = Time.unscaledTime;
            if (now - _pointer.PressTime < _config.LongPressSeconds) return;

            _pointer.MarkLongPressFired();
            OnLongPress?.Invoke();

            if (_config.EnableAim)
            {
                BeginAim(_pointer.LastScreenPos);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_buttonRect == null) return;

            _pointer.Begin(eventData.pointerId, eventData.position, Time.unscaledTime);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointer.Pressed) return;
            if (!_pointer.Matches(eventData.pointerId)) return;

            _pointer.UpdateScreenPos(eventData.position);

            if (_config.EnableAim && !_pointer.Aiming && ShouldBeginAim())
            {
                BeginAim(_pointer.LastScreenPos);
            }

            if (_pointer.Aiming)
            {
                var aim = _aim.Calculate(_pointer.LastScreenPos);
                OnAimUpdate?.Invoke(aim);
                _aim.Update(_pointer.LastScreenPos);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointer.Matches(eventData.pointerId)) return;

            var wasAiming = _pointer.Aiming;
            var longPressFired = _pointer.LongPressFired;
            _pointer.EndPress();

            if (wasAiming)
            {
                var aim = _aim.Calculate(eventData.position);
                OnAimEnd?.Invoke(aim);
                EndAim();
                return;
            }

            if (!longPressFired)
            {
                OnClick?.Invoke();
            }

            EndAim();
        }

        private bool ShouldBeginAim()
        {
            var drag = (_pointer.LastScreenPos - _pointer.PressScreenPos).magnitude;
            return drag >= _config.DragThreshold;
        }

        private void BeginAim(Vector2 currentScreen)
        {
            _pointer.SetAiming(true);
            var aim = _aim.Calculate(currentScreen);
            OnAimStart?.Invoke(aim);
            _aim.Show(currentScreen);
        }

        private void EndAim()
        {
            _pointer.SetAiming(false);
            _aim.Hide();
        }
    }
}
