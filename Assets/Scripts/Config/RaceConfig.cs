using UnityEngine;

namespace Redline.Config
{
    public enum RaceDistanceMode
    {
        QuarterMile, // 402.336m
        HalfMile,    // 804.672m
        FullMile     // 1609.344m
    }

    [CreateAssetMenu(fileName = "RaceConfig", menuName = "Redline/Race Config")]
    public class RaceConfig : ScriptableObject
    {
        [Header("Track Configuration")]
        [Tooltip("Standard quarter-mile distance in meters")]
        public float quarterMileDistance = 402.336f;
        [Tooltip("Half-mile distance in meters")]
        public float halfMileDistance = 804.672f;
        [Tooltip("One-mile distance in meters")]
        public float oneMileDistance = 1609.344f;
        [Tooltip("Target race finish distance (default: quarter mile)")]
        public float raceFinishDistance = 402.336f;

        public float GetDistanceForMode(RaceDistanceMode mode)
        {
            switch (mode)
            {
                case RaceDistanceMode.HalfMile: return halfMileDistance;
                case RaceDistanceMode.FullMile: return oneMileDistance;
                default: return quarterMileDistance;
            }
        }

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

        [Header("Control Scheme - Game Off 2026 One-Button Mode")]
        [Tooltip("When true, the superbike applies full wide-open throttle automatically during the race, making the game a pure one-button experience where the SHIFT key is your only control!")]
        public bool autoThrottleDuringRace = true;

        [Header("Opponent Baseline")]
        [Tooltip("Opponent baseline target speed in m/s (66 m/s is ~238 km/h for a 10.1s quarter-mile)")]
        public float opponentTargetSpeed = 66f;
        [Tooltip("Opponent acceleration in m/s^2 (8.2 m/s^2 gives a competitive ~10.1s quarter-mile)")]
        public float opponentAcceleration = 8.2f;

        public static RaceConfig GetDefaultConfig()
        {
#if UNITY_EDITOR
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<RaceConfig>("Assets/DefaultRaceConfig.asset");
            if (cfg != null) return cfg;
            cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<RaceConfig>("Assets/ScriptableObjects/DefaultRaceConfig.asset");
            if (cfg != null) return cfg;
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RaceConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<RaceConfig>(path);
            }
#endif
            var all = Resources.FindObjectsOfTypeAll<RaceConfig>();
            if (all != null && all.Length > 0) return all[0];
            return null;
        }
    }
}
