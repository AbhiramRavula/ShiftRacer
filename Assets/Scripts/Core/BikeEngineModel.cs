using UnityEngine;
using Redline.Config;

namespace Redline.Core
{
    public class BikeEngineModel : MonoBehaviour
    {
        [SerializeField] private RaceConfig config;

        public float CurrentRpm { get; private set; }
        public float CurrentSpeedKmh { get; private set; }
        public float DistanceTraveled { get; private set; }
        public int CurrentGear { get; private set; } = 1;
        public bool IsOnLimiter { get; private set; }
        public float CurrentDriveEfficiency { get; set; } = 1.0f;
        public float LaunchRpm => _clutchSlipRpm;

        private float _limiterTimer;
        private float _clutchSlipRpm;

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

        public void Initialize()
        {
            CurrentGear = 1;
            CurrentRpm = config != null ? config.idleRpm : 2800f;
            CurrentSpeedKmh = 0f;
            DistanceTraveled = 0f;
            CurrentDriveEfficiency = 1.0f;
            IsOnLimiter = false;
            _limiterTimer = 0f;
            _clutchSlipRpm = 0f;
        }

        public void UpdateEngine(bool isThrottling, float deltaTime, bool allowMovement = true)
        {
            if (config == null) return;

            // STAGING / NEUTRAL (Bike held stationary at launch line)
            if (!allowMovement)
            {
                CurrentSpeedKmh = 0f;
                if (isThrottling)
                {
                    if (CurrentRpm >= config.redlineRpm)
                    {
                        IsOnLimiter = true;
                        _limiterTimer += deltaTime * config.limiterFrequency;
                        CurrentRpm = config.redlineRpm - (Mathf.PingPong(_limiterTimer, 1f) * 350f);
                    }
                    else
                    {
                        IsOnLimiter = false;
                        CurrentRpm += config.rpmClimbRate * deltaTime;
                        if (CurrentRpm > config.redlineRpm) CurrentRpm = config.redlineRpm;
                    }
                }
                else
                {
                    IsOnLimiter = false;
                    CurrentRpm = Mathf.MoveTowards(CurrentRpm, config.idleRpm, config.rpmFallRate * deltaTime);
                }

                _clutchSlipRpm = CurrentRpm; // Store revved launch RPM for initial rollout
                return;
            }

            // RACING: REAL POWERTRAIN MOMENTUM PHYSICS
            int gearIndex = Mathf.Clamp(CurrentGear - 1, 0, config.gearTopSpeedsKmh.Length - 1);
            float gearMaxSpeed = config.gearTopSpeedsKmh[gearIndex];
            float gearRatio = config.redlineRpm / gearMaxSpeed;

            // Check if bike reached the mechanical speed limit of the current gear
            if (CurrentSpeedKmh >= gearMaxSpeed - 0.5f)
            {
                CurrentSpeedKmh = gearMaxSpeed;
                IsOnLimiter = true;
                _limiterTimer += deltaTime * config.limiterFrequency;
                CurrentRpm = config.redlineRpm - (Mathf.PingPong(_limiterTimer, 1f) * 350f);
            }
            else
            {
                IsOnLimiter = false;
            }

            // Forward Acceleration (Physical integration: speed continuously advances!)
            if (isThrottling && !IsOnLimiter)
            {
                // 1000cc Superbike base acceleration per gear (km/h per second)
                // Gear 1: explosive 0-100 in ~2.8s (~58 km/h/s)
                // Gear 2: ~44 km/h/s | Gear 3: ~34 km/h/s | Gear 4: ~26 km/h/s | Gear 5: ~20 km/h/s | Gear 6: ~15 km/h/s
                float[] baseGearAccel = { 58f, 44f, 34f, 26f, 20f, 15f };
                float baseAccel = gearIndex < baseGearAccel.Length ? baseGearAccel[gearIndex] : 15f;

                // Engine Dyno Torque Curve:
                // Low RPM (< 5,000): engine is bogged down
                // Mid RPM (5,000 - 9,500): strong pull
                // High RPM (9,500 - 12,600): PEAK POWERBAND (100% torque)
                // Overrev (> 12,600): slight drop-off before fuel cut
                float torqueFactor;
                if (CurrentRpm < 5000f)
                    torqueFactor = Mathf.Lerp(0.50f, 0.72f, CurrentRpm / 5000f);
                else if (CurrentRpm < 9500f)
                    torqueFactor = Mathf.Lerp(0.72f, 1.0f, (CurrentRpm - 5000f) / 4500f);
                else if (CurrentRpm <= config.perfectWindowMax)
                    torqueFactor = 1.0f;
                else
                    torqueFactor = 0.94f;

                // Aerodynamic drag resisting high top speeds
                float aeroDrag = 0.00015f * CurrentSpeedKmh * CurrentSpeedKmh;

                float netAccel = (baseAccel * torqueFactor * CurrentDriveEfficiency) - aeroDrag;
                CurrentSpeedKmh += Mathf.Max(0f, netAccel) * deltaTime;
                CurrentSpeedKmh = Mathf.Min(CurrentSpeedKmh, gearMaxSpeed);

                // Smoothly normalize shift efficiency modifier back to 1.0
                CurrentDriveEfficiency = Mathf.MoveTowards(CurrentDriveEfficiency, 1.0f, 0.25f * deltaTime);
            }
            else if (!isThrottling)
            {
                // Engine braking / coasting deceleration
                CurrentSpeedKmh = Mathf.MoveTowards(CurrentSpeedKmh, 0f, 18f * deltaTime);
                _clutchSlipRpm = Mathf.MoveTowards(_clutchSlipRpm, 0f, 6000f * deltaTime);
            }

            // Derive Engine RPM directly from rear-wheel road speed & gear ratio
            if (!IsOnLimiter)
            {
                float mechanicalRpm = CurrentSpeedKmh * gearRatio;

                if (CurrentGear == 1)
                {
                    // During launch, clutch slip smoothly merges launch revs into wheel drive
                    _clutchSlipRpm = Mathf.MoveTowards(_clutchSlipRpm, 0f, 4000f * deltaTime);
                    CurrentRpm = Mathf.Clamp(Mathf.Max(config.idleRpm, Mathf.Max(_clutchSlipRpm, mechanicalRpm)), config.idleRpm, config.redlineRpm);
                }
                else
                {
                    CurrentRpm = Mathf.Clamp(Mathf.Max(config.idleRpm, mechanicalRpm), config.idleRpm, config.redlineRpm);
                }
            }

            // Advance distance (km/h -> m/s)
            float speedMs = CurrentSpeedKmh / 3.6f;
            DistanceTraveled += speedMs * deltaTime;
        }

        public void ApplyShift(int newGear, float postShiftRpm = -1f)
        {
            CurrentGear = Mathf.Clamp(newGear, 1, 6);
            IsOnLimiter = false;
            _limiterTimer = 0f;
            _clutchSlipRpm = 0f;

            // Physical momentum is strictly preserved: Speed NEVER drops on gear change!
            // Engine RPM instantly recalculates according to the new gear's drive ratio.
            int gearIndex = CurrentGear - 1;
            float gearMaxSpeed = (config != null && gearIndex < config.gearTopSpeedsKmh.Length)
                ? config.gearTopSpeedsKmh[gearIndex]
                : 135f;
            float redline = config != null ? config.redlineRpm : 13000f;
            float idle = config != null ? config.idleRpm : 2800f;

            float gearRatio = redline / gearMaxSpeed;
            CurrentRpm = Mathf.Clamp(CurrentSpeedKmh * gearRatio, idle, redline);
        }
    }
}
