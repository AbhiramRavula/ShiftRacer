using UnityEngine;
using UnityEngine.InputSystem;

namespace Redline.UI
{
    /// <summary>
    /// Central manager for mobile touchscreen inputs, on-screen buttons, and multi-touch gestures.
    /// Provides unified polling methods consumed by RaceSessionController.
    /// </summary>
    public class MobileTouchController : MonoBehaviour
    {
        public static MobileTouchController Instance { get; private set; }

        [Header("Touch Buttons")]
        [SerializeField] private OnScreenTouchButton throttleButton;
        [SerializeField] private OnScreenTouchButton upshiftButton;
        [SerializeField] private OnScreenTouchButton downshiftButton;

        [Header("Screen Touch Zones")]
        [Tooltip("When true, holding anywhere on the right half of the screen will throttle; tapping right side can upshift")]
        [SerializeField] private bool enableScreenTouchZones = true;

        public bool IsThrottleHeld { get; private set; }
        private bool _upshiftTriggered;
        private bool _downshiftTriggered;
        private bool _startTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find buttons if unassigned
            if (throttleButton == null)
            {
                var buttons = GetComponentsInChildren<OnScreenTouchButton>(true);
                foreach (var btn in buttons)
                {
                    if (btn.ButtonType == TouchButtonType.Throttle) throttleButton = btn;
                    else if (btn.ButtonType == TouchButtonType.Upshift) upshiftButton = btn;
                    else if (btn.ButtonType == TouchButtonType.Downshift) downshiftButton = btn;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void NotifyButtonDown(TouchButtonType type)
        {
            if (type == TouchButtonType.Throttle)
            {
                IsThrottleHeld = true;
            }
            else if (type == TouchButtonType.Upshift)
            {
                _upshiftTriggered = true;
            }
            else if (type == TouchButtonType.Downshift)
            {
                _downshiftTriggered = true;
            }
            else if (type == TouchButtonType.AnyScreenTap)
            {
                _startTriggered = true;
            }
        }

        public void NotifyButtonUp(TouchButtonType type)
        {
            if (type == TouchButtonType.Throttle)
            {
                IsThrottleHeld = false;
            }
        }

        public void NotifyButtonClick(TouchButtonType type)
        {
            // Only AnyScreenTap triggers on click; Upshift and Downshift are handled on PointerDown
            if (type == TouchButtonType.AnyScreenTap)
            {
                _startTriggered = true;
            }
        }

        private void Update()
        {
            // If the on-screen throttle button is physically held down, keep throttle active
            if (throttleButton != null && throttleButton.IsPressed)
            {
                IsThrottleHeld = true;
            }

            // Direct Touchscreen Support via New Input System
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;

                if (enableScreenTouchZones)
                {
                    bool anyTouchOnThrottleZone = false;
                    foreach (var touch in touchscreen.touches)
                    {
                        if (touch.press.isPressed)
                        {
                            int touchId = touch.touchId.ReadValue();
                            bool isOverUI = eventSystem != null && (eventSystem.IsPointerOverGameObject(touchId) || eventSystem.IsPointerOverGameObject());

                            Vector2 pos = touch.position.ReadValue();

                            // Right half of screen serves as throttle zone (unless touching other specific UI)
                            if (pos.x > Screen.width * 0.45f && (!isOverUI || (throttleButton != null && throttleButton.IsPressed)))
                            {
                                anyTouchOnThrottleZone = true;
                            }

                            // Tap in upper right quadrant triggers an upshift only if NOT tapping a UI element
                            if (touch.press.wasPressedThisFrame && !isOverUI && pos.x > Screen.width * 0.55f && pos.y > Screen.height * 0.40f)
                            {
                                _upshiftTriggered = true;
                            }
                        }
                    }

                    if (anyTouchOnThrottleZone)
                    {
                        IsThrottleHeld = true;
                    }
                    else if (throttleButton == null || !throttleButton.IsPressed)
                    {
                        IsThrottleHeld = false;
                    }
                }
            }
        }

        public bool ConsumeUpshift()
        {
            bool val = _upshiftTriggered;
            _upshiftTriggered = false;
            return val;
        }

        public bool ConsumeDownshift()
        {
            bool val = _downshiftTriggered;
            _downshiftTriggered = false;
            return val;
        }

        public bool ConsumeStart()
        {
            bool val = _startTriggered;
            _startTriggered = false;
            return val;
        }
    }
}
