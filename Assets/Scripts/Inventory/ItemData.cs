using UnityEngine;

namespace DarkMazeItems
{
    [CreateAssetMenu(menuName = "DarkMaze/ItemData", fileName = "ItemData_")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "torch";
        public string displayName = "Torch";

        [Header("Inventory UI")]
        public Sprite inventoryIcon;
        public Sprite selectedInventoryIcon;

        [Header("Stacking")]
        public int maxStack = 1;

        [Header("Optional")]
        public GameObject worldPrefab;
    }
}