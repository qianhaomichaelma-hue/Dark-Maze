using UnityEngine;
using UnityEngine.Events;

namespace DarkMazeMinimal
{
    public class Bonfire : MonoBehaviour
    {
        public Light fireLit;

        [Header("Bonfire State")]
        [SerializeField] private bool isLit = false;
        [SerializeField] private bool isActivated = false;

        [Header("Starting Checkpoint")]
        [Tooltip("Enable this only for the starting campfire.")]
        [SerializeField] private bool setAsStartingCheckpoint = false;

        [Header("Progress")]
        [Tooltip("If true, this bonfire will notify CampfireProgressController when ignited.")]
        [SerializeField] private bool countsTowardCampfireProgress = true;

        [SerializeField] private CampfireProgressController progressController;

        [Tooltip("If true, the bonfire will auto-find CampfireProgressController if not assigned.")]
        [SerializeField] private bool autoFindProgressController = true;

        [Header("Safe Zone Visuals")]
        [Tooltip("The circular ground mesh / ring object that shows the safe zone range.")]
        [SerializeField] private GameObject safeZoneGroundVisual;

        [Tooltip("Optional parent object containing all safe zone visual effects.")]
        [SerializeField] private GameObject safeZoneVisualRoot;

        [Tooltip("Particle systems that should play only when this bonfire is lit.")]
        [SerializeField] private ParticleSystem[] safeZoneParticles;

        [Tooltip("If true, particles will be cleared when the bonfire is unlit.")]
        [SerializeField] private bool clearParticlesWhenUnlit = true;

        [Tooltip("If true, particles will simulate briefly before playing, so they appear already active.")]
        [SerializeField] private bool prewarmParticlesOnLit = true;

        [SerializeField] private float particlePrewarmTime = 2f;

        [Header("Respawn")]
        [SerializeField] private Transform respawnAnchor;

        [Header("Events")]
        public UnityEvent onIgnited;
        public UnityEvent onActivated;

        [Header("Audio - Ignite One Shot")]
        [SerializeField] private AudioSource igniteAudioSource;
        [SerializeField] private AudioClip igniteSFX;

        [Header("Audio - Fire Loop")]
        [SerializeField] private AudioSource fireLoopAudioSource;
        [SerializeField] private AudioClip fireLoopSFX;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        public bool IsLit => isLit;
        public bool IsActivated => isActivated;

        private void Awake()
        {
            if (progressController == null && autoFindProgressController)
                progressController = FindFirstObjectByType<CampfireProgressController>();
        }

        private void Start()
        {
            Log($"Start | name={name} | isLit={isLit} | isActivated={isActivated}");

            if (respawnAnchor == null)
                Debug.LogWarning($"[Bonfire] respawnAnchor is NULL on {name}", this);

            SetupAudioSources();

            ApplyLitVisualState(isLit);

            if (isLit)
            {
                StartFireLoopSFX();

                if (setAsStartingCheckpoint && GameManager.Instance != null && respawnAnchor != null)
                {
                    isActivated = true;
                    GameManager.Instance.SetRespawnPoint(respawnAnchor);
                    Log($"Starting checkpoint set -> {respawnAnchor.name}");
                }
            }
            else
            {
                StopFireLoopSFX();
            }
        }

        public void TryActivate(PlayerState player)
        {
            Log($"TryActivate called | name={name} | isLit={isLit} | isActivated={isActivated}");

            if (!isLit)
                return;

            if (isActivated)
                return;

            isActivated = true;

            if (GameManager.Instance != null && respawnAnchor != null)
            {
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
                Log($"Activated checkpoint -> {respawnAnchor.name}");
            }

            onActivated?.Invoke();
        }

        public bool TryIgnite()
        {
            Log($"TryIgnite called | name={name} | current isLit={isLit}");

            if (isLit)
                return false;

            isLit = true;
            isActivated = true;

            ApplyLitVisualState(true);

            PlayIgniteSFX();
            StartFireLoopSFX();

            if (GameManager.Instance != null)
            {
                PlayerState playerState = GameManager.Instance.GetPlayerState();

                if (playerState != null)
                    playerState.SetSafeZone(true);
            }

            if (GameManager.Instance != null && respawnAnchor != null)
            {
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
                Log($"SetRespawnPoint -> {respawnAnchor.name}");
            }

            if (countsTowardCampfireProgress && progressController != null)
            {
                progressController.NotifyBonfireIgnited(this);
            }
            else if (countsTowardCampfireProgress && progressController == null)
            {
                Debug.LogWarning($"[Bonfire] countsTowardCampfireProgress is true, but progressController is NULL on {name}.", this);
            }

            onIgnited?.Invoke();

            Log($"Ignited SUCCESS | name={name} | frame={Time.frameCount}");

            return true;
        }

        private void ApplyLitVisualState(bool lit)
        {
            if (fireLit != null)
            {
                fireLit.enabled = lit;
            }
            else
            {
                Debug.LogWarning($"[Bonfire] fireLit is NULL on {name}", this);
            }

            if (safeZoneVisualRoot != null)
                safeZoneVisualRoot.SetActive(lit);

            if (safeZoneGroundVisual != null)
                safeZoneGroundVisual.SetActive(lit);

            SetSafeZoneParticles(lit);
        }

        private void SetSafeZoneParticles(bool lit)
        {
            if (safeZoneParticles == null || safeZoneParticles.Length == 0)
                return;

            for (int i = 0; i < safeZoneParticles.Length; i++)
            {
                ParticleSystem ps = safeZoneParticles[i];

                if (ps == null)
                    continue;

                GameObject psObject = ps.gameObject;

                if (lit)
                {
                    if (!psObject.activeSelf)
                        psObject.SetActive(true);

                    if (prewarmParticlesOnLit && particlePrewarmTime > 0f)
                    {
                        ps.Clear(true);
                        ps.Simulate(particlePrewarmTime, true, true);
                    }

                    ps.Play(true);
                }
                else
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                    if (clearParticlesWhenUnlit)
                        ps.Clear(true);

                    psObject.SetActive(false);
                }
            }
        }

        private void SetupAudioSources()
        {
            if (igniteAudioSource != null)
            {
                igniteAudioSource.playOnAwake = false;
                igniteAudioSource.loop = false;
            }

            if (fireLoopAudioSource != null)
            {
                fireLoopAudioSource.playOnAwake = false;
                fireLoopAudioSource.loop = true;

                if (fireLoopSFX != null)
                    fireLoopAudioSource.clip = fireLoopSFX;
            }
        }

        private void PlayIgniteSFX()
        {
            if (igniteAudioSource == null || igniteSFX == null)
                return;

            igniteAudioSource.PlayOneShot(igniteSFX);
        }

        private void StartFireLoopSFX()
        {
            if (fireLoopAudioSource == null)
                return;

            if (fireLoopSFX != null)
                fireLoopAudioSource.clip = fireLoopSFX;

            if (fireLoopAudioSource.clip == null)
                return;

            if (!fireLoopAudioSource.isPlaying)
                fireLoopAudioSource.Play();
        }

        private void StopFireLoopSFX()
        {
            if (fireLoopAudioSource == null)
                return;

            if (fireLoopAudioSource.isPlaying)
                fireLoopAudioSource.Stop();
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[Bonfire] {message}", this);
        }
    }
}