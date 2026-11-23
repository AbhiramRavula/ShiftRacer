using UnityEngine;
using Redline.Config;

namespace Redline.Core
{
    public class OpponentController : MonoBehaviour
    {
        [SerializeField] private RaceConfig config;

        public float DistanceTraveled { get; private set; }
        public float CurrentSpeedMs { get; private set; }
        public bool IsRacing { get; private set; }

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

        public void ResetOpponent()
        {
            DistanceTraveled = 0f;
            CurrentSpeedMs = 0f;
            IsRacing = false;
        }

        public void StartRacing()
        {
            IsRacing = true;
        }

        public void StopRacing()
        {
            IsRacing = false;
        }

        public void UpdateOpponent(float deltaTime, float targetRaceDistance = 402.336f)
        {
            if (!IsRacing || config == null) return;

            float targetSpeed = config.opponentTargetSpeed;
            float accel = config.opponentAcceleration;

            // Dynamically extend for 1/2 Mile or 1 Mile strips so longer races remain competitive
            if (targetRaceDistance > 1200f) // 1 Mile
            {
                if (DistanceTraveled > 800f)
                {
                    targetSpeed = 85f; // ~306 km/h
                    accel = 3.8f;      // 6th gear top end pull
                }
                else if (DistanceTraveled > 400f)
                {
                    targetSpeed = 77f; // ~277 km/h
                    accel = 5.2f;      // 5th gear pull
                }
            }
            else if (targetRaceDistance > 600f) // 1/2 Mile
            {
                if (DistanceTraveled > 380f)
                {
                    targetSpeed = 76f; // ~274 km/h
                    accel = 5.0f;
                }
            }

            // Smooth deterministic acceleration to target speed
            CurrentSpeedMs = Mathf.MoveTowards(CurrentSpeedMs, targetSpeed, accel * deltaTime);
            DistanceTraveled += CurrentSpeedMs * deltaTime;
        }
    }
}
