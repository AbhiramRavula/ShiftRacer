using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Redline.Config;
using Redline.Core;
using Redline.UI;
using Redline.Audio;

namespace Redline.EditorTools
{
    public static class DragRaceSceneBuilder
    {
        [MenuItem("Redline/Setup Complete Drag Race Scene", false, 10)]
        public static void BuildCompleteScene()
        {
            // 1. Ensure ScriptableObject Config exists
            RaceConfig config = EnsureRaceConfigAsset();

            // 2. Create/Open Scene
            string scenePath = "Assets/Scenes/DragRace.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Lighting & Sky
            CreateLighting();

            // 4. Environment (Track, Barriers, Finish Line)
            CreateEnvironment(config);

            // 5. Racers (Player & Opponent)
            var (playerRoot, opponentRoot) = CreateRacers();

            // 6. Game Systems GameObject & Components
            var (session, engine, gearbox, judge, opponent, audioCtrl) = CreateGameSystems(config, playerRoot, opponentRoot);

            // 7. Canvas HUD & UI
            CreateCanvasHUD(config, session, engine, judge, opponent);

            // 8. Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=#00FF66><b>[Redline] Complete Drag Race Scene created & saved to " + scenePath + "!</b></color>");
            EditorUtility.DisplayDialog("Redline Setup Complete", "The Drag Race scene has been completely generated and wired up!\n\nOpen 'Assets/Scenes/DragRace.unity' and hit Play to test!", "Awesome!");
        }

