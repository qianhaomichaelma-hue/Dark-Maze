using UnityEngine;
using DarkMazePlayer;

namespace DarkMazeItems
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        public ItemData item;
        public int amount = 1;

        [Header("Optional")]
        public bool autoHoldOnPickup = false;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var inv = other.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                Debug.LogWarning("[PickupItem] Player has no PlayerInventory.");
                return;
            }

            if (inv.TryAdd(item, amount))
            {
                if (autoHoldOnPickup)
                {
                    inv.EquipItem(item);
                }

                Destroy(gameObject);
            }
        }
    }
}
