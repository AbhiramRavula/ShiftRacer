using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Redline.Config;
using Redline.Audio;

namespace Redline.Core
{
    public enum RaceState
    {
        Ready,
        Staging,
        Racing,
        Result
    }

    public class RaceSessionController : MonoBehaviour
    {
        [Header("Configuration & References")]
        [SerializeField] private RaceConfig config;
        [SerializeField] private BikeEngineModel playerEngine;
        [SerializeField] private SequentialGearbox gearbox;
        [SerializeField] private ShiftJudge judge;
        [SerializeField] private OpponentController opponent;

        public RaceState State { get; private set; } = RaceState.Ready;
        public float ElapsedRaceTime { get; private set; }
        public float StagingTimer { get; private set; }
        public bool PlayerWon { get; private set; }
        public string ResultSummary { get; private set; }
        public RaceDistanceMode DistanceMode { get; private set; } = RaceDistanceMode.QuarterMile;
        public float TargetRaceDistance => config != null ? config.GetDistanceForMode(DistanceMode) : 402.336f;

        public event Action<RaceState> OnStateChanged;
        public event Action<RaceDistanceMode> OnDistanceModeChanged;

        private const float StagingDuration = 3.0f;
        private bool _initialized;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
                if (config != null) UnityEditor.EditorUtility.SetDirty(this);
            }
            if (playerEngine == null) playerEngine = GetComponent<BikeEngineModel>();
            if (gearbox == null) gearbox = GetComponent<SequentialGearbox>();
            if (judge == null) judge = GetComponent<ShiftJudge>();
            if (opponent == null) opponent = GetComponent<OpponentController>();
        }
