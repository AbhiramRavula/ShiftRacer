using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Redline.Config;
using Redline.Core;
using Redline.Audio;

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

        [Header("First-Person Superbike Cockpit Overlay")]
        [SerializeField] private Image cockpitOverlayImage;
        [SerializeField] private Sprite cockpitOverlaySprite;

        [Header("Analog Tachometer Dial & Needle")]
        [SerializeField] private bool showAnalogDial = false; // When false, keeps clean digital cluster with zero gauge overlapping
        [SerializeField] private Image tachometerDialImage;
        [SerializeField] private Sprite tachometerDialSprite;
        [SerializeField] private RectTransform tachometerNeedleRect;
        [SerializeField] private Sprite tachometerNeedleSprite;

        [Header("Polished Rounded UI Sprites")]
        [SerializeField] private Sprite roundedBoxSprite;
        [SerializeField] private Sprite roundedBorderSprite;
        [SerializeField] private Sprite pillBarSprite;

        [Header("Staging Start Lights Tree")]
        [SerializeField] private Image startLightsTreeImage;
        [SerializeField] private Sprite startLightsTreeSprite;

        [Header("Dashboard Frames")]
        [SerializeField] private Image speedometerFrameImage;
        [SerializeField] private Sprite speedometerFrameSprite;
        [SerializeField] private Image gearFrameImage;
        [SerializeField] private Sprite gearFrameSprite;
        [SerializeField] private Image perfectShiftsBarImage;
        [SerializeField] private Sprite perfectShiftsBarSprite;

        [Header("Audio Controller")]
        [SerializeField] private BikeAudioController audioController;

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

        [Header("Mobile Touch Controls & Sprites")]
        [SerializeField] private GameObject mobileControlsRoot;
        [SerializeField] private Sprite hintThrottleSprite;
        [SerializeField] private Sprite hintUpshiftSprite;
        [SerializeField] private Sprite hintDownshiftSprite;

        [Header("Distance Selection UI")]
        [SerializeField] private Button btnQuarterMile;
        [SerializeField] private Button btnHalfMile;
        [SerializeField] private Button btnFullMile;

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
        private Coroutine _badgePopCoroutine;
        private bool _wasPromptingShift;
        private OnScreenTouchButton _upshiftTouchBtn;
        private GameObject _throttleTouchGo;
        private TextMeshProUGUI _muteButtonText;
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
            if (audioController == null) audioController = FindAnyObjectByType<BikeAudioController>();

            if (cockpitOverlaySprite == null) cockpitOverlaySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/First-Person Superbike Cockpit Overlay.png");
            if (tachometerDialSprite == null) tachometerDialSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tachometer_Dial.png");
            if (tachometerNeedleSprite == null) tachometerNeedleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rotating Tachometer Needle.png");

            if (speedometerFrameSprite == null) speedometerFrameSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Speedometer_Frame");
            if (gearFrameSprite == null) gearFrameSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Gear_Frame");
            if (perfectShiftsBarSprite == null) perfectShiftsBarSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "PerfectShifts_Bar");
            if (startLightsTreeSprite == null) startLightsTreeSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "StartLights_Tree");

            if (badgePerfectSprite == null) badgePerfectSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Perfect");
            if (badgeEarlySprite == null) badgeEarlySprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Early");
            if (badgeLateSprite == null) badgeLateSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Late");
            if (badgeShiftNowSprite == null) badgeShiftNowSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_ShiftNow");

            if (dotEmptySprite == null) dotEmptySprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States empty");
            if (dotGreenSprite == null) dotGreenSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States green");
            if (dotRedSprite == null) dotRedSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States red");

            if (hintThrottleSprite == null) hintThrottleSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Throttle");
            if (hintUpshiftSprite == null) hintUpshiftSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Upshift");
            if (hintDownshiftSprite == null) hintDownshiftSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Downshift");

            if (roundedBoxSprite == null) roundedBoxSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Box.png");
            if (roundedBorderSprite == null) roundedBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Border.png");
            if (pillBarSprite == null) pillBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Pill_Bar.png");

            ApplyCleanUILayout();
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
            if (audioController == null) audioController = FindAnyObjectByType<BikeAudioController>();
            if (badgeDisplayImage == null) badgeDisplayImage = transform.Find("Shift_Badge_Display")?.GetComponent<Image>();

#if UNITY_EDITOR
            if (cockpitOverlaySprite == null) cockpitOverlaySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/First-Person Superbike Cockpit Overlay.png");
            if (tachometerDialSprite == null) tachometerDialSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tachometer_Dial.png");
            if (tachometerNeedleSprite == null) tachometerNeedleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rotating Tachometer Needle.png");

            if (speedometerFrameSprite == null) speedometerFrameSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Speedometer_Frame");
            if (gearFrameSprite == null) gearFrameSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Gear_Frame");
            if (perfectShiftsBarSprite == null) perfectShiftsBarSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "PerfectShifts_Bar");
            if (startLightsTreeSprite == null) startLightsTreeSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "StartLights_Tree");

            if (badgePerfectSprite == null) badgePerfectSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Perfect");
            if (badgeEarlySprite == null) badgeEarlySprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Early");
            if (badgeLateSprite == null) badgeLateSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Late");
            if (badgeShiftNowSprite == null) badgeShiftNowSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_ShiftNow");

            if (dotEmptySprite == null) dotEmptySprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States empty");
            if (dotGreenSprite == null) dotGreenSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States green");
            if (dotRedSprite == null) dotRedSprite = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States red");

            if (hintThrottleSprite == null) hintThrottleSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Throttle");
            if (hintUpshiftSprite == null) hintUpshiftSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Upshift");
            if (hintDownshiftSprite == null) hintDownshiftSprite = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Downshift");

            if (roundedBoxSprite == null) roundedBoxSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Box.png");
            if (roundedBorderSprite == null) roundedBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Border.png");
            if (pillBarSprite == null) pillBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Pill_Bar.png");
