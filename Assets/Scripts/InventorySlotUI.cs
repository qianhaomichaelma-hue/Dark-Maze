using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkMazeUI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image backgroundImage;

        [Header("Optional Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

        public void SetData(string itemName, int count, bool isSelected, bool isEmpty)
        {
            if (selectedHighlight != null)
                selectedHighlight.SetActive(isSelected);

            if (itemNameText != null)
                itemNameText.text = isEmpty ? "Empty" : itemName;

            if (countText != null)
                countText.text = isEmpty ? string.Empty : $"x{count}";

            if (backgroundImage != null)
            {
                if (isEmpty)
                    backgroundImage.color = emptyColor;
                else
                    backgroundImage.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}