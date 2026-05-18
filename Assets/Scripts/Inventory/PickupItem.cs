using UnityEngine;
using DarkMazePlayer;
using DarkMazeUI;

namespace DarkMazeItems
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        public ItemData item;
        public int amount = 1;

        [Header("Optional")]
        public bool autoHoldOnPickup = false;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSFX;
        [SerializeField] private float pickupVolume = 0.8f;

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

            if (item == null)
            {
                Debug.LogWarning("[PickupItem] ItemData is missing.", this);
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("[PickupItem] Amount must be greater than 0.", this);
                return;
            }

            if (inv.TryAdd(item, amount))
            {
                if (autoHoldOnPickup)
                {
                    inv.EquipItem(item);
                }

                if (ItemGainPopupUI.Instance != null)
                    ItemGainPopupUI.Instance.ShowItemGain(item.displayName, amount);

                PlayPickupSFX();

                Destroy(gameObject);
            }
        }

        private void PlayPickupSFX()
        {
            if (pickupSFX == null)
                return;

            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, pickupVolume);
        }
    }
}