#endif

        private void Awake()
        {
            // Auto-detect config asset if unassigned in Inspector
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
            }

            // Auto-fallback for components if unassigned in Inspector
            if (playerEngine == null) playerEngine = GetComponent<BikeEngineModel>();
            if (gearbox == null) gearbox = GetComponent<SequentialGearbox>();
            if (judge == null) judge = GetComponent<ShiftJudge>();
            if (opponent == null) opponent = GetComponent<OpponentController>();

            if (config != null)
            {
                if (playerEngine != null) playerEngine.SetConfig(config);
                if (gearbox != null) gearbox.SetConfig(config);
                if (judge != null) judge.SetConfig(config);
                if (opponent != null) opponent.SetConfig(config);
                var audioCtrl = GetComponent<BikeAudioController>();
                if (audioCtrl != null) audioCtrl.SetConfig(config);
            }
        }

        private void Start()
        {
            if (playerEngine != null) playerEngine.Initialize();
            _initialized = false;
        }

        private void Update()
        {
            // Defer initial state to first frame
            if (!_initialized)
            {
                _initialized = true;
                TransitionTo(RaceState.Ready);
                return;
            }

            // Read inputs safely across Keyboard, Mouse, Gamepad, Touchscreen, and Mobile UI
            bool spacePressed = false;
            bool spaceHeld = false;
            bool wHeld = false;
            bool shiftPressed = false;
            bool ePressed = false;
            bool qPressed = false;
            bool rPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                spacePressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
                spaceHeld = keyboard.spaceKey.isPressed || keyboard.upArrowKey.isPressed;
                wHeld = keyboard.wKey.isPressed;
                shiftPressed = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
                ePressed = keyboard.eKey.wasPressedThisFrame;
                qPressed = keyboard.qKey.wasPressedThisFrame;
                rPressed = keyboard.rKey.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            bool mouseLeftHeld = mouse != null && mouse.leftButton.isPressed;
            bool mouseLeftPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool mouseRightPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;

            var gamepad = Gamepad.current;
            bool gamepadThrottle = gamepad != null && (gamepad.rightTrigger.isPressed || gamepad.buttonSouth.isPressed);
            bool gamepadShift = gamepad != null && (gamepad.rightShoulder.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame);
            bool gamepadStart = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            bool gamepadCycleDist = gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame);

            // Direct Touchscreen Support via New Input System
            var touchscreen = Touchscreen.current;
            bool touchPressed = false;
            if (touchscreen != null)
            {
                touchPressed = touchscreen.primaryTouch.press.wasPressedThisFrame;
            }

            // On-screen Mobile Touch Controller
            bool mobileThrottle = false;
            bool mobileUpshift = false;
            bool mobileDownshift = false;
            bool mobileStart = false;
            if (Redline.UI.MobileTouchController.Instance != null)
            {
                mobileThrottle = Redline.UI.MobileTouchController.Instance.IsThrottleHeld;
                mobileUpshift = Redline.UI.MobileTouchController.Instance.ConsumeUpshift();
                mobileDownshift = Redline.UI.MobileTouchController.Instance.ConsumeDownshift();
                mobileStart = Redline.UI.MobileTouchController.Instance.ConsumeStart();
            }

            bool tabPressed = keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
            bool cycleDistanceAction = tabPressed || gamepadCycleDist;

            // Check if the pointer/finger is currently interacting with UI (e.g. Distance Tabs)
            bool isPointerOverUI = false;
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                isPointerOverUI = eventSystem.IsPointerOverGameObject();
                if (!isPointerOverUI && touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
                {
                    isPointerOverUI = eventSystem.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
                }
            }

            // Screen taps only count as race staging/starting if NOT tapping on a UI button (like distance selector)
            bool screenTapStart = (mouseLeftPressed || touchPressed) && !isPointerOverUI;

            bool throttleHeld = spaceHeld || wHeld || mouseLeftHeld || gamepadThrottle || mobileThrottle;
            bool startAction = spacePressed || shiftPressed || wHeld || gamepadStart || mobileStart || screenTapStart;
            bool upshiftAction = shiftPressed || ePressed || mouseRightPressed || gamepadShift || mobileUpshift;
            bool downshiftAction = qPressed || mobileDownshift;

            // Always update state machine regardless of input availability
            switch (State)
            {
                case RaceState.Ready:
                    if (cycleDistanceAction)
                    {
                        CycleDistanceMode();
                    }
                    else if (startAction)
                    {
                        StartStaging();
                    }
                    break;

                case RaceState.Staging:
                    StagingTimer -= Time.deltaTime;

                    // Allow revving the engine on the staging line (clutch held, bike stationary)
                    if (playerEngine != null)
                    {
                        playerEngine.UpdateEngine(throttleHeld, Time.deltaTime, allowMovement: false);
                    }

                    if (StagingTimer <= 0f)
                    {
                        if (opponent != null) opponent.StartRacing();
                        TransitionTo(RaceState.Racing);
                    }
                    break;

                case RaceState.Racing:
                    ElapsedRaceTime += Time.deltaTime;

                    // Edge-triggered upshift
                    if (upshiftAction)
                    {
                        if (gearbox != null) gearbox.HandleUpshift();
                    }

                    // Recovery downshift
                    if (downshiftAction)
                    {
                        if (gearbox != null) gearbox.HandleDownshift();
                    }

                    // Auto-throttle: engine is pinned wide open during race (pure one-button focus on the SHIFT key!)
                    bool isAutoThrottle = config != null ? config.autoThrottleDuringRace : true;
                    bool racingThrottle = isAutoThrottle || throttleHeld;

                    if (playerEngine != null)
                    {
                        playerEngine.UpdateEngine(racingThrottle, Time.deltaTime, allowMovement: true);
                    }

                    if (opponent != null)
                    {
                        opponent.UpdateOpponent(Time.deltaTime, TargetRaceDistance);
                    }

                    // Check finish lines using the selected race distance (1/4, 1/2, or Full Mile)
                    float targetDist = TargetRaceDistance;
                    float playerDist = playerEngine != null ? playerEngine.DistanceTraveled : 0f;
                    float opponentDist = opponent != null ? opponent.DistanceTraveled : 0f;

                    if (playerDist >= targetDist || opponentDist >= targetDist)
                    {
                        FinishRace(playerDist, opponentDist);
                    }
                    break;

                case RaceState.Result:
                    bool resultScreenTap = mouseLeftPressed || touchPressed || mobileStart;
                    if (cycleDistanceAction)
                    {
                        CycleDistanceMode();
                    }
                    else if (startAction || rPressed || resultScreenTap)
                    {
                        ResetToReady();
                    }
                    break;
            }
        }

        public void ResetToReady()
        {
            ElapsedRaceTime = 0f;
            PlayerWon = false;
            ResultSummary = "";

            if (playerEngine != null) playerEngine.Initialize();
            if (judge != null) judge.ResetJudge();
            if (opponent != null) opponent.ResetOpponent();

            TransitionTo(RaceState.Ready);
        }

        public void SetDistanceMode(RaceDistanceMode mode)
        {
            DistanceMode = mode;
            Debug.Log($"<color=#00FFFF><b>[Redline] Race Distance set to: {mode} ({TargetRaceDistance:F1}m)</b></color>");
            OnDistanceModeChanged?.Invoke(mode);
        }

        public void CycleDistanceMode()
        {
            int next = ((int)DistanceMode + 1) % 3;
            SetDistanceMode((RaceDistanceMode)next);
        }

        public void StartStaging()
        {
            if (State != RaceState.Ready && State != RaceState.Result) return;

            StagingTimer = StagingDuration;
            ElapsedRaceTime = 0f;
            PlayerWon = false;
            ResultSummary = "";

            if (playerEngine != null) playerEngine.Initialize();
            if (judge != null) judge.ResetJudge();
            if (opponent != null) opponent.ResetOpponent();

            TransitionTo(RaceState.Staging);
        }

        private void FinishRace(float playerDist, float opponentDist)
        {
            if (opponent != null) opponent.StopRacing();

            bool crossedFirst = playerDist >= opponentDist;
            bool fiveCleanShifts = judge != null && judge.CleanShiftsCount >= 5 && judge.IsRunClean;

            // A win is a win! Crossing the line first is ALWAYS a victory.
            PlayerWon = crossedFirst;

            string distLabel = DistanceMode == RaceDistanceMode.QuarterMile ? "1/4 MILE" :
                               DistanceMode == RaceDistanceMode.HalfMile ? "1/2 MILE" : "1 MILE";

            float playerSpeed = playerEngine != null ? playerEngine.CurrentSpeedKmh : 0f;
            int cleanShifts = judge != null ? judge.CleanShiftsCount : 0;

            if (crossedFirst)
            {
                string timingTag = fiveCleanShifts ? "<color=#00FF66><b>FLAWLESS 100% TIMING</b></color>" : "<color=#00F0FF><b>CLEAN RUN</b></color>";
                ResultSummary = $"<size=88%><color=#9AA0A6>{distLabel} FINISH</color>  •  {timingTag}</size>\n" +
                                $"<color=#9AA0A6>TIME</color>  <color=#00F0FF><b>{ElapsedRaceTime:F2}s</b></color>      <color=#555566>|</color>      " +
                                $"<color=#9AA0A6>SPEED</color>  <color=#FFE600><b>{playerSpeed:F0} KM/H</b></color>\n" +
                                $"<color=#9AA0A6>CLEAN SHIFTS</color>  <color={(fiveCleanShifts ? "#00FF66" : "#FFFFFF")}><b>{cleanShifts}/5</b></color>";
            }
            else
            {
                float rivalLead = Mathf.Max(0.5f, opponentDist - playerDist);
                ResultSummary = $"<size=88%><color=#FF6666>{distLabel} RUN</color>  •  <color=#9AA0A6>RIVAL LEAD +{rivalLead:F1} m</color></size>\n" +
                                $"<color=#9AA0A6>TIME</color>  <color=#00F0FF><b>{ElapsedRaceTime:F2}s</b></color>      <color=#555566>|</color>      " +
                                $"<color=#9AA0A6>SPEED</color>  <color=#FFE600><b>{playerSpeed:F0} KM/H</b></color>\n" +
                                $"<color=#9AA0A6>CLEAN SHIFTS</color>  <color=#FFFFFF><b>{cleanShifts}/5</b></color>";
            }

            TransitionTo(RaceState.Result);
        }

        private void TransitionTo(RaceState newState)
        {
            State = newState;
            Debug.Log($"[Redline] Race state -> {newState}");
            OnStateChanged?.Invoke(newState);
        }
    }
}
