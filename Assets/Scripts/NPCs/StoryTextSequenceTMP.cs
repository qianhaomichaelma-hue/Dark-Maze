using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class StoryTextSequenceTMP : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text storyText;
        [SerializeField] private TMP_Text hintText;

        [Tooltip("Optional. If assigned, this CanvasGroup will fade out both story text and hint text together.")]
        [SerializeField] private CanvasGroup textFadeGroup;

        [Header("Story Lines")]
        [TextArea(2, 6)]
        [SerializeField] private string[] lines;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private bool useTypewriter = true;
        [SerializeField] private float charactersPerSecond = 35f;

        [Header("Cursor")]
        [Tooltip("If true, the cursor is hidden and locked while this story scene is playing.")]
        [SerializeField] private bool hideCursorDuringStory = true;

        [Tooltip("Use this for Ending -> MainMenu. It unlocks and shows the cursor before loading the next scene.")]
        [SerializeField] private bool showCursorBeforeLoadingNextScene = false;

        [Header("Continue")]
        [SerializeField] private string continueMessage = "Press E to continue";

        [Tooltip("If true, pressing E while typing instantly completes the current line. If false, E only works after the line is fully typed.")]
        [SerializeField] private bool allowSkipTypingToFullLine = false;

        [Header("Continue Audio")]
        [SerializeField] private AudioSource continueAudioSource;
        [SerializeField] private AudioClip continueSFX;

        [Range(0f, 1f)]
        [SerializeField] private float continueSFXVolume = 0.8f;

        [Header("Enter Level Audio")]
        [Tooltip("Only plays after pressing E on the final story line.")]
        [SerializeField] private AudioSource enterLevelAudioSource;

        [SerializeField] private AudioClip enterLevelSFX;

        [Range(0f, 1f)]
        [SerializeField] private float enterLevelSFXVolume = 1f;

        [Tooltip("Delay after final E before the enter-level sound starts.")]
        [SerializeField] private float enterLevelSFXStartDelay = 0f;

        [Tooltip("If true, scene loading waits briefly so the enter-level sound can be heard.")]
        [SerializeField] private bool waitForEnterLevelSFXBeforeSceneLoad = true;

        [Tooltip("If > 0, use this wait time instead of the clip length.")]
        [SerializeField] private float enterLevelSFXWaitOverride = 0.8f;

        [Tooltip("Safety cap. Prevents scene load from waiting too long for a long audio clip.")]
        [SerializeField] private float maxEnterLevelSFXWait = 1.2f;

        [Header("Text Fade Out")]
        [SerializeField] private bool fadeTextOutOnContinue = true;

        [Tooltip("Fade duration between normal lines.")]
        [SerializeField] private float lineFadeOutDuration = 0.25f;

        [Tooltip("Fade duration after the final line before loading the next scene.")]
        [SerializeField] private float finalFadeOutDuration = 0.3f;

        [Tooltip("Short delay after final fade before loading the next scene.")]
        [SerializeField] private float finalSceneLoadDelay = 0.15f;

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

        private bool _isTyping;
        private bool _lineFinished;
        private bool _continuePressed;

        private Color _storyBaseColor = Color.white;
        private Color _hintBaseColor = Color.white;

        private bool _enterLevelSFXStarted;
        private float _enterLevelSFXStartTime;

        private void Awake()
        {
            ApplyStoryCursorState();

            CacheBaseTextColors();

            SetupTypingAudio();
            SetupContinueAudio();
            SetupEnterLevelAudio();
        }

        private void Start()
        {
            ApplyStoryCursorState();

            ResetTextVisualState();

            if (storyText != null)
                storyText.text = "";

            if (hintText != null)
                hintText.text = "";

            StartSequence();
        }

        private void Update()
        {
            if (!ContinuePressedThisFrame())
                return;

            if (_isTyping && allowSkipTypingToFullLine)
            {
                AcceptContinueInput();
                return;
            }

            if (_lineFinished)
            {
                AcceptContinueInput();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ApplyStoryCursorState();
        }

        private void ApplyStoryCursorState()
        {
            if (!hideCursorDuringStory)
                return;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ApplyVisibleCursorState()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private bool ContinuePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E))
                return true;
