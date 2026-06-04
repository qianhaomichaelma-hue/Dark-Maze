using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class AreaCinematicTrigger : MonoBehaviour
    {
        [Header("Cinematic Camera")]
        [Tooltip("Cinemachine Virtual Camera object used to show the new area.")]
        [SerializeField] private GameObject cinematicCameraObject;

        [SerializeField] private bool deactivateCameraOnStart = true;

        [Header("Timing")]
        [Tooltip("Delay after player enters trigger before switching camera.")]
        [SerializeField] private float delayBeforeCamera = 0f;

        [Tooltip("How long the cinematic camera stays active.")]
        [SerializeField] private float holdTime = 3f;

        [Tooltip("Delay after switching back before restoring control.")]
        [SerializeField] private float delayAfterCamera = 0.2f;

        [Header("Player Control")]
        [SerializeField] private bool lockPlayerDuringCinematic = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip revealSFX;

        [Range(0f, 1f)]
        [SerializeField] private float revealVolume = 1f;

        [Header("Settings")]
        [SerializeField] private bool onlyOnce = true;

        [Header("Events")]
        public UnityEvent onCinematicStarted;
        public UnityEvent onCinematicFinished;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool _triggered;
        private bool _running;

        private StarterAssetsInputs _cachedInputs;
        private ThirdPersonController _cachedThirdPersonController;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _cachedPlayerInput;
#endif

        private bool _inputsWereEnabled;
        private bool _controllerWasEnabled;

#if ENABLE_INPUT_SYSTEM
        private bool _playerInputWasEnabled;
#endif

        private PlayerState _currentPlayerState;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (cinematicCameraObject != null && deactivateCameraOnStart)
                cinematicCameraObject.SetActive(false);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_running)
                return;

            if (onlyOnce && _triggered)
                return;

            PlayerState playerState = other.GetComponentInParent<PlayerState>();

            if (playerState == null)
                return;

            if (playerState.IsDead)
                return;

            StartCoroutine(CinematicRoutine(playerState));
        }

        private IEnumerator CinematicRoutine(PlayerState playerState)
        {
            _running = true;
            _triggered = true;
            _currentPlayerState = playerState;

            if (lockPlayerDuringCinematic)
                LockPlayerControl(playerState);

            if (delayBeforeCamera > 0f)
                yield return new WaitForSeconds(delayBeforeCamera);

            PlayRevealSFX();

            if (cinematicCameraObject != null)
                cinematicCameraObject.SetActive(true);

            onCinematicStarted?.Invoke();

            Log("Area cinematic started.");

            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            if (cinematicCameraObject != null)
                cinematicCameraObject.SetActive(false);

            onCinematicFinished?.Invoke();

            if (delayAfterCamera > 0f)
                yield return new WaitForSeconds(delayAfterCamera);

            if (lockPlayerDuringCinematic && playerState != null && !playerState.IsDead)
                RestorePlayerControl();

            _running = false;

            Log("Area cinematic finished.");
        }

        private void LockPlayerControl(PlayerState playerState)
        {
            if (playerState == null)
                return;

            _cachedInputs = playerState.GetComponent<StarterAssetsInputs>();
            _cachedThirdPersonController = playerState.GetComponent<ThirdPersonController>();

#if ENABLE_INPUT_SYSTEM
            _cachedPlayerInput = playerState.GetComponent<PlayerInput>();
#endif

            _inputsWereEnabled = _cachedInputs != null && _cachedInputs.enabled;
            _controllerWasEnabled = _cachedThirdPersonController != null && _cachedThirdPersonController.enabled;

#if ENABLE_INPUT_SYSTEM
            _playerInputWasEnabled = _cachedPlayerInput != null && _cachedPlayerInput.enabled;
#endif

            if (_cachedInputs != null)
            {
                _cachedInputs.move = Vector2.zero;
                _cachedInputs.look = Vector2.zero;
                _cachedInputs.jump = false;
                _cachedInputs.sprint = false;

                _cachedInputs.interact = false;
                _cachedInputs.throwItem = false;
                _cachedInputs.switchEquipment = false;
                _cachedInputs.attack = false;

                _cachedInputs.enabled = false;
            }

            if (_cachedThirdPersonController != null)
                _cachedThirdPersonController.enabled = false;

#if ENABLE_INPUT_SYSTEM
            if (_cachedPlayerInput != null)
                _cachedPlayerInput.enabled = false;
#endif
        }

        private void RestorePlayerControl()
        {
            if (_cachedInputs != null)
                _cachedInputs.enabled = _inputsWereEnabled;

            if (_cachedThirdPersonController != null)
                _cachedThirdPersonController.enabled = _controllerWasEnabled;

#if ENABLE_INPUT_SYSTEM
            if (_cachedPlayerInput != null)
                _cachedPlayerInput.enabled = _playerInputWasEnabled;
#endif
        }

        private void PlayRevealSFX()
        {
            if (audioSource == null || revealSFX == null)
                return;

            audioSource.spatialBlend = 0f;
            audioSource.PlayOneShot(revealSFX, revealVolume);
        }

        private void OnDisable()
        {
            if (_running && lockPlayerDuringCinematic && _currentPlayerState != null && !_currentPlayerState.IsDead)
                RestorePlayerControl();

            if (cinematicCameraObject != null)
                cinematicCameraObject.SetActive(false);

            _running = false;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[AreaCinematicTrigger] {message}", this);
        }
    }
}