using UnityEngine;
using UnityEngine.EventSystems;

namespace Redline.UI
{
    public enum TouchButtonType
    {
        Throttle,
        Upshift,
        Downshift,
        AnyScreenTap
    }

    /// <summary>
    /// Attach to on-screen UI buttons for high-responsiveness touch, mouse, and stylus input.
    /// Provides visual feedback (scale down on press) and notifies MobileTouchController.
    /// </summary>
    public class OnScreenTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private TouchButtonType buttonType;
        [SerializeField] private float pressScale = 0.92f;

        private Vector3 _originalScale = Vector3.one;
        private bool _isPressed;

        public bool IsPressed => _isPressed;
        public TouchButtonType ButtonType => buttonType;

        public void SetButtonType(TouchButtonType type) => buttonType = type;

        private Coroutine _popCoroutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (_originalScale == Vector3.zero) _originalScale = Vector3.one;
        }

        private void Start()
        {
            // Initial attention-grabbing pop for the Shift button
            if (buttonType == TouchButtonType.Upshift)
            {
                PlayPopAnimation(1.24f, 0.45f);
            }
        }

        public void PlayPopAnimation(float punchScale = 1.22f, float duration = 0.38f)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(PopRoutine(punchScale, duration));
        }

        private System.Collections.IEnumerator PopRoutine(float punchScale, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = _originalScale;
            Vector3 peakScale = _originalScale * punchScale;

            float halfDuration = duration * 0.45f;
            while (elapsed < halfDuration)
            {
                if (!_isPressed)
                {
                    float t = elapsed / halfDuration;
                    float s = Mathf.Sin(t * Mathf.PI * 0.5f);
                    transform.localScale = Vector3.Lerp(startScale, peakScale, s);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            float settleDuration = duration - halfDuration;
            while (elapsed < settleDuration)
            {
                if (!_isPressed)
                {
                    float t = elapsed / settleDuration;
                    float bounce = Mathf.Cos(t * Mathf.PI * 1.5f) * (1f - t) * (punchScale - 1f) * 0.5f;
                    transform.localScale = _originalScale * (1f + bounce);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_isPressed)
            {
                transform.localScale = _originalScale;
            }
            _popCoroutine = null;
        }

        private void OnDisable()
        {
            if (_popCoroutine != null)
            {
                StopCoroutine(_popCoroutine);
                _popCoroutine = null;
            }
            _isPressed = false;
            transform.localScale = _originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            transform.localScale = _originalScale * pressScale;
            MobileTouchController.Instance?.NotifyButtonDown(buttonType);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            transform.localScale = _originalScale;
            MobileTouchController.Instance?.NotifyButtonUp(buttonType);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only AnyScreenTap (e.g. stage button / replay) uses PointerClick.
            // Throttle, Upshift, and Downshift execute on instantaneous PointerDown to avoid double-triggers.
            if (buttonType == TouchButtonType.AnyScreenTap)
            {
                MobileTouchController.Instance?.NotifyButtonClick(buttonType);
            }
        }
    }
}
