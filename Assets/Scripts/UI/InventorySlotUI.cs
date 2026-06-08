using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DarkMazeItems;

namespace DarkMazeUI
{
    [DisallowMultipleComponent]
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image backgroundImage;

        [Header("Display Options")]
        [SerializeField] private bool showCountWhenOne = true;

        [Header("Optional Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

        public void SetData(
            ItemData item,
            int count,
            bool isSelected,
            bool isEmpty,
            bool showItemName,
            bool showItemIcon
        )
        {
            if (isEmpty || item == null)
            {
                SetEmpty();
                return;
            }

            if (selectedHighlight != null)
                selectedHighlight.SetActive(isSelected);

            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(showItemName);
                itemNameText.text = showItemName ? item.displayName : "";
            }

            if (countText != null)
            {
                bool shouldShowCount = showCountWhenOne || count > 1;

                countText.gameObject.SetActive(shouldShowCount);
                countText.text = shouldShowCount ? $"x{count}" : "";
            }

            if (itemIconImage != null)
            {
                Sprite iconToUse = GetIcon(item, isSelected);

                itemIconImage.gameObject.SetActive(showItemIcon);
                itemIconImage.enabled = showItemIcon && iconToUse != null;
                itemIconImage.sprite = showItemIcon ? iconToUse : null;
                itemIconImage.preserveAspect = true;
            }

            if (backgroundImage != null)
                backgroundImage.color = isSelected ? selectedColor : normalColor;
        }

        public void SetEmpty()
        {
            if (selectedHighlight != null)
                selectedHighlight.SetActive(false);

            if (itemNameText != null)
            {
                itemNameText.text = "";
                itemNameText.gameObject.SetActive(false);
            }

            if (countText != null)
            {
                countText.text = "";
                countText.gameObject.SetActive(false);
            }

            if (itemIconImage != null)
            {
                itemIconImage.sprite = null;
                itemIconImage.enabled = false;
                itemIconImage.gameObject.SetActive(false);
            }

            if (backgroundImage != null)
                backgroundImage.color = emptyColor;
        }

        private Sprite GetIcon(ItemData item, bool isSelected)
        {
            if (item == null)
                return null;

            if (isSelected && item.selectedInventoryIcon != null)
                return item.selectedInventoryIcon;

            return item.inventoryIcon;
        }
    }
}