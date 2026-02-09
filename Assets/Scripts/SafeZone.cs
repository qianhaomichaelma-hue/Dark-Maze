using UnityEngine;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        [SerializeField] private Bonfire ownerBonfire;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Start()
        {
            if (ownerBonfire == null)
                Debug.LogWarning($"[SafeZone] ownerBonfire is NULL on {name}", this);
            else
                Debug.Log($"[SafeZone] Start | name={name} | ownerBonfire={ownerBonfire.name} | isLit={ownerBonfire.IsLit}", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerState>();
            if (player == null)
            {
                Debug.LogWarning("[SafeZone] Player entered but PlayerState missing.", this);
                return;
            }

            string bonfireName = ownerBonfire != null ? ownerBonfire.name : "NULL";
            string lit = ownerBonfire != null ? ownerBonfire.IsLit.ToString() : "N/A";

            Debug.Log($"[SafeZone] ENTER | zone={name} | bonfire={bonfireName} | bonfireIsLit={lit}", this);

            if (ownerBonfire != null && ownerBonfire.IsLit)
            {
                player.SetSafeZone(true);
                ownerBonfire.TryActivate(player);
            }
            else
            {
                Debug.Log("[SafeZone] Not lit -> NO SAFETY granted.", this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerState>();
            if (player == null) return;

            Debug.Log($"[SafeZone] EXIT | zone={name}", this);
            player.SetSafeZone(false);
        }
    }
}
