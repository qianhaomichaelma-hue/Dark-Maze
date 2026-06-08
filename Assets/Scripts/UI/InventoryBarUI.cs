using UnityEngine;
using DarkMazeItems;

namespace DarkMazeUI
{
    public class InventoryBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private GameObject barRoot;
        [SerializeField] private InventorySlotUI[] slotUIs;

        [Header("Display Options")]
        [SerializeField] private bool showItemNames = true;
        [SerializeField] private bool showItemIcons = true;

        [Header("Options")]
        [SerializeField] private bool refreshEveryFrame = true;
        [SerializeField] private bool hideWhenInventoryEmpty = true;
        [SerializeField] private bool hideEmptySlots = true;

        private void Start()
        {
            if (inventory == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    inventory = player.GetComponent<PlayerInventory>();
            }

            if (barRoot == null)
                barRoot = gameObject;

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

            bool hasInventory = inventory != null;
            bool hasItems = hasInventory && inventory.SlotCount > 0;

            if (barRoot != null && hideWhenInventoryEmpty)
                barRoot.SetActive(hasItems);

            if (!hasInventory)
            {
                for (int i = 0; i < slotUIs.Length; i++)
                {
                    if (slotUIs[i] == null)
                        continue;

                    if (hideEmptySlots)
                        slotUIs[i].gameObject.SetActive(false);
                    else
                    {
                        slotUIs[i].gameObject.SetActive(true);
                        slotUIs[i].SetEmpty();
                    }
                }

                return;
            }

            var slots = inventory.GetAllSlots();
            int currentIndex = inventory.CurrentIndex;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] == null)
                    continue;

                bool hasSlot =
                    slots != null &&
                    i < slots.Count &&
                    slots[i] != null &&
                    slots[i].item != null;

                if (!hasSlot)
                {
                    if (hideEmptySlots)
                    {
                        slotUIs[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        slotUIs[i].gameObject.SetActive(true);
                        slotUIs[i].SetEmpty();
                    }

                    continue;
                }

                slotUIs[i].gameObject.SetActive(true);

                var slot = slots[i];
                bool isSelected = i == currentIndex;

                slotUIs[i].SetData(
                    slot.item,
                    slot.count,
                    isSelected,
                    false,
                    showItemNames,
                    showItemIcons
                );
            }
        }
    }
}