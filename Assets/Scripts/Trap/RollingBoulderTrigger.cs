using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class RollingBoulderTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RollingBoulderTrap trap;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (trap == null)
                trap = GetComponentInParent<RollingBoulderTrap>();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerState player = other.GetComponentInParent<PlayerState>();
            if (player == null)
                return;

            if (player.IsDead)
                return;

            if (trap == null)
            {
                Debug.LogWarning("[RollingBoulderTrigger] Trap reference is missing.", this);
                return;
            }

            Log($"Player triggered trap via {other.name}");

            trap.TriggerTrap();
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RollingBoulderTrigger] {message}", this);
        }
    }
}