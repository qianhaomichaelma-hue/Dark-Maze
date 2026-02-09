using System;
using System.Collections.Generic;
using UnityEngine;

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

        public bool Has(ItemData item, int amount = 1)
        {
            if (item == null) return false;
            foreach (var s in slots)
            {
                if (s.item == item && s.count >= amount) return true;
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
    }
}
