using TMPro;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class ObjectiveUI : MonoBehaviour
    {
        public static ObjectiveUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text objectiveText;

        [Header("Options")]
        [SerializeField] private bool hideOnStart = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (root != null)
                root.SetActive(!hideOnStart);
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null)
                objectiveText.text = text;

            if (root != null)
                root.SetActive(!string.IsNullOrWhiteSpace(text));
        }

        public void ClearObjective()
        {
            if (objectiveText != null)
                objectiveText.text = "";

            if (root != null)
                root.SetActive(false);
        }
    }
}