using System;
using UnityEngine;
using TMPro;

namespace DarkMazeMinimal
{
    [Serializable]
    public class DialogueAudioProfile
    {
        [Tooltip("Optional. If assigned, dialogue audio will play from this source. If empty, DialogueUI's default AudioSource will be used.")]
        public AudioSource audioSourceOverride;

        [Tooltip("Each time a new dialogue line appears, one clip from this list will be randomly played.")]
        public AudioClip[] lineSFXList;

        [Tooltip("Optional. Played when the player advances to the next line. Can be empty.")]
        public AudioClip advanceSFX;

        [Range(0f, 1f)]
        public float lineVolume = 0.7f;

        [Range(0f, 1f)]
        public float advanceVolume = 0.5f;

        [NonSerialized] public int lastLineSFXIndex = -1;
    }

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

        [Header("Default Audio")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Fallback audio used only when an NPC does not provide its own DialogueAudioProfile.")]
        [SerializeField] private DialogueAudioProfile defaultAudio = new DialogueAudioProfile();

        [Header("Audio Rules")]
        [SerializeField] private bool stopLineSFXOnAdvance = true;
        [SerializeField] private bool preventImmediateRepeat = true;

        [Tooltip("Usually keep false. If true, pressing continue also plays advanceSFX.")]
        [SerializeField] private bool playAdvanceSFX = false;

        private string _speakerName;
        private string[] _lines;
        private int _index;
        private Action _onFinished;

        private DialogueAudioProfile _currentAudioProfile;
        private AudioSource _currentLineAudioSource;

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

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        public void ShowLines(
            string speakerName,
            string[] lines,
            Action onFinished = null,
            DialogueAudioProfile audioProfile = null
        )
        {
            StopCurrentLineSFX();

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
            _currentAudioProfile = audioProfile ?? defaultAudio;

            if (panel != null)
                panel.SetActive(true);

            ShowCurrentLine();
        }

        public void ShowSingleLine(
            string speakerName,
            string line,
            Action onFinished = null,
            DialogueAudioProfile audioProfile = null
        )
        {
            ShowLines(speakerName, new[] { line }, onFinished, audioProfile);
        }

        public void Advance()
        {
            if (!IsOpen)
                return;

            if (stopLineSFXOnAdvance)
                StopCurrentLineSFX();

            if (playAdvanceSFX)
                PlayAdvanceSFX();

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
            StopCurrentLineSFX();

            _speakerName = null;
            _lines = null;
            _index = 0;
            _onFinished = null;
            _currentAudioProfile = null;

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

            PlayRandomLineSFX();
        }

        private void PlayRandomLineSFX()
        {
            DialogueAudioProfile profile = _currentAudioProfile ?? defaultAudio;
            if (profile == null)
                return;

            if (profile.lineSFXList == null || profile.lineSFXList.Length == 0)
                return;

            AudioSource source = GetAudioSource(profile);
            if (source == null)
                return;

            int selectedIndex = GetRandomLineSFXIndex(profile);

            if (selectedIndex < 0 || selectedIndex >= profile.lineSFXList.Length)
                return;

            AudioClip selectedClip = profile.lineSFXList[selectedIndex];

            if (selectedClip == null)
                return;

            StopCurrentLineSFX();

            _currentLineAudioSource = source;

            source.playOnAwake = false;
            source.loop = false;
            source.clip = selectedClip;
            source.volume = profile.lineVolume;
            source.Play();

            profile.lastLineSFXIndex = selectedIndex;
        }

        private int GetRandomLineSFXIndex(DialogueAudioProfile profile)
        {
            if (profile == null || profile.lineSFXList == null || profile.lineSFXList.Length == 0)
                return -1;

            int validCount = 0;

            for (int i = 0; i < profile.lineSFXList.Length; i++)
            {
                if (profile.lineSFXList[i] != null)
                    validCount++;
            }

            if (validCount <= 0)
                return -1;

            if (validCount == 1)
            {
                for (int i = 0; i < profile.lineSFXList.Length; i++)
                {
                    if (profile.lineSFXList[i] != null)
                        return i;
                }
            }

            int selectedIndex = -1;
            int safety = 30;

            while (safety > 0)
            {
                safety--;

                int randomIndex = UnityEngine.Random.Range(0, profile.lineSFXList.Length);

                if (profile.lineSFXList[randomIndex] == null)
                    continue;

                if (preventImmediateRepeat && randomIndex == profile.lastLineSFXIndex)
                    continue;

                selectedIndex = randomIndex;
                break;
            }

            if (selectedIndex >= 0)
                return selectedIndex;

            for (int i = 0; i < profile.lineSFXList.Length; i++)
            {
                if (profile.lineSFXList[i] == null)
                    continue;

                if (preventImmediateRepeat && i == profile.lastLineSFXIndex)
                    continue;

                return i;
            }

            for (int i = 0; i < profile.lineSFXList.Length; i++)
            {
                if (profile.lineSFXList[i] != null)
                    return i;
            }

            return -1;
        }

        private void StopCurrentLineSFX()
        {
            if (_currentLineAudioSource == null)
                return;

            if (_currentLineAudioSource.isPlaying)
                _currentLineAudioSource.Stop();

            _currentLineAudioSource.clip = null;
            _currentLineAudioSource = null;
        }

        private void PlayAdvanceSFX()
        {
            DialogueAudioProfile profile = _currentAudioProfile ?? defaultAudio;
            if (profile == null)
                return;

            if (profile.advanceSFX == null)
                return;

            AudioSource source = GetAudioSource(profile);
            if (source == null)
                return;

            source.PlayOneShot(profile.advanceSFX, profile.advanceVolume);
        }

        private AudioSource GetAudioSource(DialogueAudioProfile profile)
        {
            if (profile != null && profile.audioSourceOverride != null)
                return profile.audioSourceOverride;

            return audioSource;
        }
    }
}