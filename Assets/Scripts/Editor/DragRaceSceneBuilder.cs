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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Redline.EditorTools
{
    public static class DragRaceSceneBuilder
    {
        [MenuItem("Redline/Setup Complete Drag Race Scene", false, 10)]
        public static void BuildCompleteScene()
        {
            BuildCompleteSceneInternal(true);
        }

        public static void BuildCompleteSceneInternal(bool showDialog = false)
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
            CreateCanvasHUD(config, session, engine, judge, opponent, audioCtrl);

            // 8. Midnight Post-Processing Volume
            CreatePostProcessing();

            // 9. Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=#00FF66><b>[Redline] Complete Drag Race Scene created & saved to " + scenePath + "!</b></color>");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Redline Setup Complete", 
                    "The 2000m Drag Race Scene is built with:\n" +
                    "- 1/4 Mile Gantry & Ground Stripes (402m)\n" +
                    "- 1/2 Mile Gantry & Ground Stripes (804m)\n" +
                    "- 1 Mile Finish Gantry & Ground Stripes (1609m)\n" +
                    "- Center dashed stripes & speed light poles\n" +
                    "- Full mouse, keyboard, and gamepad throttle support!\n\n" +
                    "Hit Play to race!", "Awesome!");
            }
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
            config.opponentTargetSpeed = 66f; // competitive ~10.1s quarter-mile rival
            config.opponentAcceleration = 8.2f;
            config.autoThrottleDuringRace = true; // One-button game mode for Game Off 2026

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static Material CreateNeonMaterial(string name, Color baseColor, float emissionMultiplier)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader)
            {
                name = name,
                color = baseColor
            };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emissionMultiplier);
            return mat;
        }

        private static void CreateLighting()
        {
            // 1. Directional Moonlight (cool midnight blue-white)
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.68f, 0.95f);
            light.intensity = 0.35f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            // 2. Procedural Midnight Skybox
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skyMat = new Material(skyShader);
                skyMat.SetFloat("_SunDisk", 0); // None
                skyMat.SetFloat("_SunSize", 0.02f);
                skyMat.SetFloat("_AtmosphereThickness", 0.45f);
                skyMat.SetColor("_SkyTint", new Color(0.015f, 0.025f, 0.065f, 1f)); // Deep midnight indigo
                skyMat.SetColor("_GroundColor", new Color(0.005f, 0.008f, 0.015f, 1f)); // Dark road
                skyMat.SetFloat("_Exposure", 0.55f);

                string matDir = "Assets/Materials";
                if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);
                string skyMatPath = $"{matDir}/MidnightSkybox.mat";
                AssetDatabase.CreateAsset(skyMat, skyMatPath);
                RenderSettings.skybox = skyMat;
            }

            // 3. Atmospheric Midnight Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.045f, 1f); // Deep midnight black/navy
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 1400f;

            // 4. Ambient Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.03f, 0.04f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.02f, 0.025f, 0.05f);
            RenderSettings.ambientGroundColor = new Color(0.005f, 0.005f, 0.01f);
        }

        private static void CreateEnvironment(RaceConfig config)
        {
            GameObject envRoot = new GameObject("[ENVIRONMENT]");

            float trackTotalLength = 2200f; // over 1.3 miles
            float trackHalfLength = trackTotalLength * 0.5f;

            // Midnight materials (matte asphalt to prevent specular headlight/lamp glare)
            Material asphaltMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            asphaltMat.color = new Color(0.07f, 0.07f, 0.09f); // Dark midnight asphalt
            if (asphaltMat.HasProperty("_Smoothness")) asphaltMat.SetFloat("_Smoothness", 0.05f);

            Material whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            whiteMat.color = new Color(0.92f, 0.92f, 0.96f);

            Material curbRedMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            curbRedMat.color = new Color(0.75f, 0.1f, 0.1f);

            Material metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            metalMat.color = new Color(0.18f, 0.18f, 0.22f); // Dark industrial matte steel

            // Balanced Neon Materials (crisp color without blinding white bloom)
            Material neonYellow = CreateNeonMaterial("Neon_Yellow", new Color(1f, 0.92f, 0.05f), 1.15f);
            Material neonCyan = CreateNeonMaterial("Neon_Cyan", new Color(0f, 0.95f, 1f), 1.15f);
            Material neonMagenta = CreateNeonMaterial("Neon_Magenta", new Color(1f, 0.05f, 0.45f), 1.2f);
            Material neonAmber = CreateNeonMaterial("Neon_Amber", new Color(1f, 0.72f, 0.2f), 1.05f);

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
                Material curbMat = ((int)(z / 10f) % 2 == 0) ? curbRedMat : whiteMat;

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

            // 4. Light Poles every 40m (clean visual poles without blinding spotlights)
            GameObject lightPolesRoot = new GameObject("Light_Poles");
            lightPolesRoot.transform.parent = envRoot.transform;
            for (float z = 20f; z < trackTotalLength; z += 40f)
            {
                CreateLightPole(lightPolesRoot.transform, -6.8f, z, metalMat, neonAmber, isLeft: true, hasSpotlight: false);
                CreateLightPole(lightPolesRoot.transform, 6.8f, z, metalMat, neonAmber, isLeft: false, hasSpotlight: false);
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

            // 6. THREE MAJOR NEON GANTRIES:
            // --- 1/4 MILE (402.336m) - Cyber Neon Yellow ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_QuarterMile", config.quarterMileDistance, 
                "1/4 MILE - 402M", metalMat, neonYellow, Color.yellow);

            // --- 1/2 MILE (804.672m) - Cyber Neon Cyan ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_HalfMile", config.halfMileDistance, 
                "1/2 MILE - 804M", metalMat, neonCyan, Color.cyan);

            // --- 1 MILE (1609.344m) - Cyber Neon Magenta / Red ---
            CreateMilestoneGantry(envRoot.transform, "Gantry_OneMile", config.oneMileDistance, 
                "1 MILE FINISH - 1609M", metalMat, neonMagenta, new Color(1f, 0.1f, 0.5f));
        }

        private static void CreateMilestoneGantry(Transform parent, string name, float zPos, string labelText, Material gantryMat, Material neonMat, Color textColor)
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

            // Neon accent beam along top of gantry
            GameObject neonTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonTop.name = "Neon_Top_Accent";
            neonTop.transform.parent = gantryRoot.transform;
            neonTop.transform.localPosition = new Vector3(0f, 7.95f, -0.3f);
            neonTop.transform.localScale = new Vector3(14.2f, 0.12f, 0.15f);
            neonTop.GetComponent<Renderer>().sharedMaterial = neonMat;

            // Neon accent beam along bottom of gantry
            GameObject neonBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonBottom.name = "Neon_Bottom_Accent";
            neonBottom.transform.parent = gantryRoot.transform;
            neonBottom.transform.localPosition = new Vector3(0f, 5.35f, -0.3f);
            neonBottom.transform.localScale = new Vector3(14.2f, 0.12f, 0.15f);
            neonBottom.GetComponent<Renderer>().sharedMaterial = neonMat;

            // Signboard
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign_Board";
            sign.transform.parent = gantryRoot.transform;
            sign.transform.localPosition = new Vector3(0f, 6.2f, -0.15f);
            sign.transform.localScale = new Vector3(8.5f, 1.6f, 0.25f);
            sign.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.04f, 0.04f, 0.06f)
            };

            // Neon Border behind signboard so dark signboard stays in front as high-contrast backing
            GameObject signBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBorder.name = "Sign_Neon_Border";
            signBorder.transform.parent = sign.transform;
            signBorder.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            signBorder.transform.localScale = new Vector3(1.06f, 1.15f, 0.8f);
            signBorder.GetComponent<Renderer>().sharedMaterial = neonMat;

            // 3D Text Label (parented to gantryRoot so it is NOT stretched by Sign_Board's non-uniform scale)
            GameObject textGo = new GameObject("Gantry_Label");
            textGo.transform.parent = gantryRoot.transform;
            textGo.transform.localPosition = new Vector3(0f, 6.2f, -0.45f);
            textGo.transform.localRotation = Quaternion.identity;
            textGo.transform.localScale = Vector3.one;
            TextMesh tm = textGo.AddComponent<TextMesh>();
            tm.text = labelText;
            tm.characterSize = 0.22f;
            tm.fontSize = 64;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            // Gantry Spotlight pointing down at the finish line
            GameObject gantryLight = new GameObject("Gantry_Downlight");
            gantryLight.transform.parent = gantryRoot.transform;
            gantryLight.transform.localPosition = new Vector3(0f, 7.0f, 0f);
            gantryLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light gl = gantryLight.AddComponent<Light>();
            gl.type = LightType.Spot;
            gl.range = 14f;
            gl.spotAngle = 95f;
            gl.innerSpotAngle = 55f;
            gl.color = textColor;
            gl.intensity = 20f;

            // Ground Crossing Checkered Stripes across the track
            Material darkGround = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.06f, 0.06f, 0.08f)
            };
            for (int i = 0; i < 6; i++)
            {
                GameObject groundStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundStripe.name = $"CheckeredStripe_{i}";
                groundStripe.transform.parent = gantryRoot.transform;
                float xOffset = -5f + (i * 2f);
                groundStripe.transform.localPosition = new Vector3(xOffset, 0.02f, 0f);
                groundStripe.transform.localScale = new Vector3(1.6f, 0.02f, 3.5f);
                groundStripe.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? neonMat : darkGround;
            }
        }

        private static void CreateLightPole(Transform parent, float xPos, float zPos, Material poleMat, Material lightMat, bool isLeft, bool hasSpotlight)
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

            if (hasSpotlight)
            {
                GameObject spotGo = new GameObject("Street_Spotlight");
                spotGo.transform.parent = lamp.transform;
                spotGo.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                spotGo.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
                Light spot = spotGo.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.range = 16f;
                spot.spotAngle = 75f;
                spot.innerSpotAngle = 40f;
                spot.color = new Color(1f, 0.82f, 0.5f);
                spot.intensity = 3.5f;
            }
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

            // Post-Processing Camera Data
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // Superbike LED Headlight cutting through the midnight fog
            GameObject headlight = new GameObject("Superbike_LED_Headlight");
            headlight.transform.parent = player.transform;
            headlight.transform.localPosition = new Vector3(0f, 0.85f, 0.6f);
            headlight.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            Light hl = headlight.AddComponent<Light>();
            hl.type = LightType.Spot;
            hl.range = 80f;
            hl.spotAngle = 60f;
            hl.innerSpotAngle = 35f;
            hl.color = new Color(0.92f, 0.96f, 1f);
            hl.intensity = 22f;

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

            // Opponent Glowing Neon Taillight
            GameObject taillight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            taillight.name = "Opponent_Taillight";
            taillight.transform.parent = opponent.transform;
            taillight.transform.localPosition = new Vector3(0f, 0.72f, -0.92f);
            taillight.transform.localScale = new Vector3(0.38f, 0.12f, 0.08f);
            taillight.GetComponent<Renderer>().sharedMaterial = CreateNeonMaterial("Neon_Taillight", new Color(1f, 0.05f, 0.05f), 4.5f);

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
            soAudio.FindProperty("engineLoopClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Engine_1000cc_Loop.wav");
            soAudio.FindProperty("shiftBarkClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quickshifter_Bark.wav");
            soAudio.FindProperty("limiterPulseClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RevLimiter_Stutter.wav");
            soAudio.FindProperty("countdownBeepClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Countdown_Beep.wav");
            soAudio.FindProperty("greenLightGoClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/GreenLight_Go.wav");
            soAudio.FindProperty("victoryFanfareClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Victory_Fanfare.wav");
            soAudio.ApplyModifiedProperties();

            return (session, engine, gearbox, judge, opponent, audioCtrl);
        }

        private static void CreateCanvasHUD(RaceConfig config, RaceSessionController session, BikeEngineModel engine, ShiftJudge judge, OpponentController opponent, BikeAudioController audioCtrl)
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<RaceHUDController>();

            // Load Rounded UI Sprites
            Sprite roundedBox = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Box.png");
            Sprite roundedBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Rounded_Border.png");
            Sprite pillBar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Pill_Bar.png");

            // 1. Sleek Tachometer Cluster Panel (Bottom Center)
            GameObject tachoPanel = CreateUIPanel(canvasGo.transform, "Tachometer_Panel", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(0f, 35f), new Vector2(860f, 155f), roundedBox);
            Image tpImg = tachoPanel.GetComponent<Image>();
            tpImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);
            
            // Top Row: RPM Pill Slider (0 to 14,000 RPM)
            GameObject rpmSliderGo = CreateUISlider(tachoPanel.transform, "RPM_Bar", new Vector2(0f, 36f), new Vector2(800f, 28f), pillBar);
            Slider rpmSlider = rpmSliderGo.GetComponent<Slider>();
            rpmSlider.minValue = 0f;
            rpmSlider.maxValue = 14000f;
            rpmSlider.value = config.idleRpm;

            // Green Window Indicator inside RPM Bar (synchronized with 11,400 to 12,600 RPM)
            float maxRpm = 14000f;
            GameObject greenZone = new GameObject("GreenWindow_Zone", typeof(RectTransform), typeof(Image));
            greenZone.transform.SetParent(rpmSliderGo.transform, false);
            RectTransform gzRect = greenZone.GetComponent<RectTransform>();
            gzRect.anchorMin = new Vector2(config.perfectWindowMin / maxRpm, 0f);
            gzRect.anchorMax = new Vector2(config.perfectWindowMax / maxRpm, 1f);
            gzRect.offsetMin = Vector2.zero;
            gzRect.offsetMax = Vector2.zero;
            Image gzImg = greenZone.GetComponent<Image>();
            if (roundedBox != null)
            {
                gzImg.sprite = roundedBox;
                gzImg.type = Image.Type.Sliced;
            }
            gzImg.color = new Color(0f, 1f, 0.45f, 0.6f);
            gzImg.raycastTarget = false;

            // Bottom Row: Speed Text, Gear Text, RPM Text
            // Left: Speed
            TextMeshProUGUI speedTxt = CreateTMP(tachoPanel.transform, "Speed_Text", "0 <size=50%>KM/H</size>", new Vector2(0.5f, 0.5f), new Vector2(-260f, -25f), new Vector2(230f, 60f), 52, TextAlignmentOptions.Center, Color.white);

            // Center: Gear Numeral
            TextMeshProUGUI gearTxt = CreateTMP(tachoPanel.transform, "Gear_Text", "1", new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(100f, 65f), 68, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.1f));

            // Right: RPM Counter
            TextMeshProUGUI rpmTxt = CreateTMP(tachoPanel.transform, "RPM_Text", "2800 RPM", new Vector2(0.5f, 0.5f), new Vector2(260f, -25f), new Vector2(230f, 60f), 46, TextAlignmentOptions.Center, Color.white);

            // 2. Shift Feedback Panel (Center Screen)
            TextMeshProUGUI calloutTxt = CreateTMP(canvasGo.transform, "ShiftGrade_Callout", "PERFECT!", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(500f, 80f), 64, TextAlignmentOptions.Center, Color.green);
            calloutTxt.gameObject.SetActive(false);

            TextMeshProUGUI limiterCue = CreateTMP(canvasGo.transform, "LimiterWarning_Cue", "SHIFT NOW!", new Vector2(0.5f, 0.5f), new Vector2(0f, 125f), new Vector2(400f, 60f), 48, TextAlignmentOptions.Center, Color.red);
            limiterCue.gameObject.SetActive(false);

            // Shift Dots (5 dots) - Docked directly on top of Tachometer_Panel
            GameObject dotsPanel = CreateUIPanel(canvasGo.transform, "ShiftDots_Group", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(0f, 198f), new Vector2(250f, 32f), roundedBox);
            dotsPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
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
            GameObject progressGo = CreateUISlider(canvasGo.transform, "RaceProgress_Bar", new Vector2(0f, -35f), new Vector2(720f, 18f), pillBar);
            RectTransform prRect = progressGo.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0.5f, 1f);
            prRect.anchorMax = new Vector2(0.5f, 1f);
            prRect.pivot = new Vector2(0.5f, 1f);
            Slider progressSlider = progressGo.GetComponent<Slider>();

            TextMeshProUGUI distanceTxt = CreateTMP(canvasGo.transform, "Distance_Text", "0.0 m / 402.3 m", new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(400f, 35f), 24, TextAlignmentOptions.Center, Color.white);

            // 4. Overlays
            // Ready Overlay
            GameObject readyGo = CreateUIPanel(canvasGo.transform, "Ready_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            readyGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            readyGo.GetComponent<Image>().raycastTarget = false;
            CreateTMP(readyGo.transform, "Title", "SHIFTRACER", new Vector2(0.5f, 0.5f), new Vector2(0f, 225f), new Vector2(800f, 65f), 58, TextAlignmentOptions.Center, new Color(1f, 0.22f, 0.22f));
            CreateTMP(readyGo.transform, "Subtitle", "REDLINE QUARTER MILE  •  GITHUB GAME OFF 2026", new Vector2(0.5f, 0.5f), new Vector2(0f, 178f), new Vector2(800f, 30f), 18, TextAlignmentOptions.Center, new Color(0.9f, 0.75f, 0.2f));
            CreateTMP(readyGo.transform, "Hint", "<color=#FFE600>WIDE-OPEN AUTO-THROTTLE ENGAGED</color>\nPRESS <color=#00F0FF><b>SHIFT KEY</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> AT THE <color=#00FF66><b>GREEN ZONE</b></color> TO SHIFT GEARS", new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(850f, 55f), 20, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1f));

            // Staging Overlay
            GameObject stagingGo = CreateUIPanel(canvasGo.transform, "Staging_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stagingGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            stagingGo.GetComponent<Image>().raycastTarget = false;
            TextMeshProUGUI stagingTimerTxt = CreateTMP(stagingGo.transform, "Countdown_Text", "3", new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(400f, 180f), 140, TextAlignmentOptions.Center, new Color(1f, 0.2f, 0.2f));
            stagingGo.SetActive(false);

            // Result Overlay (Dark frosted background with rounded result card)
            GameObject resultGo = CreateUIPanel(canvasGo.transform, "Result_Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            resultGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);
            resultGo.GetComponent<Image>().raycastTarget = false;

            GameObject resultCard = CreateUIPanel(resultGo.transform, "Result_Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 360f), roundedBox);
            resultCard.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.95f);

            // Glowing border outline on Result Card
            if (roundedBorder != null)
            {
                GameObject borderGo = new GameObject("Border_Outline", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(resultCard.transform, false);
                RectTransform brt = borderGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                Image bi = borderGo.GetComponent<Image>();
                bi.sprite = roundedBorder;
                bi.type = Image.Type.Sliced;
                bi.color = new Color(0f, 1f, 0.5f, 0.85f);
                bi.raycastTarget = false;
            }

            TextMeshProUGUI resultTitleTxt = CreateTMP(resultCard.transform, "Result_Title", "VICTORY!", new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(760f, 70f), 62, TextAlignmentOptions.Center, Color.green);
            TextMeshProUGUI resultDetailsTxt = CreateTMP(resultCard.transform, "Result_Details", "Time: 9.82s  |  Clean Shifts: 5/5", new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(760f, 105f), 22, TextAlignmentOptions.Center, Color.white);
            CreateTMP(resultCard.transform, "Restart_Hint", "PRESS <color=#00F0FF><b>SHIFT / SPACE</b></color> OR <color=#00F0FF><b>TAP SCREEN</b></color> TO RACE AGAIN", new Vector2(0.5f, 0.5f), new Vector2(0f, -125f), new Vector2(760f, 40f), 20, TextAlignmentOptions.Center, Color.yellow);
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

            Sprite cockpitOverlay = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/First-Person Superbike Cockpit Overlay.png");
            Sprite tachoDial = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tachometer_Dial.png");
            Sprite tachoNeedle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rotating Tachometer Needle.png");

            Sprite hintThrottle = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Throttle");
            Sprite hintUpshift = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Upshift");
            Sprite hintDownshift = LoadSubSprite("Assets/Sprites/UI_sprites.png", "Hint_Downshift");

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

            soHud.FindProperty("audioController").objectReferenceValue = audioCtrl;
            soHud.FindProperty("cockpitOverlaySprite").objectReferenceValue = cockpitOverlay;
            soHud.FindProperty("tachometerDialSprite").objectReferenceValue = tachoDial;
            soHud.FindProperty("tachometerNeedleSprite").objectReferenceValue = tachoNeedle;
            soHud.FindProperty("speedometerFrameSprite").objectReferenceValue = speedFrame;
            soHud.FindProperty("gearFrameSprite").objectReferenceValue = gearFrame;
            soHud.FindProperty("perfectShiftsBarSprite").objectReferenceValue = dotsFrame;
            soHud.FindProperty("startLightsTreeSprite").objectReferenceValue = startTree;
            soHud.FindProperty("hintThrottleSprite").objectReferenceValue = hintThrottle;
            soHud.FindProperty("hintUpshiftSprite").objectReferenceValue = hintUpshift;
            soHud.FindProperty("hintDownshiftSprite").objectReferenceValue = hintDownshift;

            soHud.FindProperty("roundedBoxSprite").objectReferenceValue = roundedBox;
            soHud.FindProperty("roundedBorderSprite").objectReferenceValue = roundedBorder;
            soHud.FindProperty("pillBarSprite").objectReferenceValue = pillBar;

            // Create Cockpit Overlay GameObject in scene so it is directly selectable & adjustable in Editor
            GameObject cockpitGo = new GameObject("Cockpit_Overlay", typeof(RectTransform), typeof(Image));
            cockpitGo.transform.SetParent(canvasGo.transform, false);
            cockpitGo.transform.SetAsFirstSibling();
            RectTransform crt = cockpitGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(3f, 243.5f);
            crt.sizeDelta = new Vector2(1051f, 487f);
            Image cockpitImg = cockpitGo.GetComponent<Image>();
            cockpitImg.sprite = cockpitOverlay;
            cockpitImg.color = Color.white;
            cockpitImg.raycastTarget = false;
            cockpitImg.preserveAspect = false;
            soHud.FindProperty("cockpitOverlayImage").objectReferenceValue = cockpitImg;

            // Frame backgrounds if sprites exist (must be siblings placed BEHIND text)
            if (speedFrame != null)
            {
                GameObject sFrameGo = new GameObject("Speed_Frame_Bg", typeof(RectTransform), typeof(Image));
                sFrameGo.transform.SetParent(tachoPanel.transform, false);
                int textIndex = speedTxt.transform.GetSiblingIndex();
                sFrameGo.transform.SetSiblingIndex(textIndex); // Placed before text
                RectTransform sfr = sFrameGo.GetComponent<RectTransform>();
                RectTransform str = speedTxt.GetComponent<RectTransform>();
                sfr.anchorMin = str.anchorMin; sfr.anchorMax = str.anchorMax; sfr.pivot = str.pivot;
                sfr.anchoredPosition = str.anchoredPosition;
                sfr.sizeDelta = new Vector2(str.sizeDelta.x + 30f, str.sizeDelta.y + 20f);
                Image sfi = sFrameGo.GetComponent<Image>();
                sfi.sprite = speedFrame; sfi.raycastTarget = false;
                speedTxt.transform.SetAsLastSibling();
            }

            if (gearFrame != null)
            {
                GameObject gFrameGo = new GameObject("Gear_Frame_Bg", typeof(RectTransform), typeof(Image));
                gFrameGo.transform.SetParent(tachoPanel.transform, false);
                int textIndex = gearTxt.transform.GetSiblingIndex();
                gFrameGo.transform.SetSiblingIndex(textIndex); // Placed before text
                RectTransform gfr = gFrameGo.GetComponent<RectTransform>();
                RectTransform gtr = gearTxt.GetComponent<RectTransform>();
                gfr.anchorMin = gtr.anchorMin; gfr.anchorMax = gtr.anchorMax; gfr.pivot = gtr.pivot;
                gfr.anchoredPosition = gtr.anchoredPosition;
                gfr.sizeDelta = new Vector2(gtr.sizeDelta.x + 30f, gtr.sizeDelta.y + 25f);
                Image gfi = gFrameGo.GetComponent<Image>();
                gfi.sprite = gearFrame; gfi.raycastTarget = false;
                gearTxt.transform.SetAsLastSibling();
            }

            // Trackside Christmas Tree (positioned trackside left so the strip view is clear)
            if (startTree != null)
            {
                GameObject treeGo = new GameObject("Christmas_Tree_Icon", typeof(RectTransform), typeof(Image));
                treeGo.transform.SetParent(stagingGo.transform, false);
                RectTransform tr = treeGo.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0f, 0.5f); 
                tr.anchorMax = new Vector2(0f, 0.5f);
                tr.pivot = new Vector2(0f, 0.5f);
                tr.anchoredPosition = new Vector2(100f, 20f); 
                tr.sizeDelta = new Vector2(140f, 230f);
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

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Sprite sprite = null)
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
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateUISlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Sprite barSprite = null)
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
            if (barSprite != null)
            {
                bgImg.sprite = barSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
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
            if (barSprite != null)
            {
                fillImg.sprite = barSprite;
                fillImg.type = Image.Type.Sliced;
            }
            fillImg.color = new Color(1f, 0.45f, 0.05f);
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

        private static void CreatePostProcessing()
        {
            GameObject volumeGo = new GameObject("[POST_PROCESSING]");
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string dir = "Assets/Settings";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string profilePath = $"{dir}/MidnightRace_VolumeProfile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.60f);
            bloom.intensity.Override(0.18f);
            bloom.scatter.Override(0.25f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.30f);
            vignette.smoothness.Override(0.45f);

            ColorAdjustments colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.contrast.Override(10f);
            colorAdjust.postExposure.Override(0.0f);

            volume.profile = profile;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
