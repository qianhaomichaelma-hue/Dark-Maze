using UnityEngine;
using DarkMazeItems;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerHeldItemVisual : MonoBehaviour
    {
        [Header("Requirement")]
        [SerializeField] private ItemData targetItem;

        [Header("References")]
        [SerializeField] private PlayerEquipment equipment;

        [Tooltip("The visible model root for this held item.")]
        [SerializeField] private GameObject visualRoot;

        private void Awake()
        {
            if (equipment == null)
                equipment = GetComponent<PlayerEquipment>();

            if (visualRoot != null)
                visualRoot.SetActive(false);
        }

        private void Update()
        {
            if (equipment == null || targetItem == null || visualRoot == null)
                return;

            bool shouldShow = equipment.IsHolding(targetItem);

            if (visualRoot.activeSelf != shouldShow)
                visualRoot.SetActive(shouldShow);
        }
    }
}