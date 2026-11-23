using System;
using UnityEngine;
using Redline.Config;

namespace Redline.Core
{
    public class SequentialGearbox : MonoBehaviour
    {
        [SerializeField] private RaceConfig config;
        [SerializeField] private BikeEngineModel engine;
        [SerializeField] private ShiftJudge judge;

        private float _lastShiftTime = -1f;
        private const float MIN_SHIFT_INTERVAL = 0.22f; // Minimum 220ms cooldown between shifts to prevent double-tap triggers

        public event Action<int, ShiftGrade> OnUpshifted; // newGear, grade

        public void SetConfig(RaceConfig newConfig) => config = newConfig;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
                if (config != null) UnityEditor.EditorUtility.SetDirty(this);
            }
            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (judge == null) judge = GetComponent<ShiftJudge>();
        }
#endif

        private void Awake()
        {
            // Auto-fallback if unassigned in Inspector
            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (judge == null) judge = GetComponent<ShiftJudge>();

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
        }

        public void HandleUpshift()
        {
            if (Time.time - _lastShiftTime < MIN_SHIFT_INTERVAL)
            {
                return; // Ignore duplicate input within cooldown window
            }

            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (judge == null) judge = GetComponent<ShiftJudge>();

            if (engine == null || judge == null || config == null)
            {
                Debug.LogError($"[Redline Gearbox] Cannot shift! Missing references - engine: {engine != null}, judge: {judge != null}, config: {config != null}");
                return;
            }

            if (engine.CurrentGear >= 6)
            {
                Debug.Log("[Redline Gearbox] Already at top gear (6).");
                return;
            }

            _lastShiftTime = Time.time;

            float shiftRpm = engine.CurrentRpm;
            int currentGear = engine.CurrentGear;

            ShiftGrade grade = judge.EvaluateShift(shiftRpm, currentGear);

            // Apply drive penalty or boost
            switch (grade)
            {
                case ShiftGrade.Perfect:
                    engine.CurrentDriveEfficiency = Mathf.Max(1.05f, config.perfectDriveEfficiency);
                    break;
                case ShiftGrade.Early:
                    engine.CurrentDriveEfficiency = config.earlyShiftDriveEfficiency;
                    break;
                case ShiftGrade.Late:
                    engine.CurrentDriveEfficiency = config.lateShiftDriveEfficiency;
                    break;
            }

            int nextGear = currentGear + 1;
            engine.ApplyShift(nextGear);
            Debug.Log($"<color=#00FF66><b>[Redline Gearbox] Upshifted: Gear {currentGear} -> {nextGear}! RPM: {engine.CurrentRpm:F0} at {engine.CurrentSpeedKmh:F1} km/h. Grade: {grade}</b></color>");
            OnUpshifted?.Invoke(nextGear, grade);
        }

        public void HandleDownshift()
        {
            if (Time.time - _lastShiftTime < MIN_SHIFT_INTERVAL) return;

            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (engine == null || config == null || engine.CurrentGear <= 1) return;

            _lastShiftTime = Time.time;

            int prevGear = engine.CurrentGear - 1;
            engine.ApplyShift(prevGear);
            Debug.Log($"[Redline Gearbox] Downshifted to Gear {prevGear}! RPM: {engine.CurrentRpm:F0} at {engine.CurrentSpeedKmh:F1} km/h");
        }
    }
}
