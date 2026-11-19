using UnityEngine;

namespace Redline.Config
{
    [CreateAssetMenu(fileName = "RaceConfig", menuName = "Redline/Race Config")]
    public class RaceConfig : ScriptableObject
    {
        [Header("Track Configuration")]
        [Tooltip("Standard quarter-mile distance in meters")]
        public float quarterMileDistance = 402.336f;

        [Header("Engine Limits")]
        public float idleRpm = 2800f;
        public float redlineRpm = 13000f;
        public float perfectWindowMin = 11400f;
        public float perfectWindowMax = 12600f;

        [Header("Engine Physics & Acceleration")]
        [Tooltip("RPM rise rate per second at full throttle")]
        public float rpmClimbRate = 7200f;
        [Tooltip("RPM drop rate per second when throttle is released")]
        public float rpmFallRate = 5000f;
        [Tooltip("Speed of limiter stutter oscillation")]
        public float limiterFrequency = 18f;

        [Header("Gear Ratios & Speeds (1 through 6)")]
        // Top speed (km/h) for gears 1 through 6 at redline:
        public float[] gearTopSpeedsKmh = new float[] { 135f, 178f, 220f, 258f, 292f, 325f };
        // Post-upshift RPM drops from 1st to 2nd, 2nd to 3rd, ..., 5th to 6th
        public float[] gearShiftRpmDrops = new float[] { 8600f, 9300f, 9900f, 10400f, 10800f };

        [Header("Shift Penalties & Multipliers")]
        public float perfectDriveEfficiency = 1.0f;
        public float earlyShiftDriveEfficiency = 0.85f;
        public float lateShiftDriveEfficiency = 0.72f;

        [Header("Opponent Baseline")]
        [Tooltip("Opponent baseline target speed in m/s (42 m/s is ~151.2 km/h average)")]
        public float opponentTargetSpeed = 42f;
        public float opponentAcceleration = 13.5f;
    }
}
