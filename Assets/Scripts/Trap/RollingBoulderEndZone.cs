using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class RollingBoulderEndZone : MonoBehaviour
    {
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
        }

        private void OnTriggerEnter(Collider other)
        {
            RollingBoulder boulder = other.GetComponentInParent<RollingBoulder>();

            if (boulder == null && other.attachedRigidbody != null)
                boulder = other.attachedRigidbody.GetComponent<RollingBoulder>();

            if (boulder == null)
                return;

            Log($"Boulder reached end zone via {other.name}");

            boulder.NotifyReachedEndZone();
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RollingBoulderEndZone] {message}", this);
        }
    }
}