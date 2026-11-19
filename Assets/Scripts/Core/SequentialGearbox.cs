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

        public void HandleUpshift()
        {
            if (engine == null || judge == null || config == null) return;
            if (engine.CurrentGear >= 6) return; // Already in top gear

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
            OnUpshifted?.Invoke(nextGear, grade);
        }

        public void HandleDownshift()
        {
            // Optional recovery/test downshift
            if (engine == null || engine.CurrentGear <= 1) return;
            int prevGear = engine.CurrentGear - 1;
            float bumpedRpm = Mathf.Min(engine.CurrentRpm + 2500f, config.redlineRpm);
            engine.ApplyShift(prevGear, bumpedRpm);
        }
    }
}
