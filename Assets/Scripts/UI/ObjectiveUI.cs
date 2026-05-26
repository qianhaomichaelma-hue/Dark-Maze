using TMPro;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class ObjectiveUI : MonoBehaviour
    {
        public static ObjectiveUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Options")]
        [SerializeField] private bool hideOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private void Awake()
        {
            // 不 Destroy duplicate，避免运行时把你正在用的 UI 删掉
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[ObjectiveUI] Another ObjectiveUI exists. Replacing Instance with {name}. Old Instance was {Instance.name}.",
                    this
                );
            }

            Instance = this;

            ResolveReferences();

            if (hideOnStart)
                Hide();
            else
                Show();

            if (debugLogs)
            {
                Debug.Log(
                    $"[ObjectiveUI] Ready. Object={name}, Text={(objectiveText != null ? objectiveText.name : "NULL")}, CanvasGroup={(canvasGroup != null ? canvasGroup.name : "NULL")}",
                    this
                );
            }
        }

        private void OnEnable()
        {
            Instance = this;
        }

        public void SetObjective(string text)
        {
            ResolveReferences();

            if (objectiveText == null)
            {
                Debug.LogWarning($"[ObjectiveUI] Objective Text is missing on {name}.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                ClearObjective();
                return;
            }

            objectiveText.text = text;
            Show();

            if (debugLogs)
                Debug.Log($"[ObjectiveUI] SetObjective: {text}", this);
        }

        public void ClearObjective()
        {
            ResolveReferences();

            if (objectiveText != null)
                objectiveText.text = "";

            Hide();

            if (debugLogs)
                Debug.Log("[ObjectiveUI] ClearObjective.", this);
        }

        private void ResolveReferences()
        {
            if (objectiveText == null)
                objectiveText = GetComponentInChildren<TMP_Text>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Show()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Hide()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        [ContextMenu("Test Objective")]
        private void TestObjective()
        {
            SetObjective("Light the first campfire");
        }
    }
}