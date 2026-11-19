using UnityEngine;

namespace Redline.Core
{
    public class RacerVisualSync : MonoBehaviour
    {
        [Header("Models to Advance")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform opponentRoot;

        [Header("Simulation References")]
        [SerializeField] private BikeEngineModel playerEngine;
        [SerializeField] private OpponentController opponent;

        [Header("Lane Offsets")]
        [SerializeField] private float playerLaneX = -2.5f;
        [SerializeField] private float opponentLaneX = 2.5f;

        private void Update()
        {
            if (playerRoot != null && playerEngine != null)
            {
                playerRoot.position = new Vector3(playerLaneX, playerRoot.position.y, playerEngine.DistanceTraveled);
            }

            if (opponentRoot != null && opponentRoot != null)
            {
                opponentRoot.position = new Vector3(opponentLaneX, opponentRoot.position.y, opponent.DistanceTraveled);
            }
        }
    }
}
