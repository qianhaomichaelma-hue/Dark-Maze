using UnityEngine;

namespace DarkMazeMinimal
{
    public class SimpleKeyPickup : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logPickup = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            SimpleKeyHolder keyHolder = other.GetComponentInParent<SimpleKeyHolder>();
            if (keyHolder == null) return;

            if (keyHolder.HasKey) return;

            keyHolder.GiveKey();

            if (logPickup)
                Debug.Log($"[SimpleKeyPickup] Picked up by {other.name}", this);

            Destroy(gameObject);
        }
    }
}
