using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DarkMazeUI
{
    [DisallowMultipleComponent]
    public class GameMessageUI : MonoBehaviour
    {
        public static GameMessageUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float showTime = 1.2f;
        [SerializeField] private float fadeTime = 0.35f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private readonly Queue<string> _messageQueue = new Queue<string>();
        private Coroutine _routine;
        private bool _isShowing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            HideImmediate();
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messageQueue.Enqueue(message);

            if (debugLogs)
                Debug.Log($"[GameMessageUI] Queue message: {message}", this);

            if (!_isShowing)
            {
                if (_routine != null)
                    StopCoroutine(_routine);

                _routine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            _isShowing = true;

            while (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();

                if (messageText != null)
                    messageText.text = message;

                ShowImmediate();

                if (debugLogs)
                    Debug.Log($"[GameMessageUI] Show message: {message}", this);

                yield return new WaitForSecondsRealtime(showTime);

                float timer = 0f;

                while (timer < fadeTime)
                {
                    timer += Time.unscaledDeltaTime;
                    float t = timer / Mathf.Max(0.01f, fadeTime);

                    if (canvasGroup != null)
                        canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                    yield return null;
                }

                HideImmediate();
            }

            _isShowing = false;
            _routine = null;
        }

        private void ShowImmediate()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            transform.SetAsLastSibling();
        }

        private void HideImmediate()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        [ContextMenu("Test Message")]
        private void TestMessage()
        {
            ShowMessage("重生点更新");
        }
    }
}