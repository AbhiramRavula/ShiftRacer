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

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (_originalScale == Vector3.zero) _originalScale = Vector3.one;
        }

        private void OnDisable()
        {
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
