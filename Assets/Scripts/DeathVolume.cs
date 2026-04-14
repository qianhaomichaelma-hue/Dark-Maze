using UnityEngine;

namespace DarkMazeMinimal
{
    public class DeathVolume : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logTrigger = false;

        private void OnTriggerEnter(Collider other)
        {
            TryKillPlayer(other, "Enter");
        }

        private void OnTriggerStay(Collider other)
        {
            TryKillPlayer(other, "Stay");
        }

        private void TryKillPlayer(Collider other, string phase)
        {
            if (other == null) return;

            PlayerState ps = other.GetComponentInParent<PlayerState>();
            if (ps == null)
            {
                if (logTrigger)
                    Debug.Log($"[DeathVolume] {phase} ignored: no PlayerState on {other.name}", this);
                return;
            }

            if (ps.IsDead) return;

            if (logTrigger)
                Debug.Log($"[DeathVolume] {phase} -> Kill player via {other.name}", this);

            ps.Kill();
        }
    }
}