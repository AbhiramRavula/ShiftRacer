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
        [SerializeField] private GameObject limiterWarningCue; // "SHIFT NOW" prompt

        [Header("Optional Sprite Badges (Assign your sliced sprites here)")]
        [SerializeField] private Image badgeDisplayImage; // Optional UI Image for badge
        [SerializeField] private Sprite badgePerfectSprite;
        [SerializeField] private Sprite badgeEarlySprite;
        [SerializeField] private Sprite badgeLateSprite;
        [SerializeField] private Sprite badgeShiftNowSprite;

        [Header("Optional Shift Dot Sprites")]
        [SerializeField] private Sprite dotEmptySprite;
        [SerializeField] private Sprite dotGreenSprite;
        [SerializeField] private Sprite dotRedSprite;

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
        private Coroutine _goFlashCoroutine;
        private int _lastDisplayedCountdown = -1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
                if (config != null) UnityEditor.EditorUtility.SetDirty(this);
            }
            if (session == null) session = FindAnyObjectByType<RaceSessionController>();
            if (engine == null) engine = FindAnyObjectByType<BikeEngineModel>();
            if (judge == null) judge = FindAnyObjectByType<ShiftJudge>();
            if (opponent == null) opponent = FindAnyObjectByType<OpponentController>();

            if (badgePerfectSprite == null) badgePerfectSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Perfect");
            if (badgeEarlySprite == null) badgeEarlySprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Early");
            if (badgeLateSprite == null) badgeLateSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Late");
            if (badgeShiftNowSprite == null) badgeShiftNowSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_ShiftNow");

            if (dotEmptySprite == null) dotEmptySprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States empty");
            if (dotGreenSprite == null) dotGreenSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States green");
            if (dotRedSprite == null) dotRedSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States red");
        }

        private static Sprite LoadSubSprite(string texturePath, string spriteName)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(texturePath);
            if (assets == null) return null;
            foreach (var obj in assets)
            {
                if (obj is Sprite s && s.name == spriteName) return s;
            }
            return null;
        }
#endif

        private void Awake()
        {
            // Auto-recovery if unassigned
            if (session == null) session = FindAnyObjectByType<RaceSessionController>();
            if (engine == null) engine = FindAnyObjectByType<BikeEngineModel>();
            if (judge == null) judge = FindAnyObjectByType<ShiftJudge>();
            if (opponent == null) opponent = FindAnyObjectByType<OpponentController>();

            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
            }

            if (config == null)
            {
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RaceConfig");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    config = UnityEditor.AssetDatabase.LoadAssetAtPath<RaceConfig>(path);
                }