        private static RaceConfig EnsureRaceConfigAsset()
        {
            string dir = "Assets/ScriptableObjects";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            string assetPath = $"{dir}/DefaultRaceConfig.asset";
            RaceConfig config = AssetDatabase.LoadAssetAtPath<RaceConfig>(assetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RaceConfig>();
                config.quarterMileDistance = 402.336f;
                config.idleRpm = 2800f;
                config.redlineRpm = 13000f;
                config.perfectWindowMin = 11400f;
                config.perfectWindowMax = 12600f;
                config.rpmClimbRate = 7200f;
                config.rpmFallRate = 5000f;
                config.limiterFrequency = 18f;
                config.gearTopSpeedsKmh = new float[] { 135f, 178f, 220f, 258f, 292f, 325f };
                config.gearShiftRpmDrops = new float[] { 8600f, 9300f, 9900f, 10400f, 10800f };
                config.perfectDriveEfficiency = 1.0f;
                config.earlyShiftDriveEfficiency = 0.85f;
                config.lateShiftDriveEfficiency = 0.72f;
                config.opponentTargetSpeed = 42f;
                config.opponentAcceleration = 13.5f;

                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        private static void CreateLighting()
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static void CreateEnvironment(RaceConfig config)
        {
            GameObject envRoot = new GameObject("[ENVIRONMENT]");

            // Track strip (402.336m + buffer = 520m)
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = "DragStrip_Track";
            track.transform.parent = envRoot.transform;
            track.transform.position = new Vector3(0f, -0.05f, 240f);
            track.transform.localScale = new Vector3(14f, 0.1f, 540f);

            // Track Asphalt Material Color
            var trackRenderer = track.GetComponent<Renderer>();
            trackRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            trackRenderer.sharedMaterial.color = new Color(0.12f, 0.12f, 0.13f);

            // Left & Right Barriers
            CreateBarrier(envRoot.transform, "Left_Barrier", new Vector3(-7.2f, 0.5f, 240f), new Vector3(0.4f, 1f, 540f), new Color(0.8f, 0.2f, 0.2f));
            CreateBarrier(envRoot.transform, "Right_Barrier", new Vector3(7.2f, 0.5f, 240f), new Vector3(0.4f, 1f, 540f), new Color(0.8f, 0.2f, 0.2f));

            // Center Line Marker
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Center_Divider";
            centerLine.transform.parent = envRoot.transform;
            centerLine.transform.position = new Vector3(0f, 0.01f, 240f);
            centerLine.transform.localScale = new Vector3(0.25f, 0.02f, 540f);
            var centerRenderer = centerLine.GetComponent<Renderer>();
            centerRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            centerRenderer.sharedMaterial.color = Color.white;

            // Finish Line Gantry
            GameObject finishGantry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finishGantry.name = "FinishLine_Gantry";
            finishGantry.transform.parent = envRoot.transform;
            finishGantry.transform.position = new Vector3(0f, 4f, config.quarterMileDistance);
            finishGantry.transform.localScale = new Vector3(14.5f, 0.6f, 0.6f);
            var gantryRenderer = finishGantry.GetComponent<Renderer>();
            gantryRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            gantryRenderer.sharedMaterial.color = Color.yellow;

            // Finish Line Ground Stripe
            GameObject finishStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finishStripe.name = "FinishLine_Stripe";
            finishStripe.transform.parent = envRoot.transform;
            finishStripe.transform.position = new Vector3(0f, 0.02f, config.quarterMileDistance);
            finishStripe.transform.localScale = new Vector3(14f, 0.02f, 1.5f);
            var finishRenderer = finishStripe.GetComponent<Renderer>();
            finishRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            finishRenderer.sharedMaterial.color = new Color(1f, 0.9f, 0.1f);
        }

        private static void CreateBarrier(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.parent = parent;
            barrier.transform.position = pos;
            barrier.transform.localScale = scale;
            var rend = barrier.GetComponent<Renderer>();
            rend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rend.sharedMaterial.color = color;
        }

        private static (Transform playerRoot, Transform opponentRoot) CreateRacers()
        {
            GameObject racersRoot = new GameObject("[RACERS]");

            // 1. Player Bike Root
            GameObject player = new GameObject("Player_Bike_Root");
            player.transform.parent = racersRoot.transform;
            player.transform.position = new Vector3(-2.5f, 0f, 0f);

            // First-person Main Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.transform.parent = player.transform;
            camGo.transform.localPosition = new Vector3(0f, 1.15f, 0.2f);
            camGo.transform.localRotation = Quaternion.identity;
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 68f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // Handlebar mock
            GameObject handlebar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handlebar.name = "Cockpit_Handlebars";
            handlebar.transform.parent = player.transform;
            handlebar.transform.localPosition = new Vector3(0f, 0.85f, 0.65f);
            handlebar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handlebar.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
            var hbRend = handlebar.GetComponent<Renderer>();
            hbRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            hbRend.sharedMaterial.color = new Color(0.2f, 0.2f, 0.2f);

            // 2. Opponent Bike Root
            GameObject opponent = new GameObject("Opponent_Bike_Root");
            opponent.transform.parent = racersRoot.transform;
            opponent.transform.position = new Vector3(2.5f, 0f, 0f);

            GameObject opponentBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            opponentBody.name = "Opponent_Chassis";
            opponentBody.transform.parent = opponent.transform;
            opponentBody.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            opponentBody.transform.localScale = new Vector3(0.55f, 0.8f, 2.0f);
            var oppRend = opponentBody.GetComponent<Renderer>();
            oppRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            oppRend.sharedMaterial.color = new Color(0.9f, 0.2f, 0.1f);

            return (player.transform, opponent.transform);
        }

        private static (RaceSessionController, BikeEngineModel, SequentialGearbox, ShiftJudge, OpponentController, BikeAudioController) 
            CreateGameSystems(RaceConfig config, Transform playerRoot, Transform opponentRoot)
        {
            GameObject systemsGo = new GameObject("[GAME_SYSTEMS]");

            var session = systemsGo.AddComponent<RaceSessionController>();
            var engine = systemsGo.AddComponent<BikeEngineModel>();
            var gearbox = systemsGo.AddComponent<SequentialGearbox>();
            var judge = systemsGo.AddComponent<ShiftJudge>();
            var opponent = systemsGo.AddComponent<OpponentController>();
            var visualSync = systemsGo.AddComponent<RacerVisualSync>();
            var audioCtrl = systemsGo.AddComponent<BikeAudioController>();

            // Wire up SerializedObject references
            SerializedObject soSession = new SerializedObject(session);
            soSession.FindProperty("config").objectReferenceValue = config;
            soSession.FindProperty("playerEngine").objectReferenceValue = engine;
            soSession.FindProperty("gearbox").objectReferenceValue = gearbox;
            soSession.FindProperty("judge").objectReferenceValue = judge;
            soSession.FindProperty("opponent").objectReferenceValue = opponent;
            soSession.ApplyModifiedProperties();

            SerializedObject soEngine = new SerializedObject(engine);
            soEngine.FindProperty("config").objectReferenceValue = config;
            soEngine.ApplyModifiedProperties();

            SerializedObject soGearbox = new SerializedObject(gearbox);
            soGearbox.FindProperty("config").objectReferenceValue = config;
            soGearbox.FindProperty("engine").objectReferenceValue = engine;
            soGearbox.FindProperty("judge").objectReferenceValue = judge;
            soGearbox.ApplyModifiedProperties();

            SerializedObject soJudge = new SerializedObject(judge);
            soJudge.FindProperty("config").objectReferenceValue = config;
            soJudge.ApplyModifiedProperties();

            SerializedObject soOpponent = new SerializedObject(opponent);
            soOpponent.FindProperty("config").objectReferenceValue = config;
            soOpponent.ApplyModifiedProperties();

            SerializedObject soSync = new SerializedObject(visualSync);
            soSync.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            soSync.FindProperty("opponentRoot").objectReferenceValue = opponentRoot;
            soSync.FindProperty("playerEngine").objectReferenceValue = engine;
            soSync.FindProperty("opponent").objectReferenceValue = opponent;
            soSync.ApplyModifiedProperties();

            SerializedObject soAudio = new SerializedObject(audioCtrl);
            soAudio.FindProperty("config").objectReferenceValue = config;
            soAudio.FindProperty("engine").objectReferenceValue = engine;
            soAudio.FindProperty("gearbox").objectReferenceValue = gearbox;
            soAudio.FindProperty("session").objectReferenceValue = session;
            soAudio.ApplyModifiedProperties();

            return (session, engine, gearbox, judge, opponent, audioCtrl);
        }

        private static void CreateCanvasHUD(RaceConfig config, RaceSessionController session, BikeEngineModel engine, ShiftJudge judge, OpponentController opponent)
        {
            // EventSystem
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            GameObject canvasGo = new GameObject("[CANVAS_HUD]");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<RaceHUDController>();

            // 1. Tachometer Panel (Bottom Center)
            GameObject tachoPanel = CreateUIPanel(canvasGo.transform, "Tachometer_Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 180f));
            
            // RPM Slider
            GameObject rpmSliderGo = CreateUISlider(tachoPanel.transform, "RPM_Bar", new Vector2(0f, 25f), new Vector2(820f, 40f));
            Slider rpmSlider = rpmSliderGo.GetComponent<Slider>();

            // Green Window Indicator inside RPM Bar
            GameObject greenZone = new GameObject("GreenWindow_Zone", typeof(RectTransform), typeof(Image));
            greenZone.transform.SetParent(rpmSliderGo.transform, false);
            RectTransform gzRect = greenZone.GetComponent<RectTransform>();
            gzRect.anchorMin = new Vector2(0.84f, 0f);
            gzRect.anchorMax = new Vector2(0.97f, 1f);
            gzRect.offsetMin = Vector2.zero;
            gzRect.offsetMax = Vector2.zero;
            Image gzImg = greenZone.GetComponent<Image>();
            gzImg.color = new Color(0f, 1f, 0.4f, 0.45f);

            // Speed Text
            TextMeshProUGUI speedTxt = CreateTMP(tachoPanel.transform, "Speed_Text", "0 <size=50%>KM/H</size>", new Vector2(0.5f, 0.5f), new Vector2(-220f, -30f), new Vector2(250f, 60f), 54, TextAlignmentOptions.Center, Color.white);

            // Gear Text
            TextMeshProUGUI gearTxt = CreateTMP(tachoPanel.transform, "Gear_Text", "1", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(120f, 70f), 72, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0f));

            // RPM Text
            TextMeshProUGUI rpmTxt = CreateTMP(tachoPanel.transform, "RPM_Text", "2800 RPM", new Vector2(0.5f, 0.5f), new Vector2(220f, -30f), new Vector2(250f, 60f), 48, TextAlignmentOptions.Center, Color.white);

            // 2. Shift Feedback Panel (Center Screen)
            TextMeshProUGUI calloutTxt = CreateTMP(canvasGo.transform, "ShiftGrade_Callout", "PERFECT!", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(500f, 80f), 64, TextAlignmentOptions.Center, Color.green);
            calloutTxt.gameObject.SetActive(false);

            TextMeshProUGUI limiterCue = CreateTMP(canvasGo.transform, "LimiterWarning_Cue", "SHIFT NOW!", new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(400f, 60f), 48, TextAlignmentOptions.Center, Color.red);
            limiterCue.gameObject.SetActive(false);

            // Shift Dots (5 dots)
            GameObject dotsPanel = CreateUIPanel(canvasGo.transform, "ShiftDots_Group", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(240f, 30f));
            HorizontalLayoutGroup hlg = dotsPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image[] dots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject dot = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(dotsPanel.transform, false);
                RectTransform dr = dot.GetComponent<RectTransform>();
                dr.sizeDelta = new Vector2(24f, 24f);
                Image di = dot.GetComponent<Image>();
                di.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                dots[i] = di;
            }

            // 3. Race Progress Slider (Top Screen)
            GameObject progressGo = CreateUISlider(canvasGo.transform, "RaceProgress_Bar", new Vector2(0f, -50f), new Vector2(700f, 18f));
            RectTransform prRect = progressGo.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0.5f, 1f);
            prRect.anchorMax = new Vector2(0.5f, 1f);
            prRect.pivot = new Vector2(0.5f, 1f);
            Slider progressSlider = progressGo.GetComponent<Slider>();

            TextMeshProUGUI distanceTxt = CreateTMP(canvasGo.transform, "Distance_Text", "0.0 m / 402.3 m", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 35f), 24, TextAlignmentOptions.Center, Color.white);

