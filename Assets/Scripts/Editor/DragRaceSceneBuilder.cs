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

            // 4. Environment (2000m Track, Dashes, Poles, 1/4 Mile, 1/2 Mile, 1 Mile Gantries)
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
            EditorUtility.DisplayDialog("Redline Setup Complete", 
                "The 2000m Drag Race Scene is built with:\n" +
                "- 1/4 Mile Gantry & Ground Stripes (402m)\n" +
                "- 1/2 Mile Gantry & Ground Stripes (804m)\n" +
                "- 1 Mile Finish Gantry & Ground Stripes (1609m)\n" +
                "- Center dashed stripes & speed light poles\n" +
                "- Full mouse, keyboard, and gamepad throttle support!\n\n" +
                "Hit Play to race!", "Awesome!");
        }

        private static RaceConfig EnsureRaceConfigAsset()
        {
            // Prefer Assets/DefaultRaceConfig.asset first
            string primaryPath = "Assets/DefaultRaceConfig.asset";
            RaceConfig config = AssetDatabase.LoadAssetAtPath<RaceConfig>(primaryPath);

            if (config == null)
            {
                string dir = "Assets/ScriptableObjects";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }

                string assetPath = $"{dir}/DefaultRaceConfig.asset";
                config = AssetDatabase.LoadAssetAtPath<RaceConfig>(assetPath);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<RaceConfig>();
                    AssetDatabase.CreateAsset(config, assetPath);
                }
            }

            config.quarterMileDistance = 402.336f;
            config.halfMileDistance = 804.672f;
            config.oneMileDistance = 1609.344f;
            config.raceFinishDistance = 402.336f; // Quarter-mile target

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
            config.opponentTargetSpeed = 70f; // competitive rival
            config.opponentAcceleration = 17.5f;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void CreateLighting()
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.25f;
            lightGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        private static void CreateEnvironment(RaceConfig config)
        {
            GameObject envRoot = new GameObject("[ENVIRONMENT]");

            float trackTotalLength = 2200f; // over 1.3 miles
            float trackHalfLength = trackTotalLength * 0.5f;

            // Shared materials
            Material asphaltMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            asphaltMat.color = new Color(0.12f, 0.12f, 0.14f);

            Material whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            whiteMat.color = Color.white;

            Material redMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            redMat.color = new Color(0.85f, 0.15f, 0.15f);

            Material yellowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            yellowMat.color = new Color(1f, 0.88f, 0.1f);

            Material cyanMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            cyanMat.color = new Color(0.1f, 0.85f, 1f);

            Material goldMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            goldMat.color = new Color(1f, 0.75f, 0.05f);

            Material metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            metalMat.color = new Color(0.35f, 0.35f, 0.38f);

            // 1. Asphalt Main Strip
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = "DragStrip_Track";
            track.transform.parent = envRoot.transform;
            track.transform.position = new Vector3(0f, -0.05f, trackHalfLength);
            track.transform.localScale = new Vector3(13f, 0.1f, trackTotalLength);
            track.GetComponent<Renderer>().sharedMaterial = asphaltMat;

            // 2. Dashed Center Stripes (provides immediate high-speed optical motion!)
            GameObject centerStripesRoot = new GameObject("Center_Dashed_Lines");
            centerStripesRoot.transform.parent = envRoot.transform;
            for (float z = 0f; z < trackTotalLength; z += 10f)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = $"Dash_{z}";
                dash.transform.parent = centerStripesRoot.transform;
                dash.transform.position = new Vector3(0f, 0.015f, z + 2.5f);
                dash.transform.localScale = new Vector3(0.3f, 0.02f, 5f);
                dash.GetComponent<Renderer>().sharedMaterial = whiteMat;
            }

            // 3. Alternating Red & White Curbs along both barriers
            GameObject curbsRoot = new GameObject("Side_Rumble_Curbs");
            curbsRoot.transform.parent = envRoot.transform;
            for (float z = 0f; z < trackTotalLength; z += 10f)
            {
                Material curbMat = ((int)(z / 10f) % 2 == 0) ? redMat : whiteMat;

                // Left curb
                GameObject lCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lCurb.name = $"LCurb_{z}";
                lCurb.transform.parent = curbsRoot.transform;
                lCurb.transform.position = new Vector3(-6.2f, 0.25f, z + 5f);
                lCurb.transform.localScale = new Vector3(0.5f, 0.5f, 10f);
                lCurb.GetComponent<Renderer>().sharedMaterial = curbMat;

                // Right curb
                GameObject rCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rCurb.name = $"RCurb_{z}";
                rCurb.transform.parent = curbsRoot.transform;
                rCurb.transform.position = new Vector3(6.2f, 0.25f, z + 5f);
                rCurb.transform.localScale = new Vector3(0.5f, 0.5f, 10f);
                rCurb.GetComponent<Renderer>().sharedMaterial = curbMat;
            }

            // 4. Light Poles every 40m
            GameObject lightPolesRoot = new GameObject("Light_Poles");
            lightPolesRoot.transform.parent = envRoot.transform;
            for (float z = 20f; z < trackTotalLength; z += 40f)
            {
                CreateLightPole(lightPolesRoot.transform, -6.8f, z, metalMat, yellowMat, isLeft: true);
                CreateLightPole(lightPolesRoot.transform, 6.8f, z, metalMat, yellowMat, isLeft: false);
            }

            // 5. Distance Boards every 100m
            GameObject boardsRoot = new GameObject("Distance_Boards");
            boardsRoot.transform.parent = envRoot.transform;
            for (float z = 100f; z < trackTotalLength; z += 100f)
            {
                // Skip exact gantry positions
                if (Mathf.Abs(z - 400f) < 20f || Mathf.Abs(z - 800f) < 20f || Mathf.Abs(z - 1600f) < 20f) continue;
                CreateDistanceBoard(boardsRoot.transform, -6.7f, z, $"{Mathf.RoundToInt(z)}M");
            }

            // 6. THREE MAJOR GANTRIES REQUESTED BY USER:
            // --- 1/4 MILE (402.336m) ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_QuarterMile", config.quarterMileDistance, 
                "1/4 MILE - 402M", yellowMat, whiteMat, Color.yellow);

            // --- 1/2 MILE (804.672m) ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_HalfMile", config.halfMileDistance, 
                "1/2 MILE - 804M", cyanMat, whiteMat, Color.cyan);

            // --- 1 MILE (1609.344m) ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_OneMile", config.oneMileDistance, 
                "1 MILE FINISH - 1609M", goldMat, redMat, new Color(1f, 0.85f, 0.1f));
        }

        private static void CreateMilestoneGantry(Transform parent, string name, float zPos, string labelText, Material gantryMat, Material stripeMat, Color textColor)
        {
            GameObject gantryRoot = new GameObject(name);
            gantryRoot.transform.parent = parent;
            gantryRoot.transform.position = new Vector3(0f, 0f, zPos);

            // Left Pillar
            GameObject leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPillar.name = "Left_Pillar";
            leftPillar.transform.parent = gantryRoot.transform;
            leftPillar.transform.localPosition = new Vector3(-6.6f, 3.8f, 0f);
            leftPillar.transform.localScale = new Vector3(0.55f, 3.8f, 0.55f);
            leftPillar.GetComponent<Renderer>().sharedMaterial = gantryMat;

            // Right Pillar
            GameObject rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPillar.name = "Right_Pillar";
            rightPillar.transform.parent = gantryRoot.transform;
            rightPillar.transform.localPosition = new Vector3(6.6f, 3.8f, 0f);
            rightPillar.transform.localScale = new Vector3(0.55f, 3.8f, 0.55f);
            rightPillar.GetComponent<Renderer>().sharedMaterial = gantryMat;

            // Overhead Beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Overhead_Beam";
            beam.transform.parent = gantryRoot.transform;
            beam.transform.localPosition = new Vector3(0f, 7.3f, 0f);
            beam.transform.localScale = new Vector3(14.5f, 1.2f, 0.7f);
            beam.GetComponent<Renderer>().sharedMaterial = gantryMat;

            // Signboard
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign_Board";
            sign.transform.parent = gantryRoot.transform;
            sign.transform.localPosition = new Vector3(0f, 6.2f, -0.15f);
            sign.transform.localScale = new Vector3(8.5f, 1.6f, 0.25f);
            sign.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.08f, 0.08f, 0.1f)
            };

            // 3D Text Label
            GameObject textGo = new GameObject("Gantry_Label");
            textGo.transform.parent = sign.transform;
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.65f);
            textGo.transform.localRotation = Quaternion.identity;
            textGo.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            TextMesh tm = textGo.AddComponent<TextMesh>();
            tm.text = labelText;
            tm.characterSize = 1.6f;
            tm.fontSize = 54;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = textColor;

            // Ground Crossing Checkered Stripes across the track
            for (int i = 0; i < 6; i++)
            {
                GameObject groundStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundStripe.name = $"CheckeredStripe_{i}";
                groundStripe.transform.parent = gantryRoot.transform;
                float xOffset = -5f + (i * 2f);
                groundStripe.transform.localPosition = new Vector3(xOffset, 0.02f, 0f);
                groundStripe.transform.localScale = new Vector3(1.6f, 0.02f, 3.5f);
                groundStripe.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? gantryMat : stripeMat;
            }
        }

        private static void CreateLightPole(Transform parent, float xPos, float zPos, Material poleMat, Material lightMat, bool isLeft)
        {
            GameObject pole = new GameObject($"Pole_{zPos}_{(isLeft ? "L" : "R")}");
            pole.transform.parent = parent;
            pole.transform.position = new Vector3(xPos, 0f, zPos);

            // Vertical pole
            GameObject upright = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upright.transform.parent = pole.transform;
            upright.transform.localPosition = new Vector3(0f, 3f, 0f);
            upright.transform.localScale = new Vector3(0.2f, 3f, 0.2f);
            upright.GetComponent<Renderer>().sharedMaterial = poleMat;

            // Arm extending toward track
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.parent = pole.transform;
            float armOffset = isLeft ? 1.0f : -1.0f;
            arm.transform.localPosition = new Vector3(armOffset, 5.8f, 0f);
            arm.transform.localScale = new Vector3(2.0f, 0.15f, 0.15f);
            arm.GetComponent<Renderer>().sharedMaterial = poleMat;

            // Light fixture head
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.transform.parent = pole.transform;
            float lampOffset = isLeft ? 1.8f : -1.8f;
            lamp.transform.localPosition = new Vector3(lampOffset, 5.7f, 0f);
            lamp.transform.localScale = new Vector3(0.5f, 0.15f, 0.35f);
            lamp.GetComponent<Renderer>().sharedMaterial = lightMat;
        }

        private static void CreateDistanceBoard(Transform parent, float xPos, float zPos, string text)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = $"Sign_{text}";
            board.transform.parent = parent;
            board.transform.position = new Vector3(xPos, 1.2f, zPos);
            board.transform.localScale = new Vector3(0.15f, 0.8f, 1.4f);
            board.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.15f, 0.15f, 0.18f)
            };

            GameObject textGo = new GameObject("Text");
            textGo.transform.parent = board.transform;
            textGo.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            textGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            textGo.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            TextMesh tm = textGo.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        private static (Transform playerRoot, Transform opponentRoot) CreateRacers()
        {
            GameObject racersRoot = new GameObject("[RACERS]");

            // 1. Player Bike Root (Lane X: -1.8m)
            GameObject player = new GameObject("Player_Bike_Root");
            player.transform.parent = racersRoot.transform;
            player.transform.position = new Vector3(-1.8f, 0f, 0f);

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

            // 2. Opponent Bike Root (Lane X: 1.8m, Z: 0.5m so visible right beside you)
            GameObject opponent = new GameObject("Opponent_Bike_Root");
            opponent.transform.parent = racersRoot.transform;
            opponent.transform.position = new Vector3(1.8f, 0f, 0.5f);

            // Opponent Chassis
            GameObject opponentBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            opponentBody.name = "Opponent_Chassis";
            opponentBody.transform.parent = opponent.transform;
            opponentBody.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            opponentBody.transform.localScale = new Vector3(0.55f, 0.7f, 1.8f);
            var oppRend = opponentBody.GetComponent<Renderer>();
            oppRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            oppRend.sharedMaterial.color = new Color(0.95f, 0.2f, 0.05f); // Bright racing red

            // Opponent Wheels
            GameObject fWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fWheel.name = "Front_Wheel";
            fWheel.transform.parent = opponent.transform;
            fWheel.transform.localPosition = new Vector3(0f, 0.35f, 0.8f);
            fWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            fWheel.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
            fWheel.GetComponent<Renderer>().sharedMaterial.color = Color.black;

            GameObject rWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rWheel.name = "Rear_Wheel";
            rWheel.transform.parent = opponent.transform;
            rWheel.transform.localPosition = new Vector3(0f, 0.35f, -0.8f);
            rWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rWheel.transform.localScale = new Vector3(0.7f, 0.22f, 0.7f);
            rWheel.GetComponent<Renderer>().sharedMaterial.color = Color.black;

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
            soSync.FindProperty("playerLaneX").floatValue = -1.8f;
            soSync.FindProperty("opponentLaneX").floatValue = 1.8f;
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
            gzImg.raycastTarget = false;

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
                di.raycastTarget = false;
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
            readyGo.GetComponent<Image>().raycastTarget = false;
            CreateTMP(readyGo.transform, "Title", "REDLINE QUARTER MILE", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 90f), 68, TextAlignmentOptions.Center, new Color(1f, 0.25f, 0.25f));
            CreateTMP(readyGo.transform, "Hint", "HOLD SPACE, W, OR LEFT-CLICK TO THROTTLE\nPRESS SHIFT, E, OR RIGHT-CLICK TO UPSHIFT\n\n<b>PRESS SPACE OR CLICK TO STAGE</b>", new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 160f), 30, TextAlignmentOptions.Center, Color.white);

            // Staging Overlay
            GameObject stagingGo = CreateUIPanel(canvasGo.transform, "Staging_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stagingGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            stagingGo.GetComponent<Image>().raycastTarget = false;
            TextMeshProUGUI stagingTimerTxt = CreateTMP(stagingGo.transform, "Countdown_Text", "3", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(400f, 180f), 140, TextAlignmentOptions.Center, new Color(1f, 0.2f, 0.2f));
            stagingGo.SetActive(false);

            // Result Overlay
            GameObject resultGo = CreateUIPanel(canvasGo.transform, "Result_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            resultGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            resultGo.GetComponent<Image>().raycastTarget = false;
            TextMeshProUGUI resultTitleTxt = CreateTMP(resultGo.transform, "Result_Title", "VICTORY!", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 100f), 84, TextAlignmentOptions.Center, Color.green);
            TextMeshProUGUI resultDetailsTxt = CreateTMP(resultGo.transform, "Result_Details", "Time: 9.82s  |  Clean Shifts: 5/5", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 80f), 34, TextAlignmentOptions.Center, Color.white);
            CreateTMP(resultGo.transform, "Restart_Hint", "PRESS SPACE OR CLICK TO REPLAY", new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(500f, 50f), 28, TextAlignmentOptions.Center, Color.yellow);
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

            // Load and automatically assign sprites if present
            Sprite badgePerfect = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Perfect");
            Sprite badgeEarly = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Early");
            Sprite badgeLate = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_Late");
            Sprite badgeShiftNow = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Badge_ShiftNow");

            Sprite dotEmpty = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States empty");
            Sprite dotGreen = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States green");
            Sprite dotRed = LoadSubSprite("Assets/Sprites/signals.png", "Individual Shift Dot States red");

            Sprite speedFrame = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Speedometer_Frame");
            Sprite gearFrame = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Gear_Frame");
            Sprite dotsFrame = LoadSubSprite("Assets/Sprites/UI_sprites.png", "PerfectShifts_Bar");
            Sprite startTree = LoadSubSprite("Assets/Sprites/UI_sprites.png", "StartLights_Tree");

            // Create Badge Display Image in Center Screen
            GameObject badgeGo = new GameObject("Shift_Badge_Display", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bRect = badgeGo.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.anchoredPosition = new Vector2(0f, 60f);
            bRect.sizeDelta = new Vector2(340f, 130f);
            Image badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.preserveAspect = true;
            badgeImg.raycastTarget = false;
            badgeGo.SetActive(false);

            soHud.FindProperty("badgeDisplayImage").objectReferenceValue = badgeImg;
            soHud.FindProperty("badgePerfectSprite").objectReferenceValue = badgePerfect;
            soHud.FindProperty("badgeEarlySprite").objectReferenceValue = badgeEarly;
            soHud.FindProperty("badgeLateSprite").objectReferenceValue = badgeLate;
            soHud.FindProperty("badgeShiftNowSprite").objectReferenceValue = badgeShiftNow;
            soHud.FindProperty("dotEmptySprite").objectReferenceValue = dotEmpty;
            soHud.FindProperty("dotGreenSprite").objectReferenceValue = dotGreen;
            soHud.FindProperty("dotRedSprite").objectReferenceValue = dotRed;

            // Frame backgrounds if sprites exist
            if (speedFrame != null)
            {
                GameObject sFrameGo = new GameObject("Speed_Frame_Bg", typeof(RectTransform), typeof(Image));
                sFrameGo.transform.SetParent(speedTxt.transform, false);
                sFrameGo.transform.SetAsFirstSibling();
                RectTransform sfr = sFrameGo.GetComponent<RectTransform>();
                sfr.anchorMin = Vector2.zero; sfr.anchorMax = Vector2.one; sfr.sizeDelta = new Vector2(30f, 20f);
                Image sfi = sFrameGo.GetComponent<Image>();
                sfi.sprite = speedFrame; sfi.raycastTarget = false;
            }

            if (gearFrame != null)
            {
                GameObject gFrameGo = new GameObject("Gear_Frame_Bg", typeof(RectTransform), typeof(Image));
                gFrameGo.transform.SetParent(gearTxt.transform, false);
                gFrameGo.transform.SetAsFirstSibling();
                RectTransform gfr = gFrameGo.GetComponent<RectTransform>();
                gfr.anchorMin = Vector2.zero; gfr.anchorMax = Vector2.one; gfr.sizeDelta = new Vector2(25f, 25f);
                Image gfi = gFrameGo.GetComponent<Image>();
                gfi.sprite = gearFrame; gfi.raycastTarget = false;
            }

            if (startTree != null)
            {
                GameObject treeGo = new GameObject("Christmas_Tree_Icon", typeof(RectTransform), typeof(Image));
                treeGo.transform.SetParent(stagingGo.transform, false);
                RectTransform tr = treeGo.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0.5f, 0.5f); tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = new Vector2(0f, 190f); tr.sizeDelta = new Vector2(160f, 230f);
                Image ti = treeGo.GetComponent<Image>();
                ti.sprite = startTree; ti.preserveAspect = true; ti.raycastTarget = false;
            }

            var dotsProp = soHud.FindProperty("shiftIndicators");
            dotsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            }

            soHud.ApplyModifiedProperties();
        }

        private static Sprite LoadSubSprite(string texturePath, string spriteName)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            if (objects == null) return null;
            foreach (var obj in objects)
            {
                if (obj is Sprite s && s.name == spriteName)
                {
                    return s;
                }
            }
            return null;
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
            img.raycastTarget = false;
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
            bgImg.raycastTarget = false;

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
            fillImg.raycastTarget = false;

            slider.fillRect = fRect;
            slider.targetGraphic = fillImg;

            return sliderGo;
        }

        private static TMP_FontAsset _cachedDefaultFont;

        private static TMP_FontAsset GetDefaultTMPFont()
        {
            if (_cachedDefaultFont != null) return _cachedDefaultFont;

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedDefaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }

            if (_cachedDefaultFont == null)
            {
                _cachedDefaultFont = TMP_Settings.defaultFontAsset;
            }

            return _cachedDefaultFont;
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

            var font = GetDefaultTMPFont();
            if (font != null) tmp.font = font;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
