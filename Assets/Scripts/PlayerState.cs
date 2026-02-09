using UnityEngine;
using StarterAssets;
using System.Diagnostics;

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

        /// <summary>
        /// Set whether player is in a safe zone. Logs caller + frame for debugging.
        /// </summary>
        public void SetSafeZone(bool inZone)
        {
            if (isInSafeZone == inZone) return; // avoid log spam

            isInSafeZone = inZone;

            UnityEngine.Debug.Log(
                $"[PlayerState] SetSafeZone({inZone}) | frame={Time.frameCount} | caller={GetCaller()}",
                this
            );
        }

        private string GetCaller()
        {
            // StackTrace is for debugging; keep it here only while you investigate.
            var stack = new StackTrace();
            // Frame 0: GetCaller, Frame 1: SetSafeZone, Frame 2: the caller we want
            if (stack.FrameCount > 2)
            {
                var method = stack.GetFrame(2).GetMethod();
                if (method != null && method.DeclaringType != null)
                    return $"{method.DeclaringType.Name}.{method.Name}";
            }
            return "Unknown";
        }

        public void Kill()
        {
            if (isDead) return;
            isDead = true;

            UnityEngine.Debug.Log($"[PlayerState] Killed | frame={Time.frameCount}", this);

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
                UnityEngine.Debug.LogWarning("[PlayerState] No GameManager in scene.", this);
        }

        public void ReviveAt(Transform spawnPoint)
        {
            if (spawnPoint == null) return;

            UnityEngine.Debug.Log($"[PlayerState] ReviveAt({spawnPoint.name}) | frame={Time.frameCount}", this);

            // Teleport (CharacterController needs to be disabled briefly)
            if (characterController != null) characterController.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            if (characterController != null) characterController.enabled = true;

            // Reset state
            isDead = false;

            // IMPORTANT: Don't force safe here.
            // SafeZone status should come from SafeZone trigger (and bonfire.IsLit check).
            isInSafeZone = false;

            // Re-enable movement/input
            if (starterInputs != null) starterInputs.enabled = true;
            if (thirdPersonController != null) thirdPersonController.enabled = true;
        }
    }
}

