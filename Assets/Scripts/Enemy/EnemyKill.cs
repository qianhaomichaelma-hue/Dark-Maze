using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class EnemyKill : MonoBehaviour
    {
        [Header("Kill Rule")]
        [SerializeField] private bool requirePlayerTag = true;

        [Header("Audio - Kill Player")]
        [SerializeField] private AudioClip killPlayerSFX;

        [Range(0f, 1f)]
        [SerializeField] private float killPlayerVolume = 1f;

        [Tooltip("Recommended true. 2D sound will always be heard clearly.")]
        [SerializeField] private bool playAs2D = true;

        [Header("Safety")]
        [SerializeField] private float killCooldown = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private float _lastKillTime = -999f;

        private void OnTriggerEnter(Collider other)
        {
            TryKillPlayer(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryKillPlayer(other);
        }

        private void TryKillPlayer(Collider other)
        {
            if (other == null)
                return;

            if (Time.time - _lastKillTime < killCooldown)
                return;

            if (requirePlayerTag && !other.CompareTag("Player"))
                return;

            PlayerState ps = other.GetComponent<PlayerState>();

            if (ps == null)
                ps = other.GetComponentInParent<PlayerState>();

            if (ps == null)
                return;

            if (ps.IsDead)
                return;

            _lastKillTime = Time.time;

            PlayKillPlayerSFX(other.ClosestPoint(transform.position));

            if (debugLogs)
                Debug.Log($"[EnemyKill] Kill player via {other.name}", this);

            ps.Kill();
        }

        private void PlayKillPlayerSFX(Vector3 position)
        {
            if (killPlayerSFX == null)
                return;

            if (playAs2D)
            {
                GameObject audioGO = new GameObject("Enemy_KillPlayerSFX_2D");
                AudioSource source = audioGO.AddComponent<AudioSource>();

                source.clip = killPlayerSFX;
                source.volume = killPlayerVolume;
                source.spatialBlend = 0f;
                source.playOnAwake = false;
                source.loop = false;
                source.dopplerLevel = 0f;

                source.Play();

                Destroy(audioGO, killPlayerSFX.length + 0.1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(killPlayerSFX, position, killPlayerVolume);
            }
        }
    }
}