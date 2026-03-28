using UnityEngine;
using DarkMazeItems;

namespace DarkMazeUI
{
    public class InventoryBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private InventorySlotUI[] slotUIs;

        [Header("Options")]
        [SerializeField] private bool refreshEveryFrame = true;

        private void Start()
        {
            if (inventory == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    inventory = player.GetComponent<PlayerInventory>();
            }

            Refresh();
        }

        private void Update()
        {
            if (refreshEveryFrame)
                Refresh();
        }

        public void Refresh()
        {
            if (slotUIs == null || slotUIs.Length == 0)
                return;

            if (inventory == null)
            {
                for (int i = 0; i < slotUIs.Length; i++)
                {
                    if (slotUIs[i] != null)
                        slotUIs[i].SetData("No Inventory", 0, false, true);
                }
                return;
            }

            var slots = inventory.GetAllSlots();
            int currentIndex = inventory.CurrentIndex;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] == null) continue;

                bool hasSlot = slots != null && i < slots.Count && slots[i] != null && slots[i].item != null;

                if (!hasSlot)
                {
                    slotUIs[i].SetData("Empty", 0, false, true);
                    continue;
                }

                var slot = slots[i];
                bool selected = i == currentIndex;

                slotUIs[i].SetData(
                    slot.item.displayName,
                    slot.count,
                    selected,
                    false
                );
            }
        }
    }
}