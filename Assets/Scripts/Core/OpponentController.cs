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

        public void UpdateOpponent(float deltaTime)
        {
            if (!IsRacing || config == null) return;

            // Smooth deterministic acceleration to target speed
            CurrentSpeedMs = Mathf.MoveTowards(CurrentSpeedMs, config.opponentTargetSpeed, config.opponentAcceleration * deltaTime);
            DistanceTraveled += CurrentSpeedMs * deltaTime;
        }
    }
}
