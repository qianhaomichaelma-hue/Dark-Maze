using UnityEngine;
using UnityEngine.AI;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyChaser : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerState player;
        [SerializeField] private Transform homePoint;

        [Header("Update")]
        [SerializeField] private float updateRate = 0.1f;

        [Header("Chase Rules")]
        [SerializeField] private float giveUpDistance = 60f;

        [Header("Lure Rules")]
        [SerializeField] private float lureMaxConsiderDistance = 25f; // Only chase lures within this distance

        private NavMeshAgent _agent;
        private float _timer;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (player == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) player = playerGO.GetComponent<PlayerState>();
            }

            if (homePoint == null)
                Debug.LogWarning($"[EnemyChaser] homePoint is NULL on {name}", this);

            Debug.Log($"[EnemyChaser] Start | player={(player ? player.name : "NULL")} | home={(homePoint ? homePoint.name : "NULL")}", this);
        }

        private void Update()
        {
            if (player == null || homePoint == null) return;

            _timer += Time.deltaTime;
            if (_timer < updateRate) return;
            _timer = 0f;

            if (player.IsDead)
            {
                GoHome("player dead");
                return;
            }

            if (player.IsInSafeZone)
            {
                GoHome("player in safe zone");
                return;
            }

            // Optional distance safety
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer > giveUpDistance)
            {
                GoHome("player too far");
                return;
            }

            // 1) Try lure first
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                _agent.isStopped = false;
                _agent.SetDestination(lurePos);
                Debug.Log($"[EnemyChaser] Chasing LURE at {lurePos}", this);
                return;
            }

            // 2) Otherwise chase player
            _agent.isStopped = false;
            _agent.SetDestination(player.transform.position);
        }

        private bool TryGetNearestLure(out Vector3 lurePos)
        {
            lurePos = default;

            var lures = LureZone.Active;
            if (lures == null || lures.Count == 0) return false;

            float best = float.MaxValue;
            LureZone bestLure = null;

            for (int i = 0; i < lures.Count; i++)
            {
                var l = lures[i];
                if (l == null) continue;

                float d = Vector3.Distance(transform.position, l.transform.position);

                if (d > lureMaxConsiderDistance) continue;
                if (d < best)
                {
                    best = d;
                    bestLure = l;
                }
            }

            if (bestLure == null) return false;

            lurePos = bestLure.transform.position;
            return true;
        }

        private void GoHome(string reason)
        {
            _agent.isStopped = false;
            _agent.SetDestination(homePoint.position);
            // Debug.Log($"[EnemyChaser] GoHome ({reason})", this);
        }

        // Put this script on the enemy root OR on a child kill trigger (avoid duplicates)
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var ps = other.GetComponent<PlayerState>();
            if (ps != null && !ps.IsDead)
            {
                ps.Kill();
            }
        }
    }
}
