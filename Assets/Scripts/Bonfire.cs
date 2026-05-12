using UnityEngine;

namespace DarkMazeMinimal
{
    public class Bonfire : MonoBehaviour
    {
        public Light fireLit;

        [Header("Bonfire State")]
        [SerializeField] private bool isLit = false;
        [SerializeField] private bool isActivated = false;

        [Header("Respawn")]
        [SerializeField] private Transform respawnAnchor;

        [Header("Audio - Ignite One Shot")]
        [SerializeField] private AudioSource igniteAudioSource;
        [SerializeField] private AudioClip igniteSFX;

        [Header("Audio - Fire Loop")]
        [SerializeField] private AudioSource fireLoopAudioSource;
        [SerializeField] private AudioClip fireLoopSFX;

        public bool IsLit => isLit;

        private void Start()
        {
            Debug.Log($"[Bonfire] Start | name={name} | isLit={isLit} | isActivated={isActivated}", this);

            if (respawnAnchor == null)
                Debug.LogWarning($"[Bonfire] respawnAnchor is NULL on {name}", this);

            if (fireLit != null)
                fireLit.enabled = isLit;
            else
                Debug.LogWarning($"[Bonfire] fireLit is NULL on {name}", this);

            SetupAudioSources();

            if (isLit)
            {
                StartFireLoopSFX();
            }
            else
            {
                StopFireLoopSFX();
            }
        }

        public void TryActivate(PlayerState player)
        {
            Debug.Log($"[Bonfire] TryActivate called | name={name} | isLit={isLit} | isActivated={isActivated}", this);

            if (!isLit) return;
            if (isActivated) return;

            isActivated = true;

            Debug.Log($"[Bonfire] Activated checkpoint | name={name}", this);

            if (GameManager.Instance != null && respawnAnchor != null)
            {
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
            }
        }

        public bool TryIgnite()
        {
            Debug.Log($"[Bonfire] TryIgnite called | name={name} | current isLit={isLit}", this);

            if (isLit)
            {
                return false;
            }

            isLit = true;

            // Light up the campfire
            if (fireLit != null)
                fireLit.enabled = true;

            // Play ignite sound once
            PlayIgniteSFX();

            // Start looping fire sound
            StartFireLoopSFX();

            // Set the player state to safe
            if (GameManager.Instance != null)
            {
                PlayerState playerState = GameManager.Instance.GetPlayerState();

                if (playerState != null)
                    playerState.SetSafeZone(true);
            }

            Debug.Log($"[Bonfire] Ignited SUCCESS | name={name} | frame={Time.frameCount}", this);

            if (GameManager.Instance != null && respawnAnchor != null)
            {
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
                Debug.Log($"[Bonfire] SetRespawnPoint -> {respawnAnchor.name}", this);
            }

            return true;
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
    }
}