#endif
                if (config == null)
                {
                    var allConfigs = Resources.FindObjectsOfTypeAll<RaceConfig>();
                    if (allConfigs != null && allConfigs.Length > 0) config = allConfigs[0];
                }
            }

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
            ShowReadyScreen();
        }

        private void Update()
        {
            if (engine == null || config == null) return;

            // Live RPM meter updates during both Staging (revving on line) and Racing
            if (rpmSlider != null) rpmSlider.value = engine.CurrentRpm;
            if (rpmText != null) rpmText.text = $"{Mathf.RoundToInt(engine.CurrentRpm)} RPM";

            // Speed & Gear
            if (speedText != null) speedText.text = $"{Mathf.RoundToInt(engine.CurrentSpeedKmh)} <size=50%>KM/H</size>";
            if (gearText != null) gearText.text = $"{engine.CurrentGear}";

            // REAL-TIME SHIFT PROMPT: Lights up when entering green window (11,400–12,600 RPM) or redline!
            bool inGreenWindow = engine.CurrentRpm >= config.perfectWindowMin && engine.CurrentRpm <= config.perfectWindowMax;
            bool atRedline = engine.CurrentRpm > config.perfectWindowMax || engine.IsOnLimiter;
            bool shouldPromptShift = (session != null && session.State == RaceState.Racing) && (inGreenWindow || atRedline);

            if (limiterWarningCue != null)
            {
                limiterWarningCue.SetActive(shouldPromptShift);

                var cueText = limiterWarningCue.GetComponent<TextMeshProUGUI>();
                if (cueText != null)
                {
                    if (inGreenWindow)
                    {
                        cueText.text = ">> SHIFT NOW! <<";
                        cueText.color = new Color(0f, 0.95f, 1f); // Glowing Cyan
                    }
                    else if (atRedline)
                    {
                        cueText.text = "!! REDLINE - SHIFT !!";
                        cueText.color = new Color(1f, 0.2f, 0.2f); // Flashing Red
                    }
                }

                // If user assigned badgeShiftNowSprite to badgeDisplayImage
                if (badgeDisplayImage != null && badgeShiftNowSprite != null)
                {
                    badgeDisplayImage.gameObject.SetActive(shouldPromptShift);
                    badgeDisplayImage.sprite = badgeShiftNowSprite;
                }
            }

            // Distance & Progress Bar
            if (distanceText != null)
            {
                distanceText.text = $"{engine.DistanceTraveled:F1} m / {config.quarterMileDistance:F1} m";
            }

            if (raceProgressSlider != null)
            {
                raceProgressSlider.value = Mathf.Clamp01(engine.DistanceTraveled / config.quarterMileDistance);
            }

            // Staging Countdown Animation (3 -> 2 -> 1)
            if (session != null && session.State == RaceState.Staging && stagingCountdownText != null)
            {
                float timer = session.StagingTimer;
                int currentNumber = Mathf.Clamp(Mathf.CeilToInt(timer), 1, 3);

                if (currentNumber != _lastDisplayedCountdown)
                {
                    _lastDisplayedCountdown = currentNumber;
                    stagingCountdownText.text = currentNumber.ToString();

                    if (currentNumber == 3)
                        stagingCountdownText.color = new Color(1f, 0.2f, 0.2f); // Red
                    else if (currentNumber == 2)
                        stagingCountdownText.color = new Color(1f, 0.6f, 0.1f); // Orange
                    else if (currentNumber == 1)
                        stagingCountdownText.color = new Color(1f, 0.9f, 0.1f); // Yellow
                }
            }
        }

        private void HandleShiftGraded(ShiftGrade grade, int fromGear)
        {
            int index = fromGear - 1;
            if (index >= 0 && shiftIndicators != null && index < shiftIndicators.Length && shiftIndicators[index] != null)
            {
                if (grade == ShiftGrade.Perfect)
                {
                    if (dotGreenSprite != null)
                    {
                        shiftIndicators[index].sprite = dotGreenSprite;
                        shiftIndicators[index].color = Color.white;
                    }
                    else
                    {
                        shiftIndicators[index].color = Color.green;
                    }
                }
                else
                {
                    if (dotRedSprite != null)
                    {
                        shiftIndicators[index].sprite = dotRedSprite;
                        shiftIndicators[index].color = Color.white;
                    }
                    else
                    {
                        shiftIndicators[index].color = Color.red;
                    }
                }
            }

            if (shiftGradeCallout != null || badgeDisplayImage != null)
            {
                if (_calloutCoroutine != null) StopCoroutine(_calloutCoroutine);
                _calloutCoroutine = StartCoroutine(ShowCalloutRoutine(grade));
            }
        }

        private IEnumerator ShowCalloutRoutine(ShiftGrade grade)
        {
            if (shiftGradeCallout != null)
            {
                shiftGradeCallout.gameObject.SetActive(true);
                switch (grade)
                {
                    case ShiftGrade.Perfect:
                        shiftGradeCallout.text = "PERFECT!";
                        shiftGradeCallout.color = new Color(0f, 1f, 0.4f);
                        break;
                    case ShiftGrade.Early:
                        shiftGradeCallout.text = "EARLY SHIFT";
                        shiftGradeCallout.color = new Color(1f, 0.7f, 0f);
                        break;
                    case ShiftGrade.Late:
                        shiftGradeCallout.text = "LATE SHIFT";
                        shiftGradeCallout.color = new Color(1f, 0.2f, 0.2f);
                        break;
                }
            }

            // Show sprite badge if assigned
            if (badgeDisplayImage != null)
            {
                Sprite targetSprite = null;
                if (grade == ShiftGrade.Perfect && badgePerfectSprite != null) targetSprite = badgePerfectSprite;
                else if (grade == ShiftGrade.Early && badgeEarlySprite != null) targetSprite = badgeEarlySprite;
                else if (grade == ShiftGrade.Late && badgeLateSprite != null) targetSprite = badgeLateSprite;

                if (targetSprite != null)
                {
                    badgeDisplayImage.gameObject.SetActive(true);
                    badgeDisplayImage.sprite = targetSprite;
                }
            }

            yield return new WaitForSeconds(0.75f);

            if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
            if (badgeDisplayImage != null) badgeDisplayImage.gameObject.SetActive(false);
        }

        private void HandleStateChanged(RaceState newState)
        {
            Debug.Log($"[Redline HUD] State changed to {newState}");

            if (newState == RaceState.Ready)
            {
                UpdateOverlays(RaceState.Ready);
                ShowReadyScreen();
            }
            else if (newState == RaceState.Staging)
            {
                _lastDisplayedCountdown = -1;
                ResetShiftDots();
                if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
                if (badgeDisplayImage != null) badgeDisplayImage.gameObject.SetActive(false);
                UpdateOverlays(RaceState.Staging);
            }
            else if (newState == RaceState.Racing)
            {
                // Flash "GO!" before hiding staging overlay
                if (stagingOverlay != null && stagingCountdownText != null)
                {
                    if (_goFlashCoroutine != null) StopCoroutine(_goFlashCoroutine);
                    _goFlashCoroutine = StartCoroutine(FlashGoAndHideRoutine());
                }
                else
                {
                    UpdateOverlays(RaceState.Racing);
                }
            }
            else if (newState == RaceState.Result)
            {
                UpdateOverlays(RaceState.Result);
                if (resultTitleText != null)
                {
                    resultTitleText.text = session.PlayerWon ? "VICTORY!" : "DEFEAT";
                    resultTitleText.color = session.PlayerWon ? new Color(0f, 1f, 0.4f) : new Color(1f, 0.27f, 0.27f);
                }
                if (resultDetailsText != null)
                {
                    resultDetailsText.text = session.ResultSummary;
                }
            }
        }

        private IEnumerator FlashGoAndHideRoutine()
        {
            stagingOverlay.SetActive(true);
            stagingCountdownText.text = "GO!";
            stagingCountdownText.color = new Color(0f, 1f, 0.4f); // Bright green

            yield return new WaitForSeconds(0.6f);

            stagingOverlay.SetActive(false);
        }

        private void ShowReadyScreen()
        {
            if (rpmSlider != null && config != null) rpmSlider.value = config.idleRpm;
            if (rpmText != null) rpmText.text = $"{(config != null ? Mathf.RoundToInt(config.idleRpm) : 2800)} RPM";
            if (speedText != null) speedText.text = "0 <size=50%>KM/H</size>";
            if (gearText != null) gearText.text = "1";
            if (distanceText != null && config != null) distanceText.text = $"0.0 m / {config.quarterMileDistance:F1} m";
            if (raceProgressSlider != null) raceProgressSlider.value = 0f;

            ResetShiftDots();
        }

        private void ResetShiftDots()
        {
            if (shiftIndicators == null) return;
            foreach (var dot in shiftIndicators)
            {
                if (dot != null)
                {
                    if (dotEmptySprite != null)
                    {
                        dot.sprite = dotEmptySprite;
                        dot.color = Color.white;
                    }
                    else
                    {
                        dot.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                    }
                }
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