            // 4. Overlays
            // Ready Overlay
            GameObject readyGo = CreateUIPanel(canvasGo.transform, "Ready_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            readyGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            CreateTMP(readyGo.transform, "Title", "REDLINE QUARTER MILE", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 90f), 68, TextAlignmentOptions.Center, new Color(1f, 0.25f, 0.25f));
            CreateTMP(readyGo.transform, "Hint", "HOLD SPACE / W TO THROTTLE\nPRESS SHIFT OR E IN THE GREEN ZONE TO SHIFT\n\n<b>PRESS SPACE TO STAGE</b>", new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 160f), 32, TextAlignmentOptions.Center, Color.white);

            // Staging Overlay
            GameObject stagingGo = CreateUIPanel(canvasGo.transform, "Staging_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stagingGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
            TextMeshProUGUI stagingTimerTxt = CreateTMP(stagingGo.transform, "Countdown_Text", "3", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(300f, 180f), 140, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.1f));
            stagingGo.SetActive(false);

            // Result Overlay
            GameObject resultGo = CreateUIPanel(canvasGo.transform, "Result_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            resultGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            TextMeshProUGUI resultTitleTxt = CreateTMP(resultGo.transform, "Result_Title", "VICTORY!", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 100f), 84, TextAlignmentOptions.Center, Color.green);
            TextMeshProUGUI resultDetailsTxt = CreateTMP(resultGo.transform, "Result_Details", "Time: 9.82s  |  Clean Shifts: 5/5", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 80f), 34, TextAlignmentOptions.Center, Color.white);
            CreateTMP(resultGo.transform, "Restart_Hint", "PRESS SPACE TO REPLAY", new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(500f, 50f), 28, TextAlignmentOptions.Center, Color.yellow);
            resultGo.SetActive(false);

