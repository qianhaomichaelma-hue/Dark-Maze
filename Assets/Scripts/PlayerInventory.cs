using System;
using System.Collections.Generic;
using UnityEngine;
using DarkMazePlayer;
using StarterAssets;

namespace DarkMazeItems
{
    [DisallowMultipleComponent]
    public class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public ItemData item;
            public int count;
        }

        [Header("Capacity")]
        [Range(1, 10)]
        public int maxSlots = 2;

        [SerializeField] private List<Slot> slots = new List<Slot>();

        int currentIndex = 0;

        public PlayerEquipment equipment;

        public IReadOnlyList<Slot> Slots => slots;
        public int SlotCount => slots.Count;

        public Slot currentSlot => slots[currentIndex];

        private StarterAssetsInputs _inputs;

        public bool Has(ItemData item, int amount = 1)
        {
            if (item == null) return false;

            foreach (var s in slots)
            {
                if (s.item == item && s.count >= amount)
                    return true;
            }

            return false;
        }
        public bool TryAdd(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            // Try stack first
            if (item.maxStack > 1)
            {
                foreach (var s in slots)
                {
                    if (s.item == item && s.count < item.maxStack)
                    {
                        int canAdd = Mathf.Min(amount, item.maxStack - s.count);
                        s.count += canAdd;
                        amount -= canAdd;

                        if (amount <= 0)
                        {
                            Debug.Log($"[Inventory] Added {item.displayName}. Now: {s.count}");
                            return true;
                        }
                    }
                }
            }

            // Add to new slots
            while (amount > 0)
            {
                if (slots.Count >= maxSlots)
                {
                    Debug.Log($"[Inventory] Full. Can't add {item.displayName}");
                    return false;
                }

                int add = Mathf.Min(amount, item.maxStack);
                slots.Add(new Slot { item = item, count = add });
                amount -= add;

                Debug.Log($"[Inventory] Added {item.displayName} x{add}. Slots: {slots.Count}/{maxSlots}");
            }

            return true;
        }
        public bool TryRemove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var s = slots[i];
                if (s.item != item) continue;

                int take = Mathf.Min(amount, s.count);
                s.count -= take;
                amount -= take;

                if (s.count <= 0)
                    slots.RemoveAt(i);

                if (amount <= 0)
                {
                    Debug.Log($"[Inventory] Removed {item.displayName}");
                    return true;
                }
            }

            return false;
        }
        public void TryRemoveCurrent()
        {
            slots.RemoveAt(currentIndex);
        }
        public bool TryGetCurrentItem(int amount, ItemData targetItem)
        {
            if (amount <= 0) return false;

            if (SlotCount <= 0)
                return false;
            else if(currentSlot.item != targetItem)
            {
                return false;
            }
            else
            {
                Debug.Log($"[Inventory] Removed {currentSlot.item.displayName}");
                currentSlot.count--;
                return true;
            }
        }
        private void TrySwitchEquipmentItem()
        {
            // special circ...
            if(SlotCount == 0 || SlotCount == 1)
            {
                return;
            }

            // deal the change logic
            if(currentIndex == SlotCount - 1)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex += 1;
            }

            equipment.Hold(slots[currentIndex].item);
        }
        public void UpdateCurrentSlot()
        {
            // special circ...
            if (SlotCount == 0)
            {
                equipment.Hold(null);
                return;
            }
            else
            {
                equipment.Hold(slots[0].item);
                currentIndex = 0;
                return;
            }
        }
        public void EquipItem(ItemData item)
        {
            equipment.Hold(item);
            currentIndex = SlotCount - 1;
        }
        public List<Slot> GetAllSlots()
        {
            return slots;
        }

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
        }
        private void Update()
        {
            if (_inputs == null) return;

            if (_inputs.switchEquipment)
            {
                _inputs.switchEquipment = false; // œ˚∑— ‰»Î
                TrySwitchEquipmentItem();
            }
        }
    }
}