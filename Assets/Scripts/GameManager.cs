using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace DarkMazeMinimal
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerState player;

        [Header("Respawn")]
        [SerializeField] private Transform currentRespawnPoint;
        [SerializeField] private float respawnDelay = 0.6f;

        private bool _respawning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPlayer(PlayerState p)
        {
            player = p;
        }

        public PlayerState GetPlayerState()
        {
            return player;
        }

        public void SetRespawnPoint(Transform point)
        {
            if (point == null) return;
            currentRespawnPoint = point;
        }

        public void RequestRespawn()
        {
            if (_respawning) return;
            if (player == null) return;
            if (currentRespawnPoint == null)
            {
                Debug.LogWarning("[GameManager] No respawn point set yet.");
                return;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _respawning = true;

            yield return new WaitForSeconds(respawnDelay);

            player.ReviveAt(currentRespawnPoint);

            _respawning = false;
        }
    }
}
