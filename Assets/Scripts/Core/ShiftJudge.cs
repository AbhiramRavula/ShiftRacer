using System;
using UnityEngine;
using Redline.Config;

namespace Redline.Core
{
    public class ShiftJudge : MonoBehaviour
    {
        [SerializeField] private RaceConfig config;

        public int CleanShiftsCount { get; private set; }
        public int TotalUpshiftsCompleted { get; private set; }
        public bool IsRunClean { get; private set; } = true;

        public event Action<ShiftGrade, int> OnShiftGraded; // grade, fromGear

        public void SetConfig(RaceConfig newConfig) => config = newConfig;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
                if (config != null) UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Awake()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
            }
        }

        private void Awake()
        {
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

        public void ResetJudge()
        {
            CleanShiftsCount = 0;
            TotalUpshiftsCompleted = 0;
            IsRunClean = true;
        }

        public ShiftGrade EvaluateShift(float rpmAtShift, int fromGear)
        {
            if (config == null) return ShiftGrade.None;

            ShiftGrade grade;

            if (rpmAtShift >= config.perfectWindowMin && rpmAtShift <= config.perfectWindowMax)
            {
                grade = ShiftGrade.Perfect;
                CleanShiftsCount++;
            }
            else if (rpmAtShift < config.perfectWindowMin)
            {
                grade = ShiftGrade.Early;
                IsRunClean = false;
            }
            else
            {
                grade = ShiftGrade.Late;
                IsRunClean = false;
            }

            TotalUpshiftsCompleted++;
            OnShiftGraded?.Invoke(grade, fromGear);
            return grade;
        }
    }
}
