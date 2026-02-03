using UnityEngine;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private Bonfire ownerBonfire;

        private void Reset()
        {
            // Ensure trigger
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerState>();
            if (player == null) return;

            player.SetSafeZone(true);

            if (ownerBonfire != null)
                ownerBonfire.TryActivate(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerState>();
            if (player == null) return;

            player.SetSafeZone(false);
        }
    }
}
