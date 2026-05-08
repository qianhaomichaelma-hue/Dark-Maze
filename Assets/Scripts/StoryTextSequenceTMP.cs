using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class StoryTextSequenceTMP : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text storyText;
        [SerializeField] private TMP_Text hintText;

        [Header("Story Lines")]
        [TextArea(2, 6)]
        [SerializeField] private string[] lines;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private bool useTypewriter = true;
        [SerializeField] private float charactersPerSecond = 35f;
        [SerializeField] private float holdTimePerLine = 2.0f;
        [SerializeField] private float finishDelay = 1.0f;

        [Header("Scene Load")]
        [Tooltip("Leave empty if this story should not load another scene.")]
        [SerializeField] private string nextSceneName;

        [Header("Advance")]
        [Tooltip("If true, UI Button can call Advance() to skip typing or move to next line.")]
        [SerializeField] private bool allowManualAdvance = true;

        [SerializeField] private string hintMessage = "Click to continue";

        private Coroutine _routine;
        private bool _advanceRequested;
        private bool _skipTypingRequested;
        private bool _isTyping;

        private void Start()
        {
            if (storyText != null)
                storyText.text = "";

            if (hintText != null)
                hintText.text = allowManualAdvance ? hintMessage : "";

            StartSequence();
        }

        public void StartSequence()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlaySequence());
        }

        public void Advance()
        {
            if (!allowManualAdvance)
                return;

            if (_isTyping)
            {
                _skipTypingRequested = true;
            }
            else
            {
                _advanceRequested = true;
            }
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSeconds(startDelay);

            if (lines == null || lines.Length == 0)
            {
                yield return FinishSequence();
                yield break;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                _advanceRequested = false;
                _skipTypingRequested = false;

                if (useTypewriter)
                    yield return TypeLine(line);
                else if (storyText != null)
                    storyText.text = line;

                _advanceRequested = false;

                float timer = 0f;
                while (timer < holdTimePerLine)
                {
                    if (_advanceRequested)
                        break;

                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            yield return FinishSequence();
        }

        private IEnumerator TypeLine(string line)
        {
            if (storyText == null)
                yield break;

            _isTyping = true;
            storyText.text = "";

            float delay = 1f / Mathf.Max(1f, charactersPerSecond);

            for (int i = 0; i < line.Length; i++)
            {
                if (_skipTypingRequested)
                {
                    storyText.text = line;
                    break;
                }

                storyText.text += line[i];
                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
            _skipTypingRequested = false;
        }

        private IEnumerator FinishSequence()
        {
            yield return new WaitForSeconds(finishDelay);

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }
    }
}