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

        [Header("Typing Audio")]
        [SerializeField] private AudioSource typingAudioSource;
        [SerializeField] private AudioClip typingLoopClip;

        [Range(0f, 1f)]
        [SerializeField] private float typingAudioVolume = 0.6f;

        [Tooltip("If true, the typing audio loops while text is typing.")]
        [SerializeField] private bool loopTypingAudio = true;

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

        private void Awake()
        {
            SetupTypingAudio();
        }

        private void Start()
        {
            if (storyText != null)
                storyText.text = "";

            if (hintText != null)
                hintText.text = allowManualAdvance ? hintMessage : "";

            StartSequence();
        }

        private void SetupTypingAudio()
        {
            if (typingAudioSource == null)
            {
                typingAudioSource = GetComponent<AudioSource>();

                if (typingAudioSource == null)
                    typingAudioSource = gameObject.AddComponent<AudioSource>();
            }

            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = loopTypingAudio;
            typingAudioSource.spatialBlend = 0f;
            typingAudioSource.volume = typingAudioVolume;

            if (typingLoopClip != null)
                typingAudioSource.clip = typingLoopClip;

            typingAudioSource.Stop();
        }

        public void StartSequence()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            StopTypingAudio();
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

                StopTypingAudio();

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

            StartTypingAudio();

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

            StopTypingAudio();
        }

        private void StartTypingAudio()
        {
            if (typingAudioSource == null || typingLoopClip == null)
                return;

            typingAudioSource.volume = typingAudioVolume;
            typingAudioSource.loop = loopTypingAudio;

            if (typingAudioSource.clip != typingLoopClip)
                typingAudioSource.clip = typingLoopClip;

            if (!typingAudioSource.isPlaying)
            {
                typingAudioSource.Play();
            }
            else
            {
                typingAudioSource.UnPause();
            }
        }

        private void StopTypingAudio()
        {
            if (typingAudioSource == null)
                return;

            if (typingAudioSource.isPlaying)
                typingAudioSource.Pause();
        }

        private IEnumerator FinishSequence()
        {
            StopTypingAudio();

            yield return new WaitForSeconds(finishDelay);

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnDisable()
        {
            StopTypingAudio();
        }
    }
}