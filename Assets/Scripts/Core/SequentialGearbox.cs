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

            float shiftRpm = engine.CurrentRpm;
            int currentGear = engine.CurrentGear;

            ShiftGrade grade = judge.EvaluateShift(shiftRpm, currentGear);

            // Apply drive penalty or boost
            switch (grade)
            {
                case ShiftGrade.Perfect:
                    engine.CurrentDriveEfficiency = config.perfectDriveEfficiency;
                    break;
                case ShiftGrade.Early:
                    engine.CurrentDriveEfficiency = config.earlyShiftDriveEfficiency;
                    break;
                case ShiftGrade.Late:
                    engine.CurrentDriveEfficiency = config.lateShiftDriveEfficiency;
                    break;
            }

            int nextGear = currentGear + 1;
            int dropIndex = Mathf.Clamp(currentGear - 1, 0, config.gearShiftRpmDrops.Length - 1);
            float postShiftRpm = config.gearShiftRpmDrops[dropIndex];

            engine.ApplyShift(nextGear, postShiftRpm);
            Debug.Log($"<color=#00FF66><b>[Redline Gearbox] Upshifted: Gear {currentGear} -> {nextGear}! RPM dropped to {postShiftRpm}. Grade: {grade}</b></color>");
            OnUpshifted?.Invoke(nextGear, grade);
        }

        public void HandleDownshift()
        {
            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (engine == null || config == null || engine.CurrentGear <= 1) return;

            int prevGear = engine.CurrentGear - 1;
            float bumpedRpm = Mathf.Min(engine.CurrentRpm + 2500f, config.redlineRpm);
            engine.ApplyShift(prevGear, bumpedRpm);
            Debug.Log($"[Redline Gearbox] Downshifted to Gear {prevGear}");
        }
    }
}
