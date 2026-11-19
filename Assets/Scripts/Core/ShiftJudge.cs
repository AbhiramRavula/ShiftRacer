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
