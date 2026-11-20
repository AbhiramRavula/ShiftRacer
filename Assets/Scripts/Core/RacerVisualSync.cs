using UnityEngine;

namespace Redline.Core
{
    public class RacerVisualSync : MonoBehaviour
    {
        [Header("Models to Advance")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform opponentRoot;
        [SerializeField] private Transform cameraTransform;

        [Header("Simulation References")]
        [SerializeField] private BikeEngineModel playerEngine;
        [SerializeField] private OpponentController opponent;

        [Header("Lane Offsets")]
        [SerializeField] private float playerLaneX = -1.8f;
        [SerializeField] private float opponentLaneX = 1.8f;

        private Vector3 _initialCamLocalPos;

        private void Start()
        {
            if (cameraTransform != null)
            {
                _initialCamLocalPos = cameraTransform.localPosition;
            }
            else if (playerRoot != null)
            {
                var cam = playerRoot.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cameraTransform = cam.transform;
                    _initialCamLocalPos = cameraTransform.localPosition;
                }
            }
        }

        private void Update()
        {
            if (playerRoot != null && playerEngine != null)
            {
                playerRoot.position = new Vector3(playerLaneX, playerRoot.position.y, playerEngine.DistanceTraveled);
            }

            if (opponentRoot != null && opponent != null)
            {
                opponentRoot.position = new Vector3(opponentLaneX, opponentRoot.position.y, opponent.DistanceTraveled);
            }

            // High speed / RPM camera vibration for raw speed sensation
            if (cameraTransform != null && playerEngine != null)
            {
                float speedRatio = playerEngine.CurrentSpeedKmh / 300f;
                float rpmRatio = playerEngine.CurrentRpm / 13000f;
                float shakeAmount = (speedRatio * 0.018f) + (playerEngine.IsOnLimiter ? 0.025f : 0.006f * rpmRatio);

                float shakeX = (Mathf.PerlinNoise(Time.time * 35f, 0f) - 0.5f) * shakeAmount;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 35f) - 0.5f) * shakeAmount;

                cameraTransform.localPosition = _initialCamLocalPos + new Vector3(shakeX, shakeY, 0f);
            }
        }
    }
}
