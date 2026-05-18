using UnityEngine;
using DarkMazeItems;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerEquipment : MonoBehaviour
    {
        [Header("Current Held Item")]
        [SerializeField] private ItemData heldItem;

        public ItemData HeldItem => heldItem;

        public bool IsHolding(ItemData item) => heldItem == item;

        public void Hold(ItemData item)
        {
            heldItem = item;
            Debug.Log($"[Equipment] Holding: {(item ? item.displayName : "None")}");
        }

        public void Clear()
        {
            heldItem = null;
            Debug.Log("[Equipment] Holding cleared");
        }
        
    }
}