#endif

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
                session.OnDistanceModeChanged += HandleDistanceModeChanged;
            }

            EnsureVisualSpritesHUD();
            EnsureMobileTouchControls();
            EnsureDistanceSelectorUI();
            EnsureOverlayTouchTriggers();
            EnsureMuteToggleUI();
            ApplyCleanUILayout();
        }

        private void EnsureVisualSpritesHUD()
        {
            // 1. First-Person Superbike Cockpit Overlay
            if (cockpitOverlayImage == null)
            {
                var found = transform.Find("Cockpit_Overlay");
                if (found != null) cockpitOverlayImage = found.GetComponent<Image>();
            }

            if (cockpitOverlayImage == null && cockpitOverlaySprite != null)
            {
                GameObject cockpitGo = new GameObject("Cockpit_Overlay", typeof(RectTransform), typeof(Image));
                cockpitGo.transform.SetParent(transform, false);
                cockpitGo.transform.SetAsFirstSibling(); // Render behind all HUD text & controls

                RectTransform crt = cockpitGo.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0f);
                crt.anchorMax = new Vector2(0.5f, 0f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(3f, 243.5f);
                crt.sizeDelta = new Vector2(1051f, 487f);

                cockpitOverlayImage = cockpitGo.GetComponent<Image>();
                cockpitOverlayImage.sprite = cockpitOverlaySprite;
                cockpitOverlayImage.color = Color.white;
                cockpitOverlayImage.raycastTarget = false;
                cockpitOverlayImage.preserveAspect = false; // Stretch across screen
            }

            // 2. Analog Tachometer Dial & Needle
            Transform tachoPanel = transform.Find("Tachometer_Panel");
            if (tachoPanel != null)
            {
                // Polish panel background with rounded corners
                var tpImg = tachoPanel.GetComponent<Image>();
                if (tpImg != null && roundedBoxSprite != null)
                {
                    tpImg.sprite = roundedBoxSprite;
                    tpImg.type = Image.Type.Sliced;
                    tpImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
                }

                // Polish RPM_Bar with rounded pill bar
                var rpmBarTrans = tachoPanel.Find("RPM_Bar");
                if (rpmBarTrans != null)
                {
                    if (pillBarSprite != null)
                    {
                        var bgImg = rpmBarTrans.Find("Background")?.GetComponent<Image>();
                        if (bgImg != null) { bgImg.sprite = pillBarSprite; bgImg.type = Image.Type.Sliced; }

                        var fillImg = rpmBarTrans.Find("Fill Area/Fill")?.GetComponent<Image>();
                        if (fillImg != null) { fillImg.sprite = pillBarSprite; fillImg.type = Image.Type.Sliced; }
                    }

                    var gzImg = rpmBarTrans.Find("GreenWindow_Zone")?.GetComponent<Image>();
                    if (gzImg != null && roundedBoxSprite != null)
                    {
                        gzImg.sprite = roundedBoxSprite;
                        gzImg.type = Image.Type.Sliced;
                    }
                }

                if (tachometerDialImage == null)
                {
                    var d = tachoPanel.Find("Tachometer_Dial_Display");
                    if (d != null) tachometerDialImage = d.GetComponent<Image>();
                }

                if (tachometerDialImage == null && tachometerDialSprite != null)
                {
                    GameObject dialGo = new GameObject("Tachometer_Dial_Display", typeof(RectTransform), typeof(Image));
                    dialGo.transform.SetParent(tachoPanel, false);
                    dialGo.transform.SetAsFirstSibling();

                    RectTransform drt = dialGo.GetComponent<RectTransform>();
                    drt.anchorMin = new Vector2(0.5f, 0.5f);
                    drt.anchorMax = new Vector2(0.5f, 0.5f);
                    drt.pivot = new Vector2(0.5f, 0.5f);
                    drt.anchoredPosition = new Vector2(0f, 10f);
                    drt.sizeDelta = new Vector2(260f, 175f);

                    tachometerDialImage = dialGo.GetComponent<Image>();
                    tachometerDialImage.sprite = tachometerDialSprite;
                    tachometerDialImage.color = Color.white;
                    tachometerDialImage.raycastTarget = false;
                    tachometerDialImage.preserveAspect = true;

                    // Rotating Needle
                    if (tachometerNeedleSprite != null)
                    {
                        GameObject needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
                        needleGo.transform.SetParent(dialGo.transform, false);

                        tachometerNeedleRect = needleGo.GetComponent<RectTransform>();
                        tachometerNeedleRect.anchorMin = new Vector2(0.5f, 0.5f);
                        tachometerNeedleRect.anchorMax = new Vector2(0.5f, 0.5f);
                        tachometerNeedleRect.pivot = new Vector2(0.5f, 0.5f);
                        tachometerNeedleRect.anchoredPosition = Vector2.zero;
                        tachometerNeedleRect.sizeDelta = new Vector2(160f, 160f);

                        Image needleImg = needleGo.GetComponent<Image>();
                        needleImg.sprite = tachometerNeedleSprite;
                        needleImg.color = Color.white;
                        needleImg.raycastTarget = false;
                        needleImg.preserveAspect = true;
                    }

                    // By default, hide redundant overlapping analog dial
                    dialGo.SetActive(showAnalogDial);
                }
                else if (tachometerDialImage != null)
                {
                    tachometerDialImage.gameObject.SetActive(showAnalogDial);
                }
            }

            // 3. Staging Start Lights Tree (Trackside Left, never blocks the strip view)
            if (stagingOverlay != null)
            {
                if (startLightsTreeImage == null)
                {
                    var tree = stagingOverlay.transform.Find("StartLights_Tree") ?? stagingOverlay.transform.Find("Christmas_Tree_Icon");
                    if (tree != null) startLightsTreeImage = tree.GetComponent<Image>();
                }

                if (startLightsTreeImage == null && startLightsTreeSprite != null)
                {
                    GameObject treeGo = new GameObject("StartLights_Tree", typeof(RectTransform), typeof(Image));
                    treeGo.transform.SetParent(stagingOverlay.transform, false);

                    RectTransform trt = treeGo.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0f, 0.5f);
                    trt.anchorMax = new Vector2(0f, 0.5f);
                    trt.pivot = new Vector2(0f, 0.5f);
                    trt.anchoredPosition = new Vector2(80f, 20f);
                    trt.sizeDelta = new Vector2(130f, 210f);

                    startLightsTreeImage = treeGo.GetComponent<Image>();
                    startLightsTreeImage.sprite = startLightsTreeSprite;
                    startLightsTreeImage.color = Color.white;
                    startLightsTreeImage.raycastTarget = false;
                    startLightsTreeImage.preserveAspect = true;
                }
                else if (startLightsTreeImage != null)
                {
                    // Move to trackside left if placed in center
                    RectTransform trt = startLightsTreeImage.rectTransform;
                    trt.anchorMin = new Vector2(0f, 0.5f);
                    trt.anchorMax = new Vector2(0f, 0.5f);
                    trt.pivot = new Vector2(0f, 0.5f);
                    trt.anchoredPosition = new Vector2(80f, 20f);
                    trt.sizeDelta = new Vector2(130f, 210f);
                }

                // Ensure countdown text is centered and elevated above road
                if (stagingCountdownText != null)
                {
                    stagingCountdownText.rectTransform.anchoredPosition = new Vector2(0f, 130f);
                }
            }

            // Polish Race Progress Bar with rounded pill bar
            if (raceProgressSlider != null && pillBarSprite != null)
            {
                var bgImg = raceProgressSlider.transform.Find("Background")?.GetComponent<Image>();
                if (bgImg != null) { bgImg.sprite = pillBarSprite; bgImg.type = Image.Type.Sliced; }

                var fillImg = raceProgressSlider.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
                if (fillImg != null) { fillImg.sprite = pillBarSprite; fillImg.type = Image.Type.Sliced; }
            }

            // Polish Distance Readout with telemetry pill backing
            if (distanceText != null && pillBarSprite != null)
            {
                var distBg = distanceText.transform.Find("Distance_Bg")?.GetComponent<Image>();
                if (distBg == null)
                {
                    GameObject bgGo = new GameObject("Distance_Bg", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(distanceText.transform, false);
                    bgGo.transform.SetAsFirstSibling();
                    RectTransform bgrt = bgGo.GetComponent<RectTransform>();
                    bgrt.anchorMin = new Vector2(0.5f, 0.5f);
                    bgrt.anchorMax = new Vector2(0.5f, 0.5f);
                    bgrt.pivot = new Vector2(0.5f, 0.5f);
                    bgrt.anchoredPosition = Vector2.zero;
                    bgrt.sizeDelta = new Vector2(360f, 36f);
                    distBg = bgGo.GetComponent<Image>();
                    distBg.sprite = pillBarSprite;
                    distBg.type = Image.Type.Sliced;
                    distBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
                    distBg.raycastTarget = false;
                }
            }

            // 4. Frames for Speed, Gear, and Shift Dots
            if (tachoPanel != null)
            {
                var speedBg = tachoPanel.Find("Speed_Frame_Bg");
                if (speedBg != null)
                {
                    speedometerFrameImage = speedBg.GetComponent<Image>();
                    if (speedometerFrameImage != null && speedometerFrameSprite != null)
                    {
                        speedometerFrameImage.sprite = speedometerFrameSprite;
                        speedometerFrameImage.color = Color.white;
                    }
                }

                var gearBg = tachoPanel.Find("Gear_Frame_Bg");
                if (gearBg != null)
                {
                    gearFrameImage = gearBg.GetComponent<Image>();
                    if (gearFrameImage != null && gearFrameSprite != null)
                    {
                        gearFrameImage.sprite = gearFrameSprite;
                        gearFrameImage.color = Color.white;
                    }
                }
            }

            var dotsPanel = transform.Find("ShiftDots_Group");
            if (dotsPanel != null)
            {
                perfectShiftsBarImage = dotsPanel.GetComponent<Image>();
                if (perfectShiftsBarImage != null && perfectShiftsBarSprite != null)
                {
                    perfectShiftsBarImage.sprite = perfectShiftsBarSprite;
                    perfectShiftsBarImage.color = Color.white;
                }
            }
        }

        public void ApplyCleanUILayout()
        {
            // 1. Top HUD: Clean separation between progress bar and distance readout
            if (raceProgressSlider != null)
            {
                var prt = raceProgressSlider.GetComponent<RectTransform>();
                if (prt != null)
                {
                    prt.anchoredPosition = new Vector2(0f, -25f);
                    prt.sizeDelta = new Vector2(720f, 16f);
                }
            }

            if (distanceText != null)
            {
                distanceText.rectTransform.anchoredPosition = new Vector2(0f, -62f);
                distanceText.rectTransform.sizeDelta = new Vector2(400f, 32f);
                distanceText.fontSize = 20f;
            }

            // 2. Ready Overlay: Generous, non-overlapping vertical spacing
            if (readyOverlay != null)
            {
                var title = readyOverlay.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null)
                {
                    title.rectTransform.anchoredPosition = new Vector2(0f, 225f);
                    title.rectTransform.sizeDelta = new Vector2(800f, 65f);
                    title.fontSize = 58f;
                }

                var subtitle = readyOverlay.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
                if (subtitle != null)
                {
                    subtitle.rectTransform.anchoredPosition = new Vector2(0f, 178f);
                    subtitle.rectTransform.sizeDelta = new Vector2(800f, 30f);
                    subtitle.fontSize = 18f;
                }

                var distPanel = readyOverlay.transform.Find("Distance_Selector_Panel");
                if (distPanel != null)
                {
                    var prt = distPanel.GetComponent<RectTransform>();
                    if (prt != null)
                    {
                        prt.anchoredPosition = new Vector2(0f, 75f);
                        prt.sizeDelta = new Vector2(740f, 95f);
                    }
                    var hrt = distPanel.Find("Header_Text")?.GetComponent<RectTransform>();
                    if (hrt != null)
                    {
                        hrt.anchoredPosition = new Vector2(0f, 38f);
                    }
                    var b1 = distPanel.Find("Btn_1_4_Mile")?.GetComponent<RectTransform>();
                    if (b1 != null) b1.anchoredPosition = new Vector2(-240f, -12f);
                    var b2 = distPanel.Find("Btn_1_2_Mile")?.GetComponent<RectTransform>();
                    if (b2 != null) b2.anchoredPosition = new Vector2(0f, -12f);
                    var b3 = distPanel.Find("Btn_1_Mile")?.GetComponent<RectTransform>();
                    if (b3 != null) b3.anchoredPosition = new Vector2(240f, -12f);
                }

                var hint = readyOverlay.transform.Find("Hint")?.GetComponent<TextMeshProUGUI>();
                if (hint != null)
                {
                    hint.rectTransform.anchoredPosition = new Vector2(0f, -25f);
                    hint.rectTransform.sizeDelta = new Vector2(850f, 55f);
                    hint.fontSize = 20f;
                    hint.text = "<color=#FFE600>WIDE-OPEN AUTO-THROTTLE ENGAGED</color>\nPRESS <color=#00F0FF><b>SHIFT KEY</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> AT THE <color=#00FF66><b>GREEN ZONE</b></color> TO SHIFT GEARS";
                }

                var stageBtn = readyOverlay.transform.Find("Stage_Prompt_Button");
                if (stageBtn != null)
                {
                    var srt = stageBtn.GetComponent<RectTransform>();
                    if (srt != null)
                    {
                        srt.anchoredPosition = new Vector2(0f, -108f);
                        srt.sizeDelta = new Vector2(480f, 54f);
                    }
                    var stxt = stageBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (stxt != null)
                    {
                        stxt.text = "<b>PRESS SHIFT / SPACE OR TAP TO STAGE</b>";
                        stxt.fontSize = 21f;
                    }
                }
            }

            // 3. Result Overlay: Clear hierarchy, zero overlapping text
            if (resultOverlay != null)
            {
                var card = resultOverlay.transform.Find("Result_Card");
                if (card != null)
                {
                    var cardRt = card.GetComponent<RectTransform>();
                    if (cardRt != null)
                    {
                        cardRt.anchoredPosition = Vector2.zero;
                        cardRt.sizeDelta = new Vector2(820f, 360f);
                    }

                    var title = card.Find("Result_Title")?.GetComponent<TextMeshProUGUI>();
                    if (title != null)
                    {
                        title.rectTransform.anchoredPosition = new Vector2(0f, 115f);
                        title.rectTransform.sizeDelta = new Vector2(760f, 70f);
                        title.fontSize = 62f;
                    }

                    var details = card.Find("Result_Details")?.GetComponent<TextMeshProUGUI>();
                    if (details != null)
                    {
                        details.rectTransform.anchoredPosition = new Vector2(0f, -5f);
                        details.rectTransform.sizeDelta = new Vector2(760f, 105f);
                        details.fontSize = 22f;
                    }

                    var hint = card.Find("Restart_Hint")?.GetComponent<TextMeshProUGUI>();
                    if (hint != null)
                    {
                        hint.rectTransform.anchoredPosition = new Vector2(0f, -125f);
                        hint.rectTransform.sizeDelta = new Vector2(760f, 40f);
                        hint.fontSize = 20f;
                        hint.text = "PRESS <color=#00F0FF><b>SHIFT / SPACE</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> TO RETURN TO MENU";
                    }

                    var cardBtn = card.GetComponent<Button>();
                    if (cardBtn == null) cardBtn = card.gameObject.AddComponent<Button>();
                    cardBtn.onClick.RemoveAllListeners();
                    cardBtn.onClick.AddListener(() => session?.ResetToReady());
                }
            }
        }

        private void EnsureMobileTouchControls()
        {
            if (mobileControlsRoot == null)
            {
                var found = transform.Find("Mobile_Touch_Controls");
                if (found != null) mobileControlsRoot = found.gameObject;
            }

            if (mobileControlsRoot == null)
            {
                GameObject rootGo = new GameObject("Mobile_Touch_Controls", typeof(RectTransform));
                rootGo.transform.SetParent(transform, false);
                RectTransform rootRect = rootGo.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                mobileControlsRoot = rootGo;

                // Add MobileTouchController manager
                rootGo.AddComponent<MobileTouchController>();

                // Add SafeAreaFitter to keep controls safely within notch / mobile margins
                rootGo.AddComponent<SafeAreaFitter>();

                // 1. Throttle Button (Bottom-Right: thumb rests naturally here in landscape)
                CreateTouchButton(rootGo.transform, "Touch_Throttle_Button", hintThrottleSprite, TouchButtonType.Throttle,
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(210f, 115f));

                // 2. Upshift Button (Bottom-Right, directly above throttle for natural thumb reach)
                CreateTouchButton(rootGo.transform, "Touch_Upshift_Button", hintUpshiftSprite, TouchButtonType.Upshift,
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 175f), new Vector2(210f, 115f));

                // 3. Downshift Button (Bottom-Left)
                CreateTouchButton(rootGo.transform, "Touch_Downshift_Button", hintDownshiftSprite, TouchButtonType.Downshift,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(210f, 115f));
            }
        }

        private GameObject CreateTouchButton(Transform parent, string name, Sprite sprite, TouchButtonType type,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(OnScreenTouchButton));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = true;
            img.preserveAspect = true;

            var touchBtn = btnGo.GetComponent<OnScreenTouchButton>();
            touchBtn.SetButtonType(type);

            return btnGo;
        }

        private void EnsureDistanceSelectorUI()
        {
            if (readyOverlay == null) return;

            Transform existingPanel = readyOverlay.transform.Find("Distance_Selector_Panel");
            if (existingPanel == null)
            {
                GameObject panelGo = new GameObject("Distance_Selector_Panel", typeof(RectTransform));
                panelGo.transform.SetParent(readyOverlay.transform, false);

                RectTransform prt = panelGo.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.5f, 0.5f);
                prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = new Vector2(0f, 75f);
                prt.sizeDelta = new Vector2(740f, 95f);

                // Selector Header Text
                GameObject headerGo = new GameObject("Header_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                headerGo.transform.SetParent(panelGo.transform, false);
                RectTransform hrt = headerGo.GetComponent<RectTransform>();
                hrt.anchoredPosition = new Vector2(0f, 38f);
                hrt.sizeDelta = new Vector2(700f, 30f);
                TextMeshProUGUI headerTxt = headerGo.GetComponent<TextMeshProUGUI>();
                headerTxt.text = "<color=#9AA0A6>RACE DISTANCE:</color>  <size=85%><color=#00F0FF>TAP</color> OR PRESS <color=#FFE600>TAB / D</color></size>";
                headerTxt.fontSize = 18;
                headerTxt.fontStyle = FontStyles.Bold;
                headerTxt.alignment = TextAlignmentOptions.Center;
                headerTxt.color = new Color(0.9f, 0.9f, 0.95f);

                // 3 Buttons: 1/4 Mile, 1/2 Mile, 1 Mile
                btnQuarterMile = CreateDistanceTab(panelGo.transform, "Btn_1_4_Mile", "<b>1/4 MILE</b>\n<size=65%>402 METERS</size>", new Vector2(-240f, -12f));
                btnHalfMile = CreateDistanceTab(panelGo.transform, "Btn_1_2_Mile", "<b>1/2 MILE</b>\n<size=65%>805 METERS</size>", new Vector2(0f, -12f));
                btnFullMile = CreateDistanceTab(panelGo.transform, "Btn_1_Mile", "<b>1 MILE</b>\n<size=65%>1609 METERS</size>", new Vector2(240f, -12f));
            }
            else
            {
                RectTransform prt = existingPanel.GetComponent<RectTransform>();
                if (prt != null)
                {
                    prt.anchoredPosition = new Vector2(0f, 75f);
                    prt.sizeDelta = new Vector2(740f, 95f);
                }
                var hrt = existingPanel.Find("Header_Text")?.GetComponent<RectTransform>();
                if (hrt != null)
                {
                    hrt.anchoredPosition = new Vector2(0f, 38f);
                }

                if (btnQuarterMile == null) btnQuarterMile = existingPanel.Find("Btn_1_4_Mile")?.GetComponent<Button>();
                if (btnHalfMile == null) btnHalfMile = existingPanel.Find("Btn_1_2_Mile")?.GetComponent<Button>();
                if (btnFullMile == null) btnFullMile = existingPanel.Find("Btn_1_Mile")?.GetComponent<Button>();

                if (btnQuarterMile != null) btnQuarterMile.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240f, -12f);
                if (btnHalfMile != null) btnHalfMile.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -12f);
                if (btnFullMile != null) btnFullMile.GetComponent<RectTransform>().anchoredPosition = new Vector2(240f, -12f);
            }

            // Always bind fresh, reliable click/touch listeners to all 3 tabs
            if (btnQuarterMile != null)
            {
                btnQuarterMile.onClick.RemoveAllListeners();
                btnQuarterMile.onClick.AddListener(() => session?.SetDistanceMode(RaceDistanceMode.QuarterMile));
            }
            if (btnHalfMile != null)
            {
                btnHalfMile.onClick.RemoveAllListeners();
                btnHalfMile.onClick.AddListener(() => session?.SetDistanceMode(RaceDistanceMode.HalfMile));
            }
            if (btnFullMile != null)
            {
                btnFullMile.onClick.RemoveAllListeners();
                btnFullMile.onClick.AddListener(() => session?.SetDistanceMode(RaceDistanceMode.FullMile));
            }

            UpdateDistanceButtonStyles();
        }

        private Button CreateDistanceTab(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(220f, 65f);

            Image img = btnGo.GetComponent<Image>();
            if (roundedBoxSprite != null)
            {
                img.sprite = roundedBoxSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.12f, 0.13f, 0.17f, 0.95f);
            img.raycastTarget = true;

            // Rounded border outline
            if (roundedBorderSprite != null)
            {
                GameObject borderGo = new GameObject("Border_Outline", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(btnGo.transform, false);
                RectTransform brt = borderGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image borderImg = borderGo.GetComponent<Image>();
                borderImg.sprite = roundedBorderSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = new Color(0.2f, 0.22f, 0.28f, 0.45f);
                borderImg.raycastTarget = false;
            }

            Button btn = btnGo.GetComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // Don't intercept touches from the parent button

            return btn;
        }

        private void HandleDistanceModeChanged(RaceDistanceMode mode)
        {
            UpdateDistanceButtonStyles();
            float targetDist = session != null ? session.TargetRaceDistance : (config != null ? config.raceFinishDistance : 402.336f);
            if (distanceText != null && (session == null || session.State == RaceState.Ready))
            {
                distanceText.text = $"<color=#00F0FF><b>0.0 m</b></color>  <color=#555566>/</color>  <color=#FFFFFF>{targetDist:F1} m</color>";
            }
        }

        private void UpdateDistanceButtonStyles()
        {
            if (session == null) return;
            RaceDistanceMode current = session.DistanceMode;

            UpdateDistanceTabLabel(btnQuarterMile, "1/4 MILE", "402M", session.GetBestTime(RaceDistanceMode.QuarterMile));
            UpdateDistanceTabLabel(btnHalfMile, "1/2 MILE", "805M", session.GetBestTime(RaceDistanceMode.HalfMile));
            UpdateDistanceTabLabel(btnFullMile, "1 MILE", "1609M", session.GetBestTime(RaceDistanceMode.FullMile));

            StyleDistanceTab(btnQuarterMile, current == RaceDistanceMode.QuarterMile);
            StyleDistanceTab(btnHalfMile, current == RaceDistanceMode.HalfMile);
            StyleDistanceTab(btnFullMile, current == RaceDistanceMode.FullMile);
        }

        private void UpdateDistanceTabLabel(Button btn, string title, string meters, float bestTime)
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null) return;

            if (bestTime > 0f)
                txt.text = $"<b>{title}</b>\n<size=60%>{meters} | <color=#00F0FF>BEST {bestTime:F2}s</color></size>";
            else
                txt.text = $"<b>{title}</b>\n<size=65%>{meters}</size>";
        }

        private void StyleDistanceTab(Button btn, bool isSelected)
        {
            if (btn == null) return;

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected 
                    ? new Color(0.04f, 0.44f, 0.25f, 0.95f) // Highlighted racing green
                    : new Color(0.08f, 0.09f, 0.13f, 0.90f); // Sleek carbon dark
            }

            Transform border = btn.transform.Find("Border_Outline");
            if (border != null)
            {
                Image bImg = border.GetComponent<Image>();
                if (bImg != null)
                {
                    bImg.color = isSelected ? new Color(0f, 1f, 0.55f, 0.9f) : new Color(0.2f, 0.22f, 0.28f, 0.4f);
                }
            }

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = isSelected ? new Color(0.1f, 1f, 0.6f) : new Color(0.7f, 0.72f, 0.8f);
            }
        }

        private void EnsureOverlayTouchTriggers()
        {
            // Allow tapping anywhere on Ready screen or Result screen on mobile to start/replay
            if (readyOverlay != null)
            {
                var bgImg = readyOverlay.GetComponent<Image>();
                if (bgImg != null) bgImg.raycastTarget = false; // Don't intercept distance button clicks

                // Perfectly spaced layout for Ready Screen elements (No overlap, generous breathing room)
                var title = readyOverlay.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null)
                {
                    title.rectTransform.anchoredPosition = new Vector2(0f, 225f);
                    title.rectTransform.sizeDelta = new Vector2(800f, 65f);
                    title.fontSize = 58f;
                }

                var subtitle = readyOverlay.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
                if (subtitle != null)
                {
                    subtitle.rectTransform.anchoredPosition = new Vector2(0f, 178f);
                    subtitle.rectTransform.sizeDelta = new Vector2(800f, 30f);
                    subtitle.fontSize = 18f;
                }

                var hint = readyOverlay.transform.Find("Hint")?.GetComponent<TextMeshProUGUI>();
                if (hint != null)
                {
                    hint.rectTransform.anchoredPosition = new Vector2(0f, -25f);
                    hint.rectTransform.sizeDelta = new Vector2(850f, 55f);
                    hint.fontSize = 20f;
                    hint.text = "<color=#FFE600>WIDE-OPEN AUTO-THROTTLE ENGAGED</color>\nPRESS <color=#00F0FF><b>SHIFT KEY</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> AT THE <color=#00FF66><b>GREEN ZONE</b></color> TO SHIFT GEARS";
                }

                Transform existingStageBtn = readyOverlay.transform.Find("Stage_Prompt_Button");
                if (existingStageBtn == null)
                {
                    GameObject stageBtnGo = new GameObject("Stage_Prompt_Button", typeof(RectTransform), typeof(Image), typeof(OnScreenTouchButton));
                    stageBtnGo.transform.SetParent(readyOverlay.transform, false);

                    RectTransform srt = stageBtnGo.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0.5f, 0.5f);
                    srt.anchorMax = new Vector2(0.5f, 0.5f);
                    srt.pivot = new Vector2(0.5f, 0.5f);
                    srt.anchoredPosition = new Vector2(0f, -108f);
                    srt.sizeDelta = new Vector2(480f, 54f);

                    Image bimg = stageBtnGo.GetComponent<Image>();
                    if (pillBarSprite != null)
                    {
                        bimg.sprite = pillBarSprite;
                        bimg.type = Image.Type.Sliced;
                    }
                    bimg.color = new Color(0.85f, 0.18f, 0.12f, 0.95f);
                    bimg.raycastTarget = true;

                    var touchBtn = stageBtnGo.GetComponent<OnScreenTouchButton>();
                    touchBtn.SetButtonType(TouchButtonType.AnyScreenTap);

                    var sBtn = stageBtnGo.GetComponent<Button>();
                    if (sBtn == null) sBtn = stageBtnGo.AddComponent<Button>();
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(() => session?.StartStaging());

                    GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(stageBtnGo.transform, false);
                    RectTransform trt = textGo.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;

                    TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
                    tmp.text = "<b>PRESS SHIFT / SPACE OR TAP TO STAGE</b>";
                    tmp.fontSize = 21;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }
                else
                {
                    // Polish existing button with pill bar and updated prompt
                    RectTransform srt = existingStageBtn.GetComponent<RectTransform>();
                    if (srt != null)
                    {
                        srt.anchoredPosition = new Vector2(0f, -108f);
                        srt.sizeDelta = new Vector2(480f, 54f);
                    }

                    var bimg = existingStageBtn.GetComponent<Image>();
                    if (bimg != null && pillBarSprite != null)
                    {
                        bimg.sprite = pillBarSprite;
                        bimg.type = Image.Type.Sliced;
                    }

                    var sBtn = existingStageBtn.GetComponent<Button>();
                    if (sBtn == null) sBtn = existingStageBtn.gameObject.AddComponent<Button>();
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(() => session?.StartStaging());

                    var txt = existingStageBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = "<b>PRESS SHIFT / SPACE OR TAP TO STAGE</b>";
                        txt.fontSize = 21f;
                    }
                }
            }

            if (resultOverlay != null)
            {
                var img = resultOverlay.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
                
                // Remove legacy touch button that was triggering _startTriggered
                var touchBtn = resultOverlay.GetComponent<OnScreenTouchButton>();
                if (touchBtn != null) Destroy(touchBtn);

                var rBtn = resultOverlay.GetComponent<Button>();
                if (rBtn == null) rBtn = resultOverlay.gameObject.AddComponent<Button>();
                rBtn.onClick.RemoveAllListeners();
                rBtn.onClick.AddListener(() => session?.ResetToReady());
            }
        }

        private void EnsureMuteToggleUI()
        {
            Transform existing = transform.Find("Mute_Toggle_Button");
            if (existing == null)
            {
                GameObject muteGo = new GameObject("Mute_Toggle_Button", typeof(RectTransform), typeof(Image), typeof(Button));
                muteGo.transform.SetParent(transform, false);

                RectTransform rt = muteGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-24f, -20f);
                rt.sizeDelta = new Vector2(118f, 36f);

                Image img = muteGo.GetComponent<Image>();
                if (pillBarSprite != null)
                {
                    img.sprite = pillBarSprite;
                    img.type = Image.Type.Sliced;
                }
                img.color = new Color(0.08f, 0.10f, 0.15f, 0.88f);
                img.raycastTarget = true;

                GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(muteGo.transform, false);
                RectTransform trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                _muteButtonText = txtGo.GetComponent<TextMeshProUGUI>();
                _muteButtonText.fontSize = 15f;
                _muteButtonText.fontStyle = FontStyles.Bold;
                _muteButtonText.alignment = TextAlignmentOptions.Center;
                _muteButtonText.raycastTarget = false;

                var btn = muteGo.GetComponent<Button>();
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.onClick.AddListener(ToggleMuteAudio);
            }
            else
            {
                _muteButtonText = existing.GetComponentInChildren<TextMeshProUGUI>();
                var btn = existing.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ToggleMuteAudio);
                }
            }

            RefreshMuteLabel();
        }

        private void ToggleMuteAudio()
        {
            if (audioController != null)
            {
                audioController.ToggleMute();
                RefreshMuteLabel();
            }
        }

        private void RefreshMuteLabel()
        {
            if (_muteButtonText == null) return;
            bool muted = audioController != null && audioController.IsMuted;
            _muteButtonText.text = muted ? "SFX: OFF" : "SFX: ON";
            _muteButtonText.color = muted ? new Color(1f, 0.35f, 0.35f) : new Color(0f, 0.95f, 1f);
        }

        private void OnDestroy()
        {
            if (judge != null) judge.OnShiftGraded -= HandleShiftGraded;
            if (session != null)
            {
                session.OnStateChanged -= HandleStateChanged;
                session.OnDistanceModeChanged -= HandleDistanceModeChanged;
            }
        }

        private void Start()
        {
            float maxDialRpm = 14000f;
            if (config != null && rpmSlider != null)
            {
                rpmSlider.minValue = 0f;
                rpmSlider.maxValue = maxDialRpm;
            }

            // Synchronize Green Window zone inside RPM bar with shift window
            Transform greenZone = rpmSlider != null ? rpmSlider.transform.Find("GreenWindow_Zone") : null;
            if (greenZone != null && config != null)
            {
                RectTransform gz = greenZone.GetComponent<RectTransform>();
                float minNorm = config.perfectWindowMin / maxDialRpm;
                float maxNorm = config.perfectWindowMax / maxDialRpm;
                gz.anchorMin = new Vector2(minNorm, 0f);
                gz.anchorMax = new Vector2(maxNorm, 1f);
                gz.offsetMin = Vector2.zero;
                gz.offsetMax = Vector2.zero;
            }

            ResetShiftDots();
            ShowReadyScreen();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            {
                ToggleMuteAudio();
            }

            if (engine == null || config == null) return;

            // Live RPM meter updates during both Staging (revving on line) and Racing
            if (rpmSlider != null) rpmSlider.value = engine.CurrentRpm;
            if (rpmText != null) rpmText.text = $"{Mathf.RoundToInt(engine.CurrentRpm)} RPM";

            // Rotate analog tachometer needle - 100% synchronized with RPM slider and dial arc
            if (tachometerNeedleRect != null)
            {
                float maxDialRpm = 14000f;
                float rpmRatio = Mathf.Clamp01(engine.CurrentRpm / maxDialRpm);
                float angle = Mathf.Lerp(130f, -130f, rpmRatio);
                tachometerNeedleRect.localEulerAngles = new Vector3(0f, 0f, angle);
            }

            // Speed & Gear
            if (speedText != null) speedText.text = $"{Mathf.RoundToInt(engine.CurrentSpeedKmh)} <size=50%>KM/H</size>";
            if (gearText != null) gearText.text = $"{engine.CurrentGear}";

            // REAL-TIME SHIFT PROMPT: Lights up when entering green window (11,400-12,600 RPM) or redline!
            // In 6th (top) gear or during finish rollout, there are no more gears to shift into, so we never prompt to shift!
            int maxGears = (config != null && config.gearTopSpeedsKmh != null) ? config.gearTopSpeedsKmh.Length : 6;
            bool canUpshift = engine.CurrentGear < maxGears;
            bool isRacingActive = session != null && session.State == RaceState.Racing && !session.IsFinishingRollout;
            bool isStaging = session != null && session.State == RaceState.Staging;
            bool inGreenWindow = engine.CurrentRpm >= config.perfectWindowMin && engine.CurrentRpm <= config.perfectWindowMax;
            bool atRedline = engine.CurrentRpm > config.perfectWindowMax || engine.IsOnLimiter;
            bool shouldPromptShift = canUpshift && isRacingActive && (inGreenWindow || atRedline);

            if (limiterWarningCue != null)
            {
                if (isStaging)
                {
                    limiterWarningCue.SetActive(true);
                    var cueRt = limiterWarningCue.GetComponent<RectTransform>();
                    if (cueRt != null)
                    {
                        cueRt.anchoredPosition = new Vector2(0f, 185f);
                        cueRt.sizeDelta = new Vector2(650f, 45f);
                    }
                    var cueText = limiterWarningCue.GetComponent<TextMeshProUGUI>();
                    if (cueText != null)
                    {
                        bool inLaunchZone = engine.CurrentRpm >= RaceSessionController.PerfectLaunchMinRpm &&
                                            engine.CurrentRpm <= RaceSessionController.PerfectLaunchMaxRpm;
                        cueText.fontSize = 26f;
                        cueText.text = inLaunchZone ? "LAUNCH ZONE READY!" : "REV 8K-10.8K RPM FOR HOLE-SHOT";
                        cueText.color = inLaunchZone ? new Color(0f, 1f, 0.45f) : new Color(1f, 0.78f, 0.2f);
                    }
                }
                else
                {
                    bool hasShiftSprite = badgeDisplayImage != null && badgeShiftNowSprite != null;
                    limiterWarningCue.SetActive(shouldPromptShift && !hasShiftSprite);
                    if (!hasShiftSprite)
                    {
                        var cueRt = limiterWarningCue.GetComponent<RectTransform>();
                        if (cueRt != null)
                        {
                            cueRt.anchoredPosition = new Vector2(0f, 125f);
                            cueRt.sizeDelta = new Vector2(520f, 60f);
                        }

                        var cueText = limiterWarningCue.GetComponent<TextMeshProUGUI>();
                        if (cueText != null)
                        {
                            cueText.fontSize = 44f;
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
                    }
                }
            }

            // Real-time Shift Now Badge & Button Pop (triggered during shift window)
            if (shouldPromptShift && !_wasPromptingShift)
            {
                if (_upshiftTouchBtn == null && mobileControlsRoot != null)
                    _upshiftTouchBtn = mobileControlsRoot.transform.Find("Touch_Upshift_Button")?.GetComponent<OnScreenTouchButton>();
                _upshiftTouchBtn?.PlayPopAnimation(1.22f, 0.32f);

                if (badgeDisplayImage != null && badgeShiftNowSprite != null && _calloutCoroutine == null)
                {
                    badgeDisplayImage.gameObject.SetActive(true);
                    badgeDisplayImage.sprite = badgeShiftNowSprite;
                    if (_badgePopCoroutine != null) StopCoroutine(_badgePopCoroutine);
                    _badgePopCoroutine = StartCoroutine(PopTransformRoutine(badgeDisplayImage.transform, 1.25f, 0.30f));
                }
            }
            else if (!shouldPromptShift && _calloutCoroutine == null && badgeDisplayImage != null && badgeDisplayImage.gameObject.activeSelf)
            {
                badgeDisplayImage.gameObject.SetActive(false);
            }
            _wasPromptingShift = shouldPromptShift;

            // Distance & Progress Bar (Respects Selected Distance: 1/4, 1/2, or Full Mile)
            float targetDist = session != null ? session.TargetRaceDistance : (config != null ? config.raceFinishDistance : 402.336f);
            if (distanceText != null)
            {
                distanceText.text = $"<color=#00F0FF><b>{engine.DistanceTraveled:F1} m</b></color>  <color=#555566>/</color>  <color=#FFFFFF>{targetDist:F1} m</color>";
            }

            if (raceProgressSlider != null)
            {
                raceProgressSlider.value = Mathf.Clamp01(engine.DistanceTraveled / targetDist);
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
                    audioController?.PlayCountdownBeep();

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

        private IEnumerator ShowLaunchCalloutRoutine()
        {
            if (shiftGradeCallout != null)
            {
                shiftGradeCallout.gameObject.SetActive(true);
                shiftGradeCallout.text = "HOLE-SHOT LAUNCH!";
                shiftGradeCallout.color = new Color(1f, 0.85f, 0.1f);
            }

            yield return new WaitForSeconds(0.8f);

            if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
            _calloutCoroutine = null;
        }

        private IEnumerator ShowCalloutRoutine(ShiftGrade grade)
        {
            Sprite targetSprite = null;
            if (badgeDisplayImage != null)
            {
                if (grade == ShiftGrade.Perfect && badgePerfectSprite != null) targetSprite = badgePerfectSprite;
                else if (grade == ShiftGrade.Early && badgeEarlySprite != null) targetSprite = badgeEarlySprite;
                else if (grade == ShiftGrade.Late && badgeLateSprite != null) targetSprite = badgeLateSprite;
            }

            if (targetSprite != null)
            {
                if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
                badgeDisplayImage.gameObject.SetActive(true);
                badgeDisplayImage.sprite = targetSprite;
            }
            else if (shiftGradeCallout != null)
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

            yield return new WaitForSeconds(0.75f);

            if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
            if (badgeDisplayImage != null) badgeDisplayImage.gameObject.SetActive(false);
            _calloutCoroutine = null;
        }

        private void HandleStateChanged(RaceState newState)
        {
            Debug.Log($"[Redline HUD] State changed to {newState}");

            if (_throttleTouchGo == null && mobileControlsRoot != null)
                _throttleTouchGo = mobileControlsRoot.transform.Find("Touch_Throttle_Button")?.gameObject;

            bool autoThrottle = config == null || config.autoThrottleDuringRace;

            if (newState == RaceState.Ready)
            {
                if (_throttleTouchGo != null) _throttleTouchGo.SetActive(!autoThrottle);
                UpdateOverlays(RaceState.Ready);
                ShowReadyScreen();

                // Initial attention-grabbing pops so player intuitively clicks/taps
                if (_upshiftTouchBtn == null && mobileControlsRoot != null)
                    _upshiftTouchBtn = mobileControlsRoot.transform.Find("Touch_Upshift_Button")?.GetComponent<OnScreenTouchButton>();
                _upshiftTouchBtn?.PlayPopAnimation(1.24f, 0.45f);

                var stageBtn = readyOverlay?.transform.Find("Stage_Prompt_Button")?.GetComponent<OnScreenTouchButton>();
                stageBtn?.PlayPopAnimation(1.15f, 0.40f);
            }
            else if (newState == RaceState.Staging)
            {
                // Allow revving on the staging line with the touch throttle button
                if (_throttleTouchGo != null) _throttleTouchGo.SetActive(true);
                _lastDisplayedCountdown = -1;
                ResetShiftDots();
                if (shiftGradeCallout != null) shiftGradeCallout.gameObject.SetActive(false);
                if (badgeDisplayImage != null) badgeDisplayImage.gameObject.SetActive(false);
                UpdateOverlays(RaceState.Staging);
            }
            else if (newState == RaceState.Racing)
            {
                // Pure one-button SHIFT focus during auto-throttle race
                if (_throttleTouchGo != null) _throttleTouchGo.SetActive(!autoThrottle);

                // Initial launch pop for the shift button
                if (_upshiftTouchBtn == null && mobileControlsRoot != null)
                    _upshiftTouchBtn = mobileControlsRoot.transform.Find("Touch_Upshift_Button")?.GetComponent<OnScreenTouchButton>();
                _upshiftTouchBtn?.PlayPopAnimation(1.28f, 0.40f);

                if (session != null && session.GotPerfectLaunch && shiftGradeCallout != null)
                {
                    if (_calloutCoroutine != null) StopCoroutine(_calloutCoroutine);
                    _calloutCoroutine = StartCoroutine(ShowLaunchCalloutRoutine());
                }

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
                    resultTitleText.fontSize = 62f;
                    resultTitleText.rectTransform.anchoredPosition = new Vector2(0f, 115f);
                    resultTitleText.rectTransform.sizeDelta = new Vector2(760f, 70f);
                }
                if (resultDetailsText != null)
                {
                    resultDetailsText.text = session.ResultSummary;
                    resultDetailsText.fontSize = 22f;
                    resultDetailsText.rectTransform.anchoredPosition = new Vector2(0f, -5f);
                    resultDetailsText.rectTransform.sizeDelta = new Vector2(760f, 105f);
                }

                // Dynamic glowing border outline & restart prompt on Result Card
                if (resultOverlay != null)
                {
                    var card = resultOverlay.transform.Find("Result_Card");
                    if (card != null)
                    {
                        var cardRt = card.GetComponent<RectTransform>();
                        if (cardRt != null)
                        {
                            cardRt.sizeDelta = new Vector2(820f, 360f);
                            cardRt.anchoredPosition = Vector2.zero;
                        }

                        var cardImg = card.GetComponent<Image>();
                        if (cardImg != null && roundedBoxSprite != null)
                        {
                            cardImg.sprite = roundedBoxSprite;
                            cardImg.type = Image.Type.Sliced;
                        }

                        var border = card.Find("Border_Outline");
                        if (border == null && roundedBorderSprite != null)
                        {
                            GameObject bgo = new GameObject("Border_Outline", typeof(RectTransform), typeof(Image));
                            bgo.transform.SetParent(card, false);
                            var brt = bgo.GetComponent<RectTransform>();
                            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                            var bi = bgo.GetComponent<Image>();
                            bi.sprite = roundedBorderSprite;
                            bi.type = Image.Type.Sliced;
                            bi.raycastTarget = false;
                            border = bgo.transform;
                        }

                        if (border != null)
                        {
                            var bi = border.GetComponent<Image>();
                            if (bi != null)
                            {
                                bi.color = session.PlayerWon ? new Color(0f, 1f, 0.5f, 0.85f) : new Color(1f, 0.25f, 0.25f, 0.85f);
                            }
                        }

                        var hint = card.Find("Restart_Hint")?.GetComponent<TextMeshProUGUI>();
                        if (hint != null)
                        {
                            hint.text = "PRESS <color=#00F0FF><b>SHIFT / SPACE</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> TO RETURN TO MENU";
                            hint.fontSize = 20f;
                            hint.rectTransform.anchoredPosition = new Vector2(0f, -125f);
                            hint.rectTransform.sizeDelta = new Vector2(760f, 40f);
                        }

                        var cardBtn = card.GetComponent<Button>();
                        if (cardBtn == null) cardBtn = card.gameObject.AddComponent<Button>();
                        cardBtn.onClick.RemoveAllListeners();
                        cardBtn.onClick.AddListener(() => session?.ResetToReady());
                    }
                }

                if (session != null && session.PlayerWon)
                {
                    audioController?.PlayVictoryFanfare();
                }
            }
        }

        private IEnumerator FlashGoAndHideRoutine()
        {
            stagingOverlay.SetActive(true);
            stagingCountdownText.text = "GO!";
            stagingCountdownText.color = new Color(0f, 1f, 0.4f); // Bright green
            audioController?.PlayGreenLightGo();

            yield return new WaitForSeconds(0.6f);

            stagingOverlay.SetActive(false);
        }

        private void ShowReadyScreen()
        {
            float targetDist = session != null ? session.TargetRaceDistance : (config != null ? config.raceFinishDistance : 402.336f);
            if (rpmSlider != null && config != null) rpmSlider.value = config.idleRpm;
            if (tachometerNeedleRect != null && config != null)
            {
                float rpmRatio = Mathf.Clamp01(config.idleRpm / 14000f);
                float angle = Mathf.Lerp(130f, -130f, rpmRatio);
                tachometerNeedleRect.localEulerAngles = new Vector3(0f, 0f, angle);
            }
            if (rpmText != null) rpmText.text = $"{(config != null ? Mathf.RoundToInt(config.idleRpm) : 2800)} RPM";
            if (speedText != null) speedText.text = "0 <size=50%>KM/H</size>";
            if (gearText != null) gearText.text = "1";
            if (distanceText != null) distanceText.text = $"<color=#00F0FF><b>0.0 m</b></color>  <color=#555566>/</color>  <color=#FFFFFF>{targetDist:F1} m</color>";
            if (raceProgressSlider != null) raceProgressSlider.value = 0f;

            ResetShiftDots();
            UpdateDistanceButtonStyles();
            ApplyCleanUILayout();
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

        private IEnumerator PopTransformRoutine(Transform target, float punchScale, float duration)
        {
            if (target == null) yield break;
            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float halfDuration = duration * 0.45f;
            while (elapsed < halfDuration)
            {
                if (target == null) yield break;
                float t = elapsed / halfDuration;
                float s = Mathf.Sin(t * Mathf.PI * 0.5f);
                target.localScale = Vector3.Lerp(baseScale * 0.7f, baseScale * punchScale, s);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            float settleDuration = duration - halfDuration;
            while (elapsed < settleDuration)
            {
                if (target == null) yield break;
                float t = elapsed / settleDuration;
                float bounce = Mathf.Cos(t * Mathf.PI * 1.5f) * (1f - t) * (punchScale - 1f) * 0.5f;
                target.localScale = baseScale * (1f + bounce);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (target != null) target.localScale = baseScale;
            _badgePopCoroutine = null;
        }

        private void UpdateOverlays(RaceState state)
        {
            if (readyOverlay != null) readyOverlay.SetActive(state == RaceState.Ready);
            if (stagingOverlay != null) stagingOverlay.SetActive(state == RaceState.Staging);
            if (resultOverlay != null) resultOverlay.SetActive(state == RaceState.Result);
        }
    }
}
