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

        private Coroutine _routine;

        private void Awake()
        {
            ApplyHiddenCursorState();
            SetupTypingAudio();
        }

        private void Start()
        {
            ApplyHiddenCursorState();

            if (storyText != null)
                storyText.text = "";

            if (hintText != null)
                hintText.text = "";

            StartSequence();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ApplyHiddenCursorState();
        }

        private void ApplyHiddenCursorState()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
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

                if (useTypewriter)
                {
                    yield return TypeLine(line);
                }
                else if (storyText != null)
                {
                    storyText.text = line;
                }

                StopTypingAudio();

                if (holdTimePerLine > 0f)
                    yield return new WaitForSeconds(holdTimePerLine);
            }

            yield return FinishSequence();
        }

        private IEnumerator TypeLine(string line)
        {
            if (storyText == null)
                yield break;

            storyText.text = "";

            StartTypingAudio();

            float delay = 1f / Mathf.Max(1f, charactersPerSecond);

            for (int i = 0; i < line.Length; i++)
            {
                storyText.text += line[i];
                yield return new WaitForSeconds(delay);
            }

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

            if (finishDelay > 0f)
                yield return new WaitForSeconds(finishDelay);

            ApplyHiddenCursorState();

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnDisable()
        {
            StopTypingAudio();
        }
    }
}