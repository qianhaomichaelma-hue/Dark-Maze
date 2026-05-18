using System.Collections;
using TMPro;
using UnityEngine;

namespace DarkMazeUI
{
    [DisallowMultipleComponent]
    public class ItemGainPopupUI : MonoBehaviour
    {
        public static ItemGainPopupUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text popupText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float showTime = 1.2f;
        [SerializeField] private float fadeTime = 0.35f;

        [Header("Format")]
        [SerializeField] private string format = "{0} +{1}";

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private Coroutine _routine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (popupText == null)
                popupText = GetComponentInChildren<TMP_Text>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>(true);

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            HideImmediate();

            if (debugLogs)
                Debug.Log("[ItemGainPopupUI] Ready.", this);
        }

        public void ShowItemGain(string itemName, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                itemName = "Item";

            if (amount <= 0)
                amount = 1;

            if (popupText == null)
            {
                Debug.LogWarning("[ItemGainPopupUI] Popup Text is missing.", this);
                return;
            }

            if (canvasGroup == null)
            {
                Debug.LogWarning("[ItemGainPopupUI] CanvasGroup is missing.", this);
                return;
            }

            popupText.text = string.Format(format, itemName, amount);

            transform.SetAsLastSibling();

            if (debugLogs)
                Debug.Log($"[ItemGainPopupUI] Show: {popupText.text}", this);

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            yield return new WaitForSecondsRealtime(showTime);

            float timer = 0f;

            while (timer < fadeTime)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / fadeTime;

                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            HideImmediate();
        }

        private void HideImmediate()
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        [ContextMenu("Test Popup")]
        private void TestPopup()
        {
            ShowItemGain("Torch", 1);
        }
    }
}