#endif

            return false;
        }

        private void AcceptContinueInput()
        {
            if (_continuePressed)
                return;

            _continuePressed = true;
            PlayContinueSFX();
        }

        private void CacheBaseTextColors()
        {
            if (storyText != null)
                _storyBaseColor = storyText.color;

            if (hintText != null)
                _hintBaseColor = hintText.color;
        }

        private void ResetTextVisualState()
        {
            if (textFadeGroup != null)
                textFadeGroup.alpha = 1f;

            if (storyText != null)
                storyText.color = _storyBaseColor;

            if (hintText != null)
                hintText.color = _hintBaseColor;
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

        private void SetupContinueAudio()
        {
            if (continueAudioSource == null)
                continueAudioSource = gameObject.AddComponent<AudioSource>();

            continueAudioSource.playOnAwake = false;
            continueAudioSource.loop = false;
            continueAudioSource.spatialBlend = 0f;
            continueAudioSource.dopplerLevel = 0f;
        }

        private void SetupEnterLevelAudio()
        {
            if (enterLevelAudioSource == null)
                enterLevelAudioSource = gameObject.AddComponent<AudioSource>();

            enterLevelAudioSource.playOnAwake = false;
            enterLevelAudioSource.loop = false;
            enterLevelAudioSource.spatialBlend = 0f;
            enterLevelAudioSource.dopplerLevel = 0f;
        }

        public void StartSequence()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            StopTypingAudio();

            _enterLevelSFXStarted = false;
            _enterLevelSFXStartTime = 0f;

            _routine = StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            if (hintText != null)
                hintText.text = "";

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            if (lines == null || lines.Length == 0)
            {
                yield return FinishSequence();
                yield break;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                bool isLastLine = i == lines.Length - 1;
                string line = lines[i];

                ResetTextVisualState();

                _continuePressed = false;
                _lineFinished = false;

                if (hintText != null)
                    hintText.text = "";

                if (useTypewriter)
                {
                    yield return TypeLine(line);
                }
                else
                {
                    if (storyText != null)
                        storyText.text = line;
                }

                StopTypingAudio();

                _lineFinished = true;
                _continuePressed = false;

                if (hintText != null)
                    hintText.text = continueMessage;

                yield return WaitForContinue();

                if (isLastLine)
                {
                    if (enterLevelSFXStartDelay > 0f)
                        yield return new WaitForSeconds(enterLevelSFXStartDelay);

                    PlayEnterLevelSFX();
                }

                if (fadeTextOutOnContinue)
                {
                    float fadeDuration = isLastLine ? finalFadeOutDuration : lineFadeOutDuration;
                    yield return FadeCurrentTextOut(fadeDuration);
                }

                if (hintText != null)
                    hintText.text = "";
            }

            yield return FinishSequence();
        }

        private IEnumerator TypeLine(string line)
        {
            if (storyText == null)
                yield break;

            _isTyping = true;
            _continuePressed = false;

            storyText.text = "";

            StartTypingAudio();

            float delay = 1f / Mathf.Max(1f, charactersPerSecond);

            for (int i = 0; i < line.Length; i++)
            {
                if (allowSkipTypingToFullLine && _continuePressed)
                {
                    storyText.text = line;
                    break;
                }

                storyText.text += line[i];

                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
            StopTypingAudio();
        }

        private IEnumerator WaitForContinue()
        {
            while (!_continuePressed)
                yield return null;

            _continuePressed = false;
            _lineFinished = false;
        }

        private IEnumerator FadeCurrentTextOut(float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            float timer = 0f;

            if (textFadeGroup != null)
            {
                float startAlpha = textFadeGroup.alpha;

                while (timer < safeDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / safeDuration);

                    textFadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                    yield return null;
                }

                textFadeGroup.alpha = 0f;
                yield break;
            }

            Color storyStart = storyText != null ? storyText.color : Color.white;
            Color hintStart = hintText != null ? hintText.color : Color.white;

            Color storyEnd = new Color(0f, 0f, 0f, 0f);
            Color hintEnd = new Color(0f, 0f, 0f, 0f);

            while (timer < safeDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / safeDuration);

                if (storyText != null)
                    storyText.color = Color.Lerp(storyStart, storyEnd, t);

                if (hintText != null)
                    hintText.color = Color.Lerp(hintStart, hintEnd, t);

                yield return null;
            }

            if (storyText != null)
                storyText.color = storyEnd;

            if (hintText != null)
                hintText.color = hintEnd;
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

        private void PlayContinueSFX()
        {
            if (continueAudioSource == null || continueSFX == null)
                return;

            continueAudioSource.PlayOneShot(continueSFX, continueSFXVolume);
        }

        private void PlayEnterLevelSFX()
        {
            if (enterLevelAudioSource == null || enterLevelSFX == null)
                return;

            _enterLevelSFXStarted = true;
            _enterLevelSFXStartTime = Time.time;

            enterLevelAudioSource.PlayOneShot(enterLevelSFX, enterLevelSFXVolume);
        }

        private IEnumerator FinishSequence()
        {
            StopTypingAudio();

            if (hintText != null)
                hintText.text = "";

            if (finalSceneLoadDelay > 0f)
                yield return new WaitForSeconds(finalSceneLoadDelay);

            if (waitForEnterLevelSFXBeforeSceneLoad && _enterLevelSFXStarted && enterLevelSFX != null)
            {
                float desiredWait = enterLevelSFXWaitOverride > 0f
                    ? enterLevelSFXWaitOverride
                    : enterLevelSFX.length;

                desiredWait = Mathf.Min(desiredWait, Mathf.Max(0f, maxEnterLevelSFXWait));

                float elapsed = Time.time - _enterLevelSFXStartTime;
                float remaining = desiredWait - elapsed;

                if (remaining > 0f)
                    yield return new WaitForSeconds(remaining);
            }

            if (showCursorBeforeLoadingNextScene)
                ApplyVisibleCursorState();

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnDisable()
        {
            StopTypingAudio();
        }
    }
}