            // Wire up HUD SerializedObject
            SerializedObject soHud = new SerializedObject(hud);
            soHud.FindProperty("config").objectReferenceValue = config;
            soHud.FindProperty("session").objectReferenceValue = session;
            soHud.FindProperty("engine").objectReferenceValue = engine;
            soHud.FindProperty("judge").objectReferenceValue = judge;
            soHud.FindProperty("opponent").objectReferenceValue = opponent;

            soHud.FindProperty("rpmSlider").objectReferenceValue = rpmSlider;
            soHud.FindProperty("rpmText").objectReferenceValue = rpmTxt;
            soHud.FindProperty("speedText").objectReferenceValue = speedTxt;
            soHud.FindProperty("gearText").objectReferenceValue = gearTxt;

            soHud.FindProperty("shiftGradeCallout").objectReferenceValue = calloutTxt;
            soHud.FindProperty("limiterWarningCue").objectReferenceValue = limiterCue.gameObject;
            soHud.FindProperty("raceProgressSlider").objectReferenceValue = progressSlider;
            soHud.FindProperty("distanceText").objectReferenceValue = distanceTxt;

            soHud.FindProperty("readyOverlay").objectReferenceValue = readyGo;
            soHud.FindProperty("stagingOverlay").objectReferenceValue = stagingGo;
            soHud.FindProperty("stagingCountdownText").objectReferenceValue = stagingTimerTxt;
            soHud.FindProperty("resultOverlay").objectReferenceValue = resultGo;
            soHud.FindProperty("resultTitleText").objectReferenceValue = resultTitleTxt;
            soHud.FindProperty("resultDetailsText").objectReferenceValue = resultDetailsTxt;

            var dotsProp = soHud.FindProperty("shiftIndicators");
            dotsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            }

            soHud.ApplyModifiedProperties();
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (size != Vector2.zero) rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);
            return go;
        }

        private static GameObject CreateUISlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            RectTransform sRect = sliderGo.GetComponent<RectTransform>();
            sRect.anchoredPosition = anchoredPos;
            sRect.sizeDelta = size;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.2f, 0.9f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(1f, 0.35f, 0.1f);

            slider.fillRect = fRect;
            slider.targetGraphic = fillImg;

            return sliderGo;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }
    }
}
