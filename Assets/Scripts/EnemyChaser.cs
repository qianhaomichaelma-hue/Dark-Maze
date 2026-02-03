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

        [Header("Chase")]
        [SerializeField] private float updateRate = 0.1f;
        [SerializeField] private float giveUpDistance = 60f; // optional safety

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
        }

        private void Update()
        {
            if (player == null || homePoint == null) return;

            _timer += Time.deltaTime;
            if (_timer < updateRate) return;
            _timer = 0f;

            if (player.IsDead)
            {
                GoHome();
                return;
            }

            if (player.IsInSafeZone)
            {
                // Safety rule: player in safe zone => enemy retreats
                GoHome();
                return;
            }

            // Optional: if player too far, enemy goes home (prevents weird chasing across map)
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > giveUpDistance)
            {
                GoHome();
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(player.transform.position);
        }

        private void GoHome()
        {
            _agent.isStopped = false;
            _agent.SetDestination(homePoint.position);
        }

        // Put this script on the enemy root OR on a child kill trigger
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

