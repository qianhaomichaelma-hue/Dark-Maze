using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class RollingBoulderDamageRelay : MonoBehaviour
    {
        [SerializeField] private RollingBoulder boulder;

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

            if (boulder == null)
                boulder = GetComponentInParent<RollingBoulder>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (boulder == null)
                return;

            boulder.TryKillPlayerFromCollider(other);
        }
    }
}