using UnityEngine;

namespace Redline.Core
{
    public class RacerVisualSync : MonoBehaviour
    {
        [Header("Models to Advance")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform opponentRoot;
        [SerializeField] private Transform cameraTransform;

        [Header("Simulation References")]
        [SerializeField] private BikeEngineModel playerEngine;
        [SerializeField] private OpponentController opponent;
        [SerializeField] private SequentialGearbox gearbox;

        [Header("Lane Offsets")]
        [SerializeField] private float playerLaneX = -1.8f;
        [SerializeField] private float opponentLaneX = 1.8f;

        private Vector3 _initialCamLocalPos;
        private Quaternion _initialCamLocalRot;
        private Camera _cam;
        private float _baseFov = 60f;
        private float _shiftFovKick;
        private float _shiftPitchKick;

        private void Start()
        {
            if (gearbox == null) gearbox = FindAnyObjectByType<SequentialGearbox>();
            if (gearbox != null) gearbox.OnUpshifted += HandleUpshifted;

            if (cameraTransform == null && playerRoot != null)
            {
                var cam = playerRoot.GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            if (cameraTransform != null)
            {
                _initialCamLocalPos = cameraTransform.localPosition;
                _initialCamLocalRot = cameraTransform.localRotation;
                _cam = cameraTransform.GetComponent<Camera>();
                if (_cam != null) _baseFov = _cam.fieldOfView;
            }

            TuneTrackBloomAndLighting();
        }

        public static void TuneTrackBloomAndLighting()
        {
            // 1. Disable blinding Bloom across all Volumes
            var volumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                var profile = vol.sharedProfile != null ? vol.sharedProfile : vol.profile;
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                    vol.profile = profile;
                }

                if (!profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
                {
                    bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                }
                // Keep active=true so this volume overrides any global URP default profile to 0 intensity
                bloom.active = true;
                bloom.intensity.Override(0f);
                bloom.threshold.Override(2.0f);

                if (profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var colorAdjust))
                {
                    colorAdjust.contrast.Override(8f);
                    colorAdjust.postExposure.Override(0.5f);
                }
            }

            // 1b. Lift ambient lighting so track, curbs, and gantries are clearly visible
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.25f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.12f);
            RenderSettings.ambientIntensity = 1.0f;

            // 2. Crisp, well-lit midnight track lighting (Bloom stays off so it's never blinding)
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                l.enabled = true;

                if (l.type == LightType.Directional)
                {
                    l.color = new Color(0.80f, 0.85f, 1.0f);
                    l.intensity = 1.4f;
                }
                else if (l.gameObject.name.Contains("Headlight") || l.gameObject.name.Contains("headlight"))
                {
                    l.intensity = 22f;
                    l.range = 75f;
                    l.spotAngle = 62f;
                    l.innerSpotAngle = 36f;
                    l.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
                }
                else if (l.gameObject.name.Contains("Gantry"))
                {
                    l.intensity = 6.5f;
                    l.range = 20f;
                }
                else if (l.gameObject.name == "Street_Spotlight")
                {
                    l.intensity = 4.5f;
                    l.range = 18f;
                }
                else
                {
                    l.intensity = Mathf.Clamp(l.intensity, 3.0f, 5.0f);
                    l.range = Mathf.Min(l.range, 20f);
                }
            }

            // 3. Remove starfield particles & switch Lit materials to Simple Lit (no PBR/specular fireflies)
            var stars = GameObject.Find("Midnight_Stars_Canopy");
            if (stars != null)
            {
                var ps = stars.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                stars.SetActive(false);
            }

