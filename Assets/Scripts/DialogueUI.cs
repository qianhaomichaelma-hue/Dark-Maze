using System;
using UnityEngine;
using TMPro;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text continueText;

        [Header("Text")]
        [SerializeField] private string continueMessage = "Press E to continue";

        private string _speakerName;
        private string[] _lines;
        private int _index;
        private Action _onFinished;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (panel != null)
                panel.SetActive(false);

            if (continueText != null)
                continueText.text = continueMessage;
        }

        public void ShowLines(string speakerName, string[] lines, Action onFinished = null)
        {
            if (lines == null || lines.Length == 0)
            {
                Hide();
                onFinished?.Invoke();
                return;
            }

            _speakerName = speakerName;
            _lines = lines;
            _index = 0;
            _onFinished = onFinished;

            if (panel != null)
                panel.SetActive(true);

            ShowCurrentLine();
        }

        public void ShowSingleLine(string speakerName, string line, Action onFinished = null)
        {
            ShowLines(speakerName, new[] { line }, onFinished);
        }

        public void Advance()
        {
            if (!IsOpen)
                return;

            _index++;

            if (_lines == null || _index >= _lines.Length)
            {
                Action finished = _onFinished;
                Hide();
                finished?.Invoke();
                return;
            }

            ShowCurrentLine();
        }

        public void Hide()
        {
            _speakerName = null;
            _lines = null;
            _index = 0;
            _onFinished = null;

            if (panel != null)
                panel.SetActive(false);
        }

        private void ShowCurrentLine()
        {
            if (speakerText != null)
                speakerText.text = string.IsNullOrEmpty(_speakerName) ? "" : _speakerName;

            if (dialogueText != null && _lines != null && _lines.Length > 0)
                dialogueText.text = _lines[_index];

            if (continueText != null)
                continueText.text = continueMessage;
        }
    }
}