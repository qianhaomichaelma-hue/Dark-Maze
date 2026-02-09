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

        public bool IsLit => isLit;

        private void Start()
        {
            Debug.Log($"[Bonfire] Start | name={name} | isLit={isLit} | isActivated={isActivated}", this);
            if (respawnAnchor == null)
                Debug.LogWarning($"[Bonfire] respawnAnchor is NULL on {name}", this);

            // initialize the camp fire
            fireLit.enabled = isLit;
        }

        public void TryActivate(PlayerState player)
        {
            Debug.Log($"[Bonfire] TryActivate called | name={name} | isLit={isLit} | isActivated={isActivated}", this);

            if (!isLit) return;
            if (isActivated) return;

            isActivated = true;
            Debug.Log($"[Bonfire] Activated checkpoint | name={name}", this);

            if (GameManager.Instance != null && respawnAnchor != null)
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
        }

        public bool TryIgnite()
        {
            Debug.Log($"[Bonfire] TryIgnite called | name={name} | current isLit={isLit}", this);

            if (isLit)
            {
                Debug.Log($"[Bonfire] Already lit, ignore | name={name}", this);
                return false;
            }

            isLit = true;
            // ignite at the same time light up the camp fire
            fireLit.enabled = true;
            Debug.Log($"[Bonfire] Ignited SUCCESS | name={name} | frame={Time.frameCount}", this);

            if (GameManager.Instance != null && respawnAnchor != null)
            {
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
                Debug.Log($"[Bonfire] SetRespawnPoint -> {respawnAnchor.name}", this);
            }

            return true;
        }
    }
}
