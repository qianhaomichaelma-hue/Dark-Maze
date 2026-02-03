using UnityEngine;
using StarterAssets;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class PlayerState : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isInSafeZone;
        [SerializeField] private bool isDead;

        [Header("Components (auto)")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private StarterAssetsInputs starterInputs;
        [SerializeField] private CharacterController characterController;

        public bool IsInSafeZone => isInSafeZone;
        public bool IsDead => isDead;

        private void Awake()
        {
            // Auto-find common components on Starter Assets player
            if (thirdPersonController == null) thirdPersonController = GetComponent<ThirdPersonController>();
            if (starterInputs == null) starterInputs = GetComponent<StarterAssetsInputs>();
            if (characterController == null) characterController = GetComponent<CharacterController>();

            // Register into GameManager if exists
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterPlayer(this);
        }

        public void SetSafeZone(bool inZone)
        {
            isInSafeZone = inZone;
        }

        public void Kill()
        {
            if (isDead) return;
            isDead = true;

            // Disable movement/input
            if (starterInputs != null)
            {
                starterInputs.move = Vector2.zero;
                starterInputs.look = Vector2.zero;
                starterInputs.jump = false;
                starterInputs.sprint = false;
            }

            if (thirdPersonController != null) thirdPersonController.enabled = false;
            if (starterInputs != null) starterInputs.enabled = false;

            // Ask manager to respawn
            if (GameManager.Instance != null)
                GameManager.Instance.RequestRespawn();
            else
                Debug.LogWarning("[PlayerState] No GameManager in scene.");
        }

        public void ReviveAt(Transform spawnPoint)
        {
            if (spawnPoint == null) return;

            // Teleport (CharacterController needs to be disabled briefly)
            if (characterController != null) characterController.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            if (characterController != null) characterController.enabled = true;

            // Reset state
            isDead = false;
            isInSafeZone = true; // respawn considered safe

            // Re-enable movement/input
            if (starterInputs != null) starterInputs.enabled = true;
            if (thirdPersonController != null) thirdPersonController.enabled = true;
        }
    }
}
