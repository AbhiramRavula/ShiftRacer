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

        public event Action<RaceState> OnStateChanged;

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

            // Read inputs safely across Keyboard, Mouse, and Gamepad
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

            bool throttleHeld = spaceHeld || wHeld || mouseLeftHeld || gamepadThrottle;
            bool startAction = spacePressed || wHeld || mouseLeftPressed || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool upshiftAction = shiftPressed || ePressed || mouseRightPressed || gamepadShift;

            // Always update state machine regardless of input availability
            switch (State)
            {
                case RaceState.Ready:
                    if (startAction)
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
                    if (qPressed)
                    {
                        if (gearbox != null) gearbox.HandleDownshift();
                    }

                    if (playerEngine != null)
                    {
                        playerEngine.UpdateEngine(throttleHeld, Time.deltaTime, allowMovement: true);
                    }

                    if (opponent != null)
                    {
                        opponent.UpdateOpponent(Time.deltaTime);
                    }

                    // Check finish lines
                    float targetDist = config != null ? config.raceFinishDistance : 402.336f;
                    float playerDist = playerEngine != null ? playerEngine.DistanceTraveled : 0f;
                    float opponentDist = opponent != null ? opponent.DistanceTraveled : 0f;

                    if (playerDist >= targetDist || opponentDist >= targetDist)
                    {
                        FinishRace(playerDist, opponentDist);
                    }
                    break;

                case RaceState.Result:
                    if (startAction || rPressed)
                    {
                        StartStaging();
                    }
                    break;
            }
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

            PlayerWon = crossedFirst && fiveCleanShifts;

            if (PlayerWon)
            {
                ResultSummary = $"VICTORY! Perfect 5/5 shifts! Time: {ElapsedRaceTime:F2}s";
            }
            else if (!crossedFirst)
            {
                ResultSummary = $"DEFEAT: Opponent crossed first ({opponentDist:F1}m). Time: {ElapsedRaceTime:F2}s";
            }
            else
            {
                ResultSummary = $"UNCLEAN RUN: You crossed first, but missed perfect shifts ({judge.CleanShiftsCount}/5).";
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
