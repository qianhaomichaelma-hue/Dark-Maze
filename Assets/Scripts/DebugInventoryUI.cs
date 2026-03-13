using System.Text;
using TMPro;
using UnityEngine;
using DarkMazeItems;

namespace DarkMazeUI
{
    public class DebugInventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private TMP_Text inventoryText;

        [Header("Options")]
        [SerializeField] private bool refreshEveryFrame = true;

        private readonly StringBuilder _sb = new StringBuilder();

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
            if (inventoryText == null)
                return;

            _sb.Clear();
            _sb.AppendLine("=== INVENTORY ===");

            if (inventory == null)
            {
                _sb.AppendLine("Missing PlayerInventory");
                inventoryText.text = _sb.ToString();
                return;
            }

            var slots = inventory.GetAllSlots();

            if (slots == null || slots.Count == 0)
            {
                _sb.AppendLine("Empty");
            }
            else
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];

                    if (slot == null || slot.item == null)
                    {
                        _sb.AppendLine($"[{i}] NULL");
                        continue;
                    }

                    _sb.AppendLine($"{slot.item.displayName} x {slot.count}");
                }
            }

            _sb.AppendLine("----------------");
            _sb.AppendLine($"Slots: {inventory.SlotCount}/{inventory.maxSlots}");

            inventoryText.text = _sb.ToString();
        }
    }
}
