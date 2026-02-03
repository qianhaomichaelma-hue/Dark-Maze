using UnityEngine;

namespace DarkMazeMinimal
{
    public class Bonfire : MonoBehaviour
    {
        [Header("Bonfire State")]
        [SerializeField] private bool isLit = true;        // future: thrown torch to light
        [SerializeField] private bool isActivated = false; // checkpoint

        [Header("Respawn")]
        [SerializeField] private Transform respawnAnchor;

        public void TryActivate(PlayerState player)
        {
            if (!isLit) return;
            if (isActivated) return;

            isActivated = true;

            if (GameManager.Instance != null && respawnAnchor != null)
                GameManager.Instance.SetRespawnPoint(respawnAnchor);
        }
    }
}

