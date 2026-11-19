using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Redline.Config;

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

        private void Awake()
        {
            if (config != null)
            {
                if (playerEngine != null) playerEngine.SetConfig(config);
                if (judge != null) judge.SetConfig(config);
                if (opponent != null) opponent.SetConfig(config);
            }
        }

        private void Start()
        {
            TransitionTo(RaceState.Ready);
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

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            switch (State)
            {
                case RaceState.Ready:
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                    {
                        StartStaging();
                    }
                    break;

                case RaceState.Staging:
                    StagingTimer -= Time.deltaTime;
                    if (StagingTimer <= 0f)
                    {
                        if (opponent != null) opponent.StartRacing();
                        TransitionTo(RaceState.Racing);
                    }
                    break;

                case RaceState.Racing:
                    ElapsedRaceTime += Time.deltaTime;

                    // Continuous throttle check
                    bool throttle = keyboard.spaceKey.isPressed ||
                                    keyboard.wKey.isPressed ||
                                    keyboard.upArrowKey.isPressed;

                    // Edge-triggered upshift check (Shift or E)
                    if (keyboard.leftShiftKey.wasPressedThisFrame ||
                        keyboard.rightShiftKey.wasPressedThisFrame ||
                        keyboard.eKey.wasPressedThisFrame)
                    {
                        if (gearbox != null) gearbox.HandleUpshift();
                    }

                    // Optional downshift (Q)
                    if (keyboard.qKey.wasPressedThisFrame)
                    {
                        if (gearbox != null) gearbox.HandleDownshift();
                    }

                    if (playerEngine != null) playerEngine.UpdateEngine(throttle, Time.deltaTime);
                    if (opponent != null) opponent.UpdateOpponent(Time.deltaTime);

                    // Check finish line crossing (402.336m)
                    float targetDist = config != null ? config.quarterMileDistance : 402.336f;
                    float playerDist = playerEngine != null ? playerEngine.DistanceTraveled : 0f;
                    float opponentDist = opponent != null ? opponent.DistanceTraveled : 0f;

                    if (playerDist >= targetDist || opponentDist >= targetDist)
                    {
                        FinishRace(playerDist, opponentDist);
                    }
                    break;

                case RaceState.Result:
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)
                    {
                        StartStaging();
                    }
                    break;
            }
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
                ResultSummary = $"DEFEAT: Opponent crossed first. Time: {ElapsedRaceTime:F2}s";
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
            OnStateChanged?.Invoke(newState);
        }
    }
}
