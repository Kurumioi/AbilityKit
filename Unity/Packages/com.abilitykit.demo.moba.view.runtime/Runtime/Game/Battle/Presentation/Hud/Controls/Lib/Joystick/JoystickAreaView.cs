using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AbilityKit.Game.Battle.View.Lib.Joystick
{
    public sealed class JoystickAreaView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _area;
        [SerializeField] private RectTransform _outer;
        [SerializeField] private RectTransform _inner;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private JoystickConfig _config = default;

        private int _pointerId = int.MinValue;
        private Camera _uiCamera;

        private Vector2 _centerLocal;
        private JoystickOutput _output;

        public JoystickConfig Config
        {
            get => _config;
            set => _config = value;
        }

        public JoystickOutput Output => _output;

        public event Action OnBegin;
        public event Action<JoystickOutput> OnValueChanged;
        public event Action OnEnd;

        public void Initialize(RectTransform area, RectTransform outer, RectTransform inner, Canvas canvas, JoystickConfig config)
        {
            _area = area;
            _outer = outer;
            _inner = inner;
            _canvas = canvas;
            _config = config.Radius > 0f ? config : JoystickConfig.Default;

            RefreshReferences();
            if (_outer != null) _outer.gameObject.SetActive(!_config.HideWhenReleased);
        }

        private void Awake()
        {
            if (_config.Radius <= 0f) _config = JoystickConfig.Default;
            RefreshReferences();

            if (_outer != null && _config.HideWhenReleased) _outer.gameObject.SetActive(false);
        }

        private void RefreshReferences()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_area == null)
            {
                _area = transform as RectTransform;
            }

            _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            if (_area == null) return;

            _pointerId = eventData.pointerId;
            _centerLocal = ScreenToLocalInRect(_area, eventData.position);

            if (_outer != null)
            {
                _outer.anchoredPosition = _centerLocal;
                if (_config.HideWhenReleased) _outer.gameObject.SetActive(true);
            }

            if (_inner != null)
            {
                _inner.anchoredPosition = _centerLocal;
            }

            UpdateOutput(eventData.position);
            OnBegin?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            UpdateOutput(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            _pointerId = int.MinValue;
            _output = default;

            if (_inner != null)
            {
                _inner.anchoredPosition = _centerLocal;
            }

            if (_outer != null && _config.HideWhenReleased)
            {
                _outer.gameObject.SetActive(false);
            }

            OnValueChanged?.Invoke(_output);
            OnEnd?.Invoke();
        }

        private void UpdateOutput(Vector2 screenPos)
        {
            if (_area == null) return;

            var local = ScreenToLocalInRect(_area, screenPos);
            _output = JoystickInputCalculator.Calculate(_centerLocal, local, _config, out var clamped);

            if (_inner != null)
            {
                _inner.anchoredPosition = _centerLocal + clamped;
            }

            OnValueChanged?.Invoke(_output);
        }

        private Vector2 ScreenToLocalInRect(RectTransform rect, Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, _uiCamera, out var local);
            return local;
        }
    }
}
