using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Redline.Config;
using Redline.Core;

namespace Redline.UI
{
    public class RaceHUDController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private RaceConfig config;
        [SerializeField] private RaceSessionController session;
        [SerializeField] private BikeEngineModel engine;
        [SerializeField] private ShiftJudge judge;
        [SerializeField] private OpponentController opponent;

        [Header("RPM Meter & Speed Readouts")]
        [SerializeField] private Slider rpmSlider;
        [SerializeField] private TextMeshProUGUI rpmText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI gearText;

        [Header("Shift Indicators & Feedback")]
        [SerializeField] private TextMeshProUGUI shiftGradeCallout;
        [SerializeField] private Image[] shiftIndicators; // 5 dots/bars
        [SerializeField] private GameObject limiterWarningCue;

        [Header("Race Progress & Distances")]
        [SerializeField] private Slider raceProgressSlider;
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Overlays & Screens")]
        [SerializeField] private GameObject readyOverlay;
        [SerializeField] private GameObject stagingOverlay;
        [SerializeField] private TextMeshProUGUI stagingCountdownText;
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultDetailsText;

        private Coroutine _calloutCoroutine;

        private void Awake()
        {
            if (judge != null)
            {
                judge.OnShiftGraded += HandleShiftGraded;
            }

            if (session != null)
            {
                session.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (judge != null) judge.OnShiftGraded -= HandleShiftGraded;
            if (session != null) session.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            if (config != null && rpmSlider != null)
            {
                rpmSlider.minValue = config.idleRpm;
                rpmSlider.maxValue = config.redlineRpm;
            }

            ResetShiftDots();
            UpdateOverlays(RaceState.Ready);
        }

        private void Update()
        {
            if (engine == null || config == null) return;

            // Update RPM meter & text
            if (rpmSlider != null) rpmSlider.value = engine.CurrentRpm;
            if (rpmText != null) rpmText.text = $"{Mathf.RoundToInt(engine.CurrentRpm)} RPM";

            // Update Speed & Gear
            if (speedText != null) speedText.text = $"{Mathf.RoundToInt(engine.CurrentSpeedKmh)} <size=60%>KM/H</size>";
            if (gearText != null) gearText.text = $"{engine.CurrentGear}";

            // Limiter cue
            if (limiterWarningCue != null)
            {
                limiterWarningCue.SetActive(engine.IsOnLimiter);
            }

            // Update Distance & Track Progress
            if (distanceText != null)
            {
                distanceText.text = $"{engine.DistanceTraveled:F1} m / {config.quarterMileDistance:F1} m";
            }

            if (raceProgressSlider != null)
            {
                raceProgressSlider.value = Mathf.Clamp01(engine.DistanceTraveled / config.quarterMileDistance);
            }

            // Update Staging Countdown if active
            if (session != null && session.State == RaceState.Staging && stagingCountdownText != null)
            {
                stagingCountdownText.text = Mathf.CeilToInt(session.StagingTimer).ToString();
            }
        }

        private void HandleShiftGraded(ShiftGrade grade, int fromGear)
        {
            int index = fromGear - 1;
            if (index >= 0 && index < shiftIndicators.Length && shiftIndicators[index] != null)
            {
                shiftIndicators[index].color = (grade == ShiftGrade.Perfect) ? Color.green : Color.red;
            }

            if (shiftGradeCallout != null)
            {
                if (_calloutCoroutine != null) StopCoroutine(_calloutCoroutine);
                _calloutCoroutine = StartCoroutine(ShowCalloutRoutine(grade));
            }
        }

        private IEnumerator ShowCalloutRoutine(ShiftGrade grade)
        {
            shiftGradeCallout.gameObject.SetActive(true);
            switch (grade)
            {
                case ShiftGrade.Perfect:
                    shiftGradeCallout.text = "<color=#00FF66>PERFECT!</color>";
                    break;
                case ShiftGrade.Early:
                    shiftGradeCallout.text = "<color=#FFB300>EARLY</color>";
                    break;
                case ShiftGrade.Late:
                    shiftGradeCallout.text = "<color=#FF3333>LATE</color>";
                    break;
            }

            yield return new WaitForSeconds(0.75f);
            shiftGradeCallout.gameObject.SetActive(false);
        }

        private void HandleStateChanged(RaceState newState)
        {
            UpdateOverlays(newState);

            if (newState == RaceState.Staging)
            {
                ResetShiftDots();
                if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
            }
            else if (newState == RaceState.Result)
            {
                if (resultTitleText != null)
                {
                    resultTitleText.text = session.PlayerWon ? "<color=#00FF66>VICTORY!</color>" : "<color=#FF4444>DEFEAT</color>";
                }
                if (resultDetailsText != null)
                {
                    resultDetailsText.text = session.ResultSummary;
                }
            }
        }

        private void ResetShiftDots()
        {
            if (shiftIndicators == null) return;
            foreach (var dot in shiftIndicators)
            {
                if (dot != null) dot.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            }
        }

        private void UpdateOverlays(RaceState state)
        {
            if (readyOverlay != null) readyOverlay.SetActive(state == RaceState.Ready);
            if (stagingOverlay != null) stagingOverlay.SetActive(state == RaceState.Staging);
            if (resultOverlay != null) resultOverlay.SetActive(state == RaceState.Result);
        }
    }
}
