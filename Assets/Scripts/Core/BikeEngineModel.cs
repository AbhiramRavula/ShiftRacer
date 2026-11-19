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

        private float _limiterTimer;

        public void SetConfig(RaceConfig newConfig) => config = newConfig;

        public void Initialize()
        {
            CurrentGear = 1;
            CurrentRpm = config != null ? config.idleRpm : 2800f;
            CurrentSpeedKmh = 0f;
            DistanceTraveled = 0f;
            CurrentDriveEfficiency = 1.0f;
            IsOnLimiter = false;
            _limiterTimer = 0f;
        }

        public void UpdateEngine(bool isThrottling, float deltaTime)
        {
            if (config == null) return;

            if (isThrottling)
            {
                if (CurrentRpm >= config.redlineRpm)
                {
                    IsOnLimiter = true;
                    _limiterTimer += deltaTime * config.limiterFrequency;
                    // Limiter bounce oscillation:
                    CurrentRpm = config.redlineRpm - (Mathf.PingPong(_limiterTimer, 1f) * 350f);
                }
                else
                {
                    IsOnLimiter = false;
                    CurrentRpm += config.rpmClimbRate * CurrentDriveEfficiency * deltaTime;
                    if (CurrentRpm > config.redlineRpm)
                    {
                        CurrentRpm = config.redlineRpm;
                    }
                }
            }
            else
            {
                IsOnLimiter = false;
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, config.idleRpm, config.rpmFallRate * deltaTime);
            }

            // Calculate forward speed from gear ratio and RPM ratio
            int gearIndex = Mathf.Clamp(CurrentGear - 1, 0, config.gearTopSpeedsKmh.Length - 1);
            float gearMaxSpeed = config.gearTopSpeedsKmh[gearIndex];
            float rpmFraction = Mathf.InverseLerp(config.idleRpm, config.redlineRpm, CurrentRpm);
            
            CurrentSpeedKmh = rpmFraction * gearMaxSpeed * CurrentDriveEfficiency;

            // Advance distance (km/h to m/s)
            float speedMs = CurrentSpeedKmh / 3.6f;
            DistanceTraveled += speedMs * deltaTime;
        }

        public void ApplyShift(int newGear, float postShiftRpm)
        {
            CurrentGear = Mathf.Clamp(newGear, 1, 6);
            CurrentRpm = postShiftRpm;
            IsOnLimiter = false;
            _limiterTimer = 0f;
        }
    }
}
