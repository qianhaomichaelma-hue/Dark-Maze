using System.Collections;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class RemoteSlidingDoor : MonoBehaviour
    {
        [Header("Door Movement")]
        [SerializeField] private Transform doorMover;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(3f, 0f, 0f);
        [SerializeField] private float openDuration = 2.0f;
        [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Door Light")]
        [SerializeField] private GameObject[] lightObjectsToEnable;
        [SerializeField] private Light[] lightsToEnable;
        [SerializeField] private bool lightsOffOnStart = true;

        [Header("Audio - 2D")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("2D audio means the door sounds will not become quieter because of camera distance.")]
        [SerializeField] private bool force2DAudio = true;

        [SerializeField] private AudioClip lightOnSFX;
        [Range(0f, 1f)]
        [SerializeField] private float lightOnVolume = 1f;

        [SerializeField] private AudioClip doorMoveSFX;
        [Range(0f, 1f)]
        [SerializeField] private float doorMoveVolume = 1f;

        [SerializeField] private AudioClip doorStopSFX;
        [Range(0f, 1f)]
        [SerializeField] private float doorStopVolume = 1f;

        [Header("Audio - Dramatic After Open")]
        [SerializeField] private AudioClip dramaticAfterOpenSFX;

        [Range(0f, 1f)]
        [SerializeField] private float dramaticAfterOpenVolume = 1f;

        [Tooltip("Extra delay after Door Stop SFX finishes, before dramatic sound plays.")]
        [SerializeField] private float dramaticAfterOpenExtraDelay = 0.1f;

        [Tooltip("If true, OpenDoorRoutine waits for the dramatic sound to finish before returning to the lever cinematic script.")]
        [SerializeField] private bool waitForDramaticSFXToFinish = false;

        [Header("Direct Camera Shake")]
        [Tooltip("Drag DoorCinematicCamera_ShakeRoot here. This should be the parent of DoorCinematicCamera.")]
        [SerializeField] private Transform directCameraShakeTarget;

        [SerializeField] private bool useDirectCameraShake = true;

        [Tooltip("How often the shake offset updates while the door is moving.")]
        [SerializeField] private float directShakeInterval = 0.018f;

        [Tooltip("Small position shake while the door is moving.")]
        [SerializeField] private Vector3 directPositionStrength = new Vector3(0.025f, 0.018f, 0.025f);

        [Tooltip("Small rotation shake while the door is moving.")]
        [SerializeField] private Vector3 directRotationStrength = new Vector3(0.25f, 0.20f, 0.15f);

        [Tooltip("If true, shake fades out near the end of the door movement.")]
        [SerializeField] private bool fadeShakeOutNearEnd = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;

        private bool _lightsOn;
        private bool _isOpen;
        private bool _isOpening;

        private float _nextDirectShakeTime;

        private Vector3 _directShakeBaseLocalPosition;
        private Quaternion _directShakeBaseLocalRotation;
        private bool _hasDirectShakeBase;

        public bool IsOpen => _isOpen;
        public bool IsOpening => _isOpening;

        private void Awake()
        {
            if (doorMover == null)
                doorMover = transform;

            _closedLocalPosition = doorMover.localPosition;
            _openLocalPosition = _closedLocalPosition + openLocalOffset;

            SetupAudioSource();
            CacheDirectShakeBase();

            if (lightsOffOnStart)
                SetDoorLights(false);
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
                audioSource.minDistance = 6f;
                audioSource.maxDistance = 45f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        public void TurnOnDoorLights()
        {
            if (_lightsOn)
                return;

            _lightsOn = true;

            SetDoorLights(true);
            PlayOneShot(lightOnSFX, lightOnVolume);

            Log("Door lights turned on.");
        }

        public void OpenDoor()
        {
            if (_isOpen || _isOpening)
                return;

            StartCoroutine(OpenDoorRoutine());
        }

        public IEnumerator OpenDoorRoutine()
        {
            if (_isOpen || _isOpening)
                yield break;

            _isOpening = true;
            _nextDirectShakeTime = Time.time;

            CacheDirectShakeBase();
            RestoreDirectShakeTarget();

            PlayOneShot(doorMoveSFX, doorMoveVolume);

            float timer = 0f;
            float duration = Mathf.Max(0.01f, openDuration);

            Vector3 startPos = doorMover.localPosition;
            Vector3 targetPos = _openLocalPosition;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                float t = Mathf.Clamp01(timer / duration);
                float curvedT = openCurve != null ? openCurve.Evaluate(t) : t;

                doorMover.localPosition = Vector3.LerpUnclamped(startPos, targetPos, curvedT);

                UpdateDirectCameraShake(t);

                yield return null;
            }

            doorMover.localPosition = targetPos;

            RestoreDirectShakeTarget();

            _isOpening = false;
            _isOpen = true;

            PlayOneShot(doorStopSFX, doorStopVolume);

            yield return StartCoroutine(PlayDramaticAfterOpenRoutine());

            Log("Door opened.");
        }

        private IEnumerator PlayDramaticAfterOpenRoutine()
        {
            if (dramaticAfterOpenSFX == null)
                yield break;

            if (doorStopSFX != null)
                yield return new WaitForSeconds(doorStopSFX.length);

            if (dramaticAfterOpenExtraDelay > 0f)
                yield return new WaitForSeconds(dramaticAfterOpenExtraDelay);

            PlayOneShot(dramaticAfterOpenSFX, dramaticAfterOpenVolume);

            if (waitForDramaticSFXToFinish)
                yield return new WaitForSeconds(dramaticAfterOpenSFX.length);
        }

        public void ResetDoorClosed()
        {
            StopAllCoroutines();

            RestoreDirectShakeTarget();

            if (doorMover == null)
                doorMover = transform;

            doorMover.localPosition = _closedLocalPosition;

            _isOpen = false;
            _isOpening = false;
            _lightsOn = false;

            SetDoorLights(false);

            Log("Door reset closed.");
        }

        private void SetDoorLights(bool visible)
        {
            if (lightObjectsToEnable != null)
            {
                for (int i = 0; i < lightObjectsToEnable.Length; i++)
                {
                    if (lightObjectsToEnable[i] != null)
                        lightObjectsToEnable[i].SetActive(visible);
                }
            }

            if (lightsToEnable != null)
            {
                for (int i = 0; i < lightsToEnable.Length; i++)
                {
                    if (lightsToEnable[i] != null)
                        lightsToEnable[i].enabled = visible;
                }
            }
        }

        private void UpdateDirectCameraShake(float openProgress01)
        {
            if (!useDirectCameraShake)
                return;

            if (directCameraShakeTarget == null)
                return;

            if (Time.time < _nextDirectShakeTime)
                return;

            _nextDirectShakeTime = Time.time + Mathf.Max(0.01f, directShakeInterval);

            CacheDirectShakeBase();

            float strengthMultiplier = 1f;

            if (fadeShakeOutNearEnd)
                strengthMultiplier = 1f - Mathf.Clamp01(openProgress01);

            Vector3 posOffset = new Vector3(
                Random.Range(-directPositionStrength.x, directPositionStrength.x),
                Random.Range(-directPositionStrength.y, directPositionStrength.y),
                Random.Range(-directPositionStrength.z, directPositionStrength.z)
            ) * strengthMultiplier;

            Vector3 rotOffset = new Vector3(
                Random.Range(-directRotationStrength.x, directRotationStrength.x),
                Random.Range(-directRotationStrength.y, directRotationStrength.y),
                Random.Range(-directRotationStrength.z, directRotationStrength.z)
            ) * strengthMultiplier;

            directCameraShakeTarget.localPosition = _directShakeBaseLocalPosition + posOffset;
            directCameraShakeTarget.localRotation = _directShakeBaseLocalRotation * Quaternion.Euler(rotOffset);
        }

        private void CacheDirectShakeBase()
        {
            if (directCameraShakeTarget == null)
                return;

            if (_hasDirectShakeBase)
                return;

            _directShakeBaseLocalPosition = directCameraShakeTarget.localPosition;
            _directShakeBaseLocalRotation = directCameraShakeTarget.localRotation;
            _hasDirectShakeBase = true;
        }

        private void RestoreDirectShakeTarget()
        {
            if (directCameraShakeTarget == null)
                return;

            if (!_hasDirectShakeBase)
                return;

            directCameraShakeTarget.localPosition = _directShakeBaseLocalPosition;
            directCameraShakeTarget.localRotation = _directShakeBaseLocalRotation;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (audioSource == null || clip == null)
                return;

            if (force2DAudio)
                audioSource.spatialBlend = 0f;

            audioSource.PlayOneShot(clip, volume);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RemoteSlidingDoor] {message}", this);
        }
    }
}