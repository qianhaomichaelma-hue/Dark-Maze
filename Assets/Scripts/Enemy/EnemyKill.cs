using UnityEngine;

namespace DarkMazeMinimal
{
    public class EnemyKill : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var ps = other.GetComponent<PlayerState>();
            if (ps != null && !ps.IsDead)
                ps.Kill();
        }
    }
}
