using UnityEngine;

namespace DarkMazeMinimal
{
    public class NPCLookAtPlayer : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform player;

        [Header("Rotation")]
        [SerializeField] private bool onlyYRotation = true;
        [SerializeField] private float rotationSpeed = 8f;

        private void Start()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    player = playerGO.transform;
            }
        }

        private void LateUpdate()
        {
            if (player == null)
                return;

            Vector3 direction = player.position - transform.position;

            if (onlyYRotation)
                direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}