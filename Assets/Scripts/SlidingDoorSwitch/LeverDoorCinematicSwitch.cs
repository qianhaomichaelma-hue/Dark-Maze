using System.Collections;
using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class LeverDoorCinematicSwitch : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private bool onlyOnce = true;
        [SerializeField] private InteractPromptTarget promptTarget;
        [SerializeField] private bool disablePromptAfterUse = true;

        [Header("Lever Animator Only")]
        [SerializeField] private Animator leverAnimator;

        [Tooltip("Usually 0.")]
        [SerializeField] private int animatorLayerIndex = 0;

        [Tooltip("Animator state name. Use the exact state name, e.g. Pull.")]
        [SerializeField] private string leverStateName = "Pull";

        [Tooltip("If direct state name fails, try full path like Base Layer.Pull.")]
        [SerializeField] private string leverFullStatePath = "Base Layer.Pull";

        [Tooltip("Optional. Leave empty if using direct Animator.Play only.")]
        [SerializeField] private string leverTriggerName = "";

        [SerializeField] private bool useDirectAnimatorPlay = true;
        [SerializeField] private bool alsoSetTrigger = false;

        [Tooltip("How long to wait after playing lever animation before switching to cinematic camera.")]
        [SerializeField] private float leverAnimationWaitTime = 0.65f;

        [Tooltip("For testing long imported clips. 1 = normal, 10 = 10x speed.")]
        [SerializeField] private float animatorSpeedDuringPull = 1f;

        [Header("Lever Audio - 2D")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("If true, lever sound is played as 2D feedback and will not be reduced by distance.")]
        [SerializeField] private bool force2DAudio = true;

        [SerializeField] private AudioClip leverPullSFX;

        [Range(0f, 1f)]
        [SerializeField] private float leverPullVolume = 1f;

        [Header("Cinematic Camera")]
        [SerializeField] private GameObject cinematicCameraObject;
        [SerializeField] private bool deactivateCinematicCameraOnStart = true;

        [SerializeField] private float cameraBlendInWait = 0.6f;
        [SerializeField] private float holdAfterDoorOpen = 0.9f;
        [SerializeField] private float cameraBlendOutWait = 0.35f;

        [Header("Door Sequence")]
        [SerializeField] private RemoteSlidingDoor targetDoor;
        [SerializeField] private float delayBeforeDoorLight = 0.4f;
        [SerializeField] private float delayAfterDoorLight = 0.45f;

        [Header("Player Control")]
        [SerializeField] private bool lockPlayerDuringSequence = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private bool _used;
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

        private void Awake()
        {
            if (leverAnimator == null)
                leverAnimator = GetComponentInChildren<Animator>();

            SetupAudioSource();

            if (promptTarget == null)
                promptTarget = GetComponentInChildren<InteractPromptTarget>(true);

            if (cinematicCameraObject != null && deactivateCinematicCameraOnStart)
                cinematicCameraObject.SetActive(false);
        }

        private void SetupAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.dopplerLevel = 0f;

            if (force2DAudio)
            {
                audioSource.spatialBlend = 0f;
            }
            else
            {
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 4f;
                audioSource.maxDistance = 25f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (_running)
                return;

            if (onlyOnce && _used)
                return;

            if (interactor == null)
                return;

            PlayerState playerState = interactor.PlayerState;

            if (playerState != null && playerState.IsDead)
                return;

            StartCoroutine(LeverSequence(interactor));
        }

        private IEnumerator LeverSequence(PlayerInteractor interactor)
        {
            _running = true;

            if (promptTarget != null)
                promptTarget.Hide();

            if (lockPlayerDuringSequence)
                LockPlayerControl(interactor);

            PlayLeverSFX();

            yield return StartCoroutine(PlayLeverAnimationAnimatorOnly());

            if (cinematicCameraObject == null)
                Debug.LogWarning("[LeverDoorCinematicSwitch] Cinematic Camera Object is missing.", this);

            if (targetDoor == null)
                Debug.LogWarning("[LeverDoorCinematicSwitch] Target Door is missing.", this);

            EnableCinematicCamera(true);

            if (cameraBlendInWait > 0f)
                yield return new WaitForSeconds(cameraBlendInWait);

            if (delayBeforeDoorLight > 0f)
                yield return new WaitForSeconds(delayBeforeDoorLight);

            if (targetDoor != null)
                targetDoor.TurnOnDoorLights();

            if (delayAfterDoorLight > 0f)
                yield return new WaitForSeconds(delayAfterDoorLight);

            if (targetDoor != null)
                yield return StartCoroutine(targetDoor.OpenDoorRoutine());

            if (holdAfterDoorOpen > 0f)
                yield return new WaitForSeconds(holdAfterDoorOpen);

            EnableCinematicCamera(false);

            if (cameraBlendOutWait > 0f)
                yield return new WaitForSeconds(cameraBlendOutWait);

            if (lockPlayerDuringSequence)
                RestorePlayerControl();

            _running = false;
            _used = true;

            if (disablePromptAfterUse && promptTarget != null)
            {
                promptTarget.Hide();
                promptTarget.enabled = false;
            }

            Log("Lever door sequence finished.");
        }

        private IEnumerator PlayLeverAnimationAnimatorOnly()
        {
            if (leverAnimator == null)
            {
                Debug.LogWarning("[LeverDoorCinematicSwitch] Lever Animator is missing.", this);

                if (leverAnimationWaitTime > 0f)
                    yield return new WaitForSeconds(leverAnimationWaitTime);

                yield break;
            }

            leverAnimator.enabled = true;
            leverAnimator.applyRootMotion = false;
            leverAnimator.speed = Mathf.Max(0.01f, animatorSpeedDuringPull);

            bool played = false;

            if (alsoSetTrigger && !string.IsNullOrEmpty(leverTriggerName))
            {
                leverAnimator.ResetTrigger(leverTriggerName);
                leverAnimator.SetTrigger(leverTriggerName);
                Log($"SetTrigger: {leverTriggerName}");
            }

            if (useDirectAnimatorPlay)
            {
                if (!string.IsNullOrEmpty(leverFullStatePath) &&
                    leverAnimator.HasState(animatorLayerIndex, Animator.StringToHash(leverFullStatePath)))
                {
                    leverAnimator.Play(leverFullStatePath, animatorLayerIndex, 0f);
                    leverAnimator.Update(0f);
                    played = true;

                    Log($"Animator.Play full path: {leverFullStatePath}");
                }
                else if (!string.IsNullOrEmpty(leverStateName) &&
                         leverAnimator.HasState(animatorLayerIndex, Animator.StringToHash(leverStateName)))
                {
                    leverAnimator.Play(leverStateName, animatorLayerIndex, 0f);
                    leverAnimator.Update(0f);
                    played = true;

                    Log($"Animator.Play state: {leverStateName}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[LeverDoorCinematicSwitch] Animator state not found. Tried '{leverFullStatePath}' and '{leverStateName}'. " +
                        "Check Animator state name and layer.",
                        this
                    );
                }
            }

            if (!played && !alsoSetTrigger)
            {
                Debug.LogWarning(
                    "[LeverDoorCinematicSwitch] Lever animation was not played. Enable Use Direct Animator Play or Also Set Trigger.",
                    this
                );
            }

            if (leverAnimationWaitTime > 0f)
                yield return new WaitForSeconds(leverAnimationWaitTime);
        }

        private void EnableCinematicCamera(bool enabled)
        {
            if (cinematicCameraObject == null)
                return;

            cinematicCameraObject.SetActive(enabled);

            Log(enabled ? "Cinematic camera enabled." : "Cinematic camera disabled.");
        }

        private void LockPlayerControl(PlayerInteractor interactor)
        {
            _cachedInputs = interactor.GetComponent<StarterAssetsInputs>();
            _cachedThirdPersonController = interactor.GetComponent<ThirdPersonController>();

#if ENABLE_INPUT_SYSTEM
            _cachedPlayerInput = interactor.GetComponent<PlayerInput>();
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

        private void PlayLeverSFX()
        {
            if (audioSource == null || leverPullSFX == null)
                return;

            if (force2DAudio)
                audioSource.spatialBlend = 0f;

            audioSource.PlayOneShot(leverPullSFX, leverPullVolume);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[LeverDoorCinematicSwitch] {message}", this);
        }

        [ContextMenu("DEBUG / Force Play Lever Animation")]
        private void DebugForcePlayLeverAnimation()
        {
            if (leverAnimator == null)
                leverAnimator = GetComponentInChildren<Animator>();

            if (leverAnimator == null)
            {
                Debug.LogWarning("[LeverDoorCinematicSwitch] No Animator found.", this);
                return;
            }

            leverAnimator.enabled = true;
            leverAnimator.applyRootMotion = false;
            leverAnimator.speed = Mathf.Max(0.01f, animatorSpeedDuringPull);

            if (!string.IsNullOrEmpty(leverFullStatePath) &&
                leverAnimator.HasState(animatorLayerIndex, Animator.StringToHash(leverFullStatePath)))
            {
                leverAnimator.Play(leverFullStatePath, animatorLayerIndex, 0f);
                leverAnimator.Update(0f);
                Debug.Log($"[LeverDoorCinematicSwitch] Debug Play full path: {leverFullStatePath}", this);
                return;
            }

            if (!string.IsNullOrEmpty(leverStateName) &&
                leverAnimator.HasState(animatorLayerIndex, Animator.StringToHash(leverStateName)))
            {
                leverAnimator.Play(leverStateName, animatorLayerIndex, 0f);
                leverAnimator.Update(0f);
                Debug.Log($"[LeverDoorCinematicSwitch] Debug Play state: {leverStateName}", this);
                return;
            }

            Debug.LogWarning(
                $"[LeverDoorCinematicSwitch] Debug failed. State not found. Tried '{leverFullStatePath}' and '{leverStateName}'.",
                this
            );
        }

        [ContextMenu("DEBUG / Force Restore Player Control")]
        private void DebugForceRestorePlayerControl()
        {
            RestorePlayerControl();
            _running = false;
        }
    }
}