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

        [SerializeField] private int currentIndex = 0;

        public PlayerEquipment equipment;

        private StarterAssetsInputs _inputs;

        public IReadOnlyList<Slot> Slots => slots;
        public int SlotCount => slots.Count;
        public int CurrentIndex => currentIndex;

        public Slot currentSlot
        {
            get
            {
                if (slots == null || slots.Count == 0)
                    return null;

                if (currentIndex < 0 || currentIndex >= slots.Count)
                    currentIndex = 0;

                return slots[currentIndex];
            }
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
                _inputs.switchEquipment = false;
                TrySwitchEquipmentItem();
            }
        }

        public bool Has(ItemData item, int amount = 1)
        {
            if (item == null) return false;

            foreach (var s in slots)
            {
                if (s != null && s.item == item && s.count >= amount)
                    return true;
            }

            return false;
        }

        public bool TryAdd(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            if (item.maxStack > 1)
            {
                foreach (var s in slots)
                {
                    if (s != null && s.item == item && s.count < item.maxStack)
                    {
                        int canAdd = Mathf.Min(amount, item.maxStack - s.count);
                        s.count += canAdd;
                        amount -= canAdd;

                        if (amount <= 0)
                        {
                            Debug.Log($"[Inventory] Added {item.displayName}. Now: {s.count}");
                            EnsureValidCurrentIndex();
                            SyncHeldItemToCurrentSlot();
                            return true;
                        }
                    }
                }
            }

            while (amount > 0)
            {
                if (slots.Count >= maxSlots)
                {
                    Debug.Log($"[Inventory] Full. Can't add {item.displayName}");
                    return false;
                }

                int add = Mathf.Min(amount, Mathf.Max(1, item.maxStack));
                slots.Add(new Slot { item = item, count = add });
                amount -= add;

                Debug.Log($"[Inventory] Added {item.displayName} x{add}. Slots: {slots.Count}/{maxSlots}");
            }

            EnsureValidCurrentIndex();

            if (slots.Count == 1 && equipment != null && equipment.HeldItem == null)
                SyncHeldItemToCurrentSlot();

            return true;
        }

        public bool TryRemove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var s = slots[i];
                if (s == null || s.item != item) continue;

                int take = Mathf.Min(amount, s.count);
                s.count -= take;
                amount -= take;

                if (s.count <= 0)
                    slots.RemoveAt(i);

                if (amount <= 0)
                {
                    Debug.Log($"[Inventory] Removed {item.displayName}");
                    EnsureValidCurrentIndex();
                    SyncHeldItemToCurrentSlot();
                    return true;
                }
            }

            return false;
        }

        public void TryRemoveCurrent()
        {
            if (slots == null || slots.Count == 0) return;
            if (currentIndex < 0 || currentIndex >= slots.Count) return;

            slots.RemoveAt(currentIndex);
            EnsureValidCurrentIndex();
            SyncHeldItemToCurrentSlot();
        }

        public bool TryGetCurrentItem(int amount, ItemData targetItem)
        {
            if (amount <= 0) return false;

            var slot = currentSlot;
            if (slot == null) return false;
            if (slot.item != targetItem) return false;
            if (slot.count < amount) return false;

            slot.count -= amount;
            Debug.Log($"[Inventory] Removed {slot.item.displayName} x{amount}");

            if (slot.count <= 0)
            {
                slots.RemoveAt(currentIndex);
                EnsureValidCurrentIndex();
                SyncHeldItemToCurrentSlot();
            }

            return true;
        }

        private void TrySwitchEquipmentItem()
        {
            if (SlotCount <= 1)
                return;

            currentIndex++;
            if (currentIndex >= SlotCount)
                currentIndex = 0;

            SyncHeldItemToCurrentSlot();
        }

        public void UpdateCurrentSlot()
        {
            EnsureValidCurrentIndex();
            SyncHeldItemToCurrentSlot();
        }

        public void EquipItem(ItemData item)
        {
            if (item == null)
            {
                if (equipment != null)
                    equipment.Hold(null);
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].item == item)
                {
                    currentIndex = i;
                    SyncHeldItemToCurrentSlot();
                    return;
                }
            }

            EnsureValidCurrentIndex();
            SyncHeldItemToCurrentSlot();
        }

        public List<Slot> GetAllSlots()
        {
            return slots;
        }

        private void EnsureValidCurrentIndex()
        {
            if (slots == null || slots.Count == 0)
            {
                currentIndex = 0;
                return;
            }

            if (currentIndex < 0)
                currentIndex = 0;

            if (currentIndex >= slots.Count)
                currentIndex = 0;
        }

        private void SyncHeldItemToCurrentSlot()
        {
            if (equipment == null) return;

            var slot = currentSlot;
            if (slot == null || slot.item == null)
            {
                equipment.Hold(null);
                return;
            }

            equipment.Hold(slot.item);
        }
    }
}