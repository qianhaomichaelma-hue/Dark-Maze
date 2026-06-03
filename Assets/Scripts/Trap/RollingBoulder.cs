using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class RollingBoulder : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private bool killPlayerOnTouch = true;

        [Tooltip("The trigger child object used only for killing the player. It will be destroyed after the boulder finishes.")]
        [SerializeField] private GameObject killTriggerObject;

        [Tooltip("If true, the kill trigger object is destroyed after the boulder reaches the end zone.")]
        [SerializeField] private bool destroyKillTriggerWhenFinished = true;

        [Tooltip("If true, root collision will also stop killing the player after the boulder finishes.")]
        [SerializeField] private bool disableRootDamageWhenFinished = true;

        [Header("Audio - Sources")]
        [SerializeField] private AudioSource eventAudioSource;
        [SerializeField] private AudioSource rollingLoopSource;

        [Header("Audio - Release")]
        [SerializeField] private AudioClip releaseSFX;

        [Range(0f, 1f)]
        [SerializeField] private float releaseVolume = 1f;

        [Header("Audio - Rolling Loop")]
        [SerializeField] private AudioClip rollingLoopSFX;

        [Tooltip("Delay after Release before the rolling loop starts.")]
        [SerializeField] private float rollingLoopStartDelay = 0.8f;

        [Tooltip("Fixed rolling loop volume. This version does NOT scale volume by speed.")]
        [Range(0f, 1f)]
        [SerializeField] private float rollingLoopVolume = 0.9f;

        [Tooltip("Fixed rolling loop pitch.")]
        [SerializeField] private float rollingLoopPitch = 1f;

        [Tooltip("If true, rolling sound is 2D and always audible. Recommended for gameplay clarity.")]
        [SerializeField] private bool rollingLoopAs2D = true;

        [Tooltip("Only used if Rolling Loop As 2D is false.")]
        [SerializeField] private float rollingLoopMinDistance = 8f;

        [Tooltip("Only used if Rolling Loop As 2D is false.")]
        [SerializeField] private float rollingLoopMaxDistance = 60f;

        [Header("Audio - Player Kill")]
        [SerializeField] private AudioClip playerKillSFX;

        [Range(0f, 1f)]
        [SerializeField] private float playerKillVolume = 1f;

        [Tooltip("If true, the kill sound is played as 2D feedback so the player can always hear it.")]
        [SerializeField] private bool playerKillSFXAs2D = true;

        [Header("Audio - End Impact")]
        [SerializeField] private AudioClip endImpactSFX;

        [Range(0f, 1f)]
        [SerializeField] private float endImpactVolume = 1f;

        [Header("Camera Shake - Rolling")]
        [SerializeField] private bool shakeCameraWhileRolling = true;
        [SerializeField] private float rollingShakeInterval = 0.12f;
        [SerializeField] private float rollingShakeDuration = 0.06f;
        [SerializeField] private float rollingShakeStrength = 0.035f;
        [SerializeField] private float rollingShakeMinSpeed = 1.5f;

        [Header("Camera Shake - End Impact")]
        [SerializeField] private bool shakeCameraOnEndImpact = true;
        [SerializeField] private float endImpactShakeDuration = 0.35f;
        [SerializeField] private float endImpactShakeStrength = 0.18f;

        [Header("State Debug")]
        [SerializeField] private bool debugLogs = true;

        private RollingBoulderTrap _ownerTrap;
        private Rigidbody _rb;

        private bool _isReleased;
        private bool _hasKilledPlayer;
        private bool _hasFinished;
        private bool _rollingLoopAllowed;

        private float _nextRollingShakeTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            AutoFindKillTrigger();
            SetupAudioSources();
        }

        private void Update()
        {
            if (!_isReleased || _hasKilledPlayer || _hasFinished)
                return;

            if (_rollingLoopAllowed)
                KeepRollingLoopAudible();

            float speed = GetRigidbodySpeed();
            UpdateRollingCameraShake(speed);
        }

        public void Initialize(RollingBoulderTrap ownerTrap)
        {
            _ownerTrap = ownerTrap;

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            AutoFindKillTrigger();
            SetupAudioSources();

            Log("Initialized.");
        }

        public void ResetToHeldState(Vector3 heldPosition, Quaternion heldRotation)
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            gameObject.SetActive(true);

            _isReleased = false;
            _hasKilledPlayer = false;
            _hasFinished = false;
            _rollingLoopAllowed = false;

            if (killTriggerObject != null)
                killTriggerObject.SetActive(true);

            killPlayerOnTouch = true;

            StopRollingLoopImmediately();

            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
                ClearVelocity();
            }

            transform.position = heldPosition;
            transform.rotation = heldRotation;

            Log($"Reset to held state at {heldPosition}.");
        }

        public void Release()
        {
            if (_hasFinished)
                return;

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            _isReleased = true;
            _hasKilledPlayer = false;
            _rollingLoopAllowed = false;
            _nextRollingShakeTime = Time.time;

            if (killTriggerObject != null)
                killTriggerObject.SetActive(true);

            killPlayerOnTouch = true;

            if (_rb != null)
            {
                ClearVelocity();

                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.WakeUp();
            }

            PlayReleaseSFX();
            StartRollingLoopAfterDelay();

            Log($"Released. Gravity enabled. Rolling loop will start after {rollingLoopStartDelay:F2}s.");
        }

        public void ApplyInitialImpulse(Vector3 direction, float impulse)
        {
            if (!_isReleased)
                return;

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            if (_rb == null)
                return;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;

            direction.Normalize();

            _rb.AddForce(direction * impulse, ForceMode.Impulse);

            Log($"Initial impulse applied. direction={direction}, impulse={impulse}");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
                return;

            TryKillPlayerFromCollider(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryKillPlayerFromCollider(other);
        }

        public void TryKillPlayerFromCollider(Collider other)
        {
            if (!killPlayerOnTouch)
                return;

            if (!_isReleased)
                return;

            if (_hasKilledPlayer || _hasFinished)
                return;

            if (other == null)
                return;

            PlayerState player = other.GetComponentInParent<PlayerState>();
            if (player == null)
                return;

            if (player.IsDead)
                return;

            _hasKilledPlayer = true;

            StopRollingLoopImmediately();

            Vector3 impactPosition = other.ClosestPoint(transform.position);
            PlayPlayerKillSFX(impactPosition);

            Log($"Killed player via {other.name}.");

            if (_ownerTrap != null)
                _ownerTrap.NotifyPlayerKilledByBoulder(this);

            player.Kill();
        }

        public void NotifyReachedEndZone()
        {
            if (!_isReleased)
                return;

            if (_hasKilledPlayer || _hasFinished)
                return;

            _hasFinished = true;

            StopRollingLoopImmediately();

            PlayEndImpactSFX();
            PlayEndImpactCameraShake();

            Log("Reached end zone.");

            if (_ownerTrap != null)
                _ownerTrap.NotifyBoulderFinished(this);
            else
                StopAndDisableDamage();
        }

        public void StopAndDisableDamage()
        {
            _isReleased = false;
            _hasFinished = true;
            _rollingLoopAllowed = false;

            if (disableRootDamageWhenFinished)
                killPlayerOnTouch = false;

            StopRollingLoopImmediately();

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            if (_rb != null)
            {
                ClearVelocity();

                _rb.useGravity = false;
                _rb.isKinematic = true;
            }

            RemoveKillTriggerAfterFinish();

            Log("Stopped, damage disabled, kill trigger removed.");
        }

        private void RemoveKillTriggerAfterFinish()
        {
            if (!destroyKillTriggerWhenFinished)
            {
                if (killTriggerObject != null)
                    killTriggerObject.SetActive(false);

                return;
            }

            if (killTriggerObject == null)
                AutoFindKillTrigger();

            if (killTriggerObject != null)
            {
                Log($"Destroying kill trigger: {killTriggerObject.name}");
                Destroy(killTriggerObject);
                killTriggerObject = null;
            }
        }

        private void AutoFindKillTrigger()
        {
            if (killTriggerObject != null)
                return;

            Transform killTrigger = transform.Find("KillTrigger");
            if (killTrigger != null)
            {
                killTriggerObject = killTrigger.gameObject;
                return;
            }

            RollingBoulderDamageRelay relay = GetComponentInChildren<RollingBoulderDamageRelay>(true);
            if (relay != null)
                killTriggerObject = relay.gameObject;
        }

        private void SetupAudioSources()
        {
            if (eventAudioSource == null)
            {
                eventAudioSource = GetComponent<AudioSource>();

                if (eventAudioSource == null)
                    eventAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (eventAudioSource != null)
            {
                eventAudioSource.playOnAwake = false;
                eventAudioSource.loop = false;
                eventAudioSource.spatialBlend = 1f;
                eventAudioSource.minDistance = 8f;
                eventAudioSource.maxDistance = 60f;
                eventAudioSource.rolloffMode = AudioRolloffMode.Linear;
                eventAudioSource.dopplerLevel = 0.1f;
            }

            if (rollingLoopSource == null)
            {
                Transform existing = transform.Find("RollingLoopAudioSource");

                if (existing != null)
                    rollingLoopSource = existing.GetComponent<AudioSource>();

                if (rollingLoopSource == null)
                {
                    GameObject loopAudioGO = new GameObject("RollingLoopAudioSource");
                    loopAudioGO.transform.SetParent(transform);
                    loopAudioGO.transform.localPosition = Vector3.zero;
                    loopAudioGO.transform.localRotation = Quaternion.identity;
                    rollingLoopSource = loopAudioGO.AddComponent<AudioSource>();
                }
            }

            ConfigureRollingLoopSource();
        }

        private void ConfigureRollingLoopSource()
        {
            if (rollingLoopSource == null)
                return;

            rollingLoopSource.playOnAwake = false;
            rollingLoopSource.loop = true;
            rollingLoopSource.dopplerLevel = 0f;
            rollingLoopSource.volume = Mathf.Clamp01(rollingLoopVolume);
            rollingLoopSource.pitch = Mathf.Max(0.01f, rollingLoopPitch);

            if (rollingLoopAs2D)
            {
                rollingLoopSource.spatialBlend = 0f;
            }
            else
            {
                rollingLoopSource.spatialBlend = 1f;
                rollingLoopSource.minDistance = rollingLoopMinDistance;
                rollingLoopSource.maxDistance = rollingLoopMaxDistance;
                rollingLoopSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (rollingLoopSFX != null)
                rollingLoopSource.clip = rollingLoopSFX;
        }

        private void PlayReleaseSFX()
        {
            if (eventAudioSource == null || releaseSFX == null)
                return;

            eventAudioSource.PlayOneShot(releaseSFX, releaseVolume);
        }

        private void PlayPlayerKillSFX(Vector3 impactPosition)
        {
            if (playerKillSFX == null)
                return;

            if (playerKillSFXAs2D)
            {
                GameObject audioGO = new GameObject("Boulder_PlayerKillSFX_2D");
                AudioSource source = audioGO.AddComponent<AudioSource>();

                source.clip = playerKillSFX;
                source.volume = playerKillVolume;
                source.spatialBlend = 0f;
                source.playOnAwake = false;
                source.loop = false;

                source.Play();

                Destroy(audioGO, playerKillSFX.length + 0.1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(playerKillSFX, impactPosition, playerKillVolume);
            }
        }

        private void PlayEndImpactSFX()
        {
            if (eventAudioSource == null || endImpactSFX == null)
                return;

            eventAudioSource.PlayOneShot(endImpactSFX, endImpactVolume);
        }

        private void StartRollingLoopAfterDelay()
        {
            CancelInvoke(nameof(EnableRollingLoop));

            _rollingLoopAllowed = false;

            if (rollingLoopStartDelay <= 0f)
            {
                EnableRollingLoop();
            }
            else
            {
                Invoke(nameof(EnableRollingLoop), rollingLoopStartDelay);
            }
        }

        private void EnableRollingLoop()
        {
            if (!_isReleased || _hasKilledPlayer || _hasFinished)
                return;

            _rollingLoopAllowed = true;
            StartRollingLoopFixed();
        }

        private void StartRollingLoopFixed()
        {
            if (rollingLoopSource == null || rollingLoopSFX == null)
            {
                Log("Rolling loop not started. Source or clip missing.");
                return;
            }

            ConfigureRollingLoopSource();

            rollingLoopSource.clip = rollingLoopSFX;
            rollingLoopSource.volume = Mathf.Clamp01(rollingLoopVolume);
            rollingLoopSource.pitch = Mathf.Max(0.01f, rollingLoopPitch);

            if (!rollingLoopSource.isPlaying)
                rollingLoopSource.Play();

            Log($"Rolling loop playing. volume={rollingLoopSource.volume}, spatialBlend={rollingLoopSource.spatialBlend}");
        }

        private void KeepRollingLoopAudible()
        {
            if (rollingLoopSource == null || rollingLoopSFX == null)
                return;

            if (!rollingLoopSource.isPlaying)
                rollingLoopSource.Play();

            rollingLoopSource.volume = Mathf.Clamp01(rollingLoopVolume);
            rollingLoopSource.pitch = Mathf.Max(0.01f, rollingLoopPitch);
        }

        private void StopRollingLoopImmediately()
        {
            CancelInvoke(nameof(EnableRollingLoop));

            _rollingLoopAllowed = false;

            if (rollingLoopSource == null)
                return;

            rollingLoopSource.Stop();
            rollingLoopSource.volume = Mathf.Clamp01(rollingLoopVolume);
        }

        private void UpdateRollingCameraShake(float speed)
        {
            if (!shakeCameraWhileRolling)
                return;

            if (CameraShakeTarget.Instance == null)
                return;

            if (speed < rollingShakeMinSpeed)
                return;

            if (Time.time < _nextRollingShakeTime)
                return;

            _nextRollingShakeTime = Time.time + Mathf.Max(0.02f, rollingShakeInterval);

            CameraShakeTarget.Instance.Shake(
                rollingShakeDuration,
                rollingShakeStrength
            );
        }

        private void PlayEndImpactCameraShake()
        {
            if (!shakeCameraOnEndImpact)
                return;

            if (CameraShakeTarget.Instance == null)
                return;

            CameraShakeTarget.Instance.Shake(
                endImpactShakeDuration,
                endImpactShakeStrength
            );
        }

        private float GetRigidbodySpeed()
        {
            if (_rb == null)
                return 0f;

#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity.magnitude;
#else
            return _rb.velocity.magnitude;
#endif
        }

        private void ClearVelocity()
        {
            if (_rb == null)
                return;

#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector3.zero;
#else
            _rb.velocity = Vector3.zero;
#endif

            _rb.angularVelocity = Vector3.zero;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RollingBoulder] {message}", this);
        }
    }
}