            var simpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                if (mats == null) continue;
                foreach (var mat in mats)
                {
                    if (mat == null) continue;

                    if (simpleLitShader != null && mat.shader != null && mat.shader.name == "Universal Render Pipeline/Lit")
                    {
                        Color baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.color : Color.white);
                        Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                        mat.shader = simpleLitShader;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                        if (mat.HasProperty("_Color")) mat.color = baseColor;
                        if (baseMap != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
                    }

                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
                    if (mat.HasProperty("_SpecularHighlights"))
                    {
                        mat.SetFloat("_SpecularHighlights", 0f);
                        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    }
                    if (mat.HasProperty("_EnvironmentReflections"))
                    {
                        mat.SetFloat("_EnvironmentReflections", 0f);
                        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    }

                    if (mat.name.StartsWith("Neon_") || r.gameObject.name == "Cube" || (r.transform.parent != null && r.transform.parent.name.StartsWith("Pole_")))
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                            mat.SetColor("_EmissionColor", baseCol * 0.65f);
                        }
                    }
                    else if (r.gameObject.name == "DragStrip_Track")
                    {
                        Color roadCol = new Color(0.14f, 0.14f, 0.17f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", roadCol);
                        if (mat.HasProperty("_Color")) mat.color = roadCol;
                    }
                }
            }

            // 4. Fix Gantry_Label placement & Sign_Neon_Border layering so finish line text is crisp and readable
            var textMeshes = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach (var tm in textMeshes)
            {
                if (tm.gameObject.name != "Gantry_Label") continue;
                Transform sign = tm.transform.parent;
                if (sign != null && sign.name == "Sign_Board" && sign.parent != null)
                {
                    Transform border = sign.Find("Sign_Neon_Border");
                    if (border != null)
                    {
                        border.localPosition = new Vector3(0f, 0f, 0.25f);
                        border.localScale = new Vector3(1.06f, 1.15f, 0.8f);
                    }
                    tm.transform.SetParent(sign.parent, false);
                }

                tm.transform.localPosition = new Vector3(0f, 6.2f, -0.45f);
                tm.transform.localRotation = Quaternion.identity;
                tm.transform.localScale = Vector3.one;
                tm.characterSize = 0.22f;
                tm.fontSize = 64;
                tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
            }
        }

        private void OnDestroy()
        {
            if (gearbox != null) gearbox.OnUpshifted -= HandleUpshifted;
        }

        private void HandleUpshifted(int newGear, ShiftGrade grade)
        {
            // Brief speed-kick and helmet tilt on gear engagement
            _shiftFovKick = grade == ShiftGrade.Perfect ? 2.6f : 1.2f;
            _shiftPitchKick = grade == ShiftGrade.Perfect ? -1.4f : -0.6f;
        }

        private void Update()
        {
            if (playerRoot != null && playerEngine != null)
            {
                playerRoot.position = new Vector3(playerLaneX, playerRoot.position.y, playerEngine.DistanceTraveled);
            }

            if (opponentRoot != null && opponent != null)
            {
                opponentRoot.position = new Vector3(opponentLaneX, opponentRoot.position.y, opponent.DistanceTraveled);
            }

            // High speed / RPM camera vibration + dynamic speed-sense FOV ramp
            if (cameraTransform != null && playerEngine != null)
            {
                float speedRatio = playerEngine.CurrentSpeedKmh / 300f;
                float rpmRatio = playerEngine.CurrentRpm / 13000f;
                float shakeAmount = (speedRatio * 0.018f) + (playerEngine.IsOnLimiter ? 0.025f : 0.006f * rpmRatio);

                float shakeX = (Mathf.PerlinNoise(Time.time * 35f, 0f) - 0.5f) * shakeAmount;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 35f) - 0.5f) * shakeAmount;

                cameraTransform.localPosition = _initialCamLocalPos + new Vector3(shakeX, shakeY, 0f);

                _shiftFovKick = Mathf.MoveTowards(_shiftFovKick, 0f, Time.deltaTime * 10f);
                _shiftPitchKick = Mathf.MoveTowards(_shiftPitchKick, 0f, Time.deltaTime * 8f);
                cameraTransform.localRotation = _initialCamLocalRot * Quaternion.Euler(_shiftPitchKick, 0f, 0f);

                if (_cam != null)
                {
                    float speedNorm = Mathf.Clamp01(playerEngine.CurrentSpeedKmh / 325f);
                    float targetFov = _baseFov + (speedNorm * 14f) + _shiftFovKick;
                    _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * 8f);
                }
            }
        }
    }
}
