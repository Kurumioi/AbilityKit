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
        private readonly SkillButtonGestureController _gesture = new SkillButtonGestureController();
        private Camera _uiCamera;

        public SkillButtonConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                NormalizeConfig(ref _config);
                _gesture.Configure(_config);
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
            _config = config;
            NormalizeConfig(ref _config);
            _gesture.Configure(_config);

            RefreshReferences();
            _aim.Hide();
        }

        private void Awake()
        {
            NormalizeConfig(ref _config);
            _gesture.Configure(_config);
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

        private static void NormalizeConfig(ref SkillButtonConfig config)
        {
            var defaults = SkillButtonConfig.Default;
            if (config.LongPressSeconds < 0f) config.LongPressSeconds = defaults.LongPressSeconds;
            if (config.DragThreshold <= 0f) config.DragThreshold = defaults.DragThreshold;
            if (config.AimMaxRadius <= 0f) config.AimMaxRadius = defaults.AimMaxRadius;
            if (config.IndicatorLengthPixels <= 0f) config.IndicatorLengthPixels = defaults.IndicatorLengthPixels;
            if (config.IndicatorWidthPixels <= 0f) config.IndicatorWidthPixels = defaults.IndicatorWidthPixels;
        }

        private void OnDisable()
        {
            _gesture.Reset();
            _aim.Hide();
        }

        private void Update()
        {
            if (!_gesture.Tick(Time.unscaledTime)) return;
            OnLongPress?.Invoke();

            if (_config.EnableAim)
            {
                BeginAim(_gesture.LastScreenPos);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_buttonRect == null) return;

            _gesture.Begin(eventData.pointerId, eventData.position, Time.unscaledTime);
            if (ShouldPreviewImmediately())
            {
                BeginAim(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_gesture.Drag(eventData.pointerId, eventData.position, out var shouldBeginAim, out var shouldUpdateAim)) return;
            if (shouldBeginAim)
            {
                BeginAim(_gesture.LastScreenPos);
                shouldUpdateAim = true;
            }

            if (shouldUpdateAim)
            {
                var aim = _aim.Calculate(_gesture.LastScreenPos);
                OnAimUpdate?.Invoke(aim);
                _aim.Update(_gesture.LastScreenPos);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_gesture.End(eventData.pointerId, out var wasAiming, out var longPressFired)) return;

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

        private void BeginAim(Vector2 currentScreen)
        {
            _gesture.SetAiming(true);
            var aim = _aim.Calculate(currentScreen);
            OnAimStart?.Invoke(aim);
            _aim.Show(currentScreen);
        }

        private bool ShouldPreviewImmediately()
        {
            return _config.EnableAim && _config.IndicatorShape == SkillAimIndicatorShape.SelfCircle;
        }

        private void EndAim()
        {
            _gesture.SetAiming(false);
            _aim.Hide();
        }
    }
}
