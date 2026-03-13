using UnityEngine;
using UnityEngine.AI;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyChaser : MonoBehaviour
    {
        private enum EnemyState
        {
            Patrol,
            Chase,
            Return
        }

        [Header("References")]
        [SerializeField] private PlayerState player;
        [SerializeField] private Transform homePoint;
        [SerializeField] private Transform activityCenter;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 1.2f;
        [SerializeField] private float patrolPointReachDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float loseTargetRange = 10f;

        [Header("Activity Area")]
        [SerializeField] private float activityRadius = 12f;

        [Header("Lure")]
        [SerializeField] private float lureMaxConsiderDistance = 25f;

        [Header("Update")]
        [SerializeField] private float updateRate = 0.1f;

        private NavMeshAgent _agent;
        private EnemyState _state = EnemyState.Patrol;

        private int _currentPatrolIndex = 0;
        private float _waitTimer = 0f;
        private float _updateTimer = 0f;

        private PlayerChaseTracker _chaseTracker;
        private bool _isCurrentlyChasingPlayer = false;

        public bool IsChasingPlayer => _isCurrentlyChasingPlayer;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    player = playerGO.GetComponent<PlayerState>();
            }

            if (player != null)
                _chaseTracker = player.GetComponent<PlayerChaseTracker>();

            if (activityCenter == null)
                activityCenter = homePoint;

            if (homePoint == null)
                Debug.LogWarning($"[EnemyChaser] homePoint is NULL on {name}", this);

            if (activityCenter == null)
                Debug.LogWarning($"[EnemyChaser] activityCenter is NULL on {name}", this);

            if (patrolPoints == null || patrolPoints.Length == 0)
                Debug.LogWarning($"[EnemyChaser] No patrol points assigned on {name}", this);

            Debug.Log(
                $"[EnemyChaser] Start | player={(player ? player.name : "NULL")} | home={(homePoint ? homePoint.name : "NULL")} | center={(activityCenter ? activityCenter.name : "NULL")}",
                this
            );

            EnterPatrolState();
        }

        private void Update()
        {
            if (player == null || homePoint == null || activityCenter == null)
                return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer < updateRate) return;
            _updateTimer = 0f;

            switch (_state)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;

                case EnemyState.Chase:
                    UpdateChase();
                    break;

                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }
        }

        private void OnDisable()
        {
            StopChasingPlayer();
        }

        private void OnDestroy()
        {
            StopChasingPlayer();
        }

        // =========================
        // State Enter
        // =========================

        private void EnterPatrolState()
        {
            _state = EnemyState.Patrol;
            _waitTimer = 0f;

            StopChasingPlayer();

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                MoveToPatrolPoint(_currentPatrolIndex);
            }
            else
            {
                MoveToPosition(homePoint.position);
            }

            Debug.Log($"[EnemyChaser] Enter PATROL | {name}", this);
        }

        private void EnterChaseState()
        {
            _state = EnemyState.Chase;
            BeginChasingPlayer();
            Debug.Log($"[EnemyChaser] Enter CHASE | {name}", this);
        }

        private void EnterReturnState()
        {
            _state = EnemyState.Return;

            StopChasingPlayer();

            Transform target = GetNearestPatrolPoint();
            if (target != null)
                MoveToPosition(target.position);
            else
                MoveToPosition(homePoint.position);

            Debug.Log($"[EnemyChaser] Enter RETURN | {name}", this);
        }

        // =========================
        // State Update
        // =========================

        private void UpdatePatrol()
        {
            // 如果场景中有 lure，优先去 lure
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                StopChasingPlayer();
                MoveToPosition(lurePos);
                return;
            }

            // 检查是否开始追击
            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            // 没有巡逻点就待在 home
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                MoveToPosition(homePoint.position);
                return;
            }

            // 到达巡逻点后等待，再走下一个
            if (!_agent.pathPending && _agent.remainingDistance <= patrolPointReachDistance)
            {
                _waitTimer += updateRate;

                if (_waitTimer >= patrolWaitTime)
                {
                    _waitTimer = 0f;
                    _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                    MoveToPatrolPoint(_currentPatrolIndex);
                }
            }
        }

        private void UpdateChase()
        {
            // lure 优先级最高
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                StopChasingPlayer();
                MoveToPosition(lurePos);
                return;
            }

            // 脱战条件 1：玩家死了
            if (player.IsDead)
            {
                EnterReturnState();
                return;
            }

            // 脱战条件 2：玩家进入安全区
            if (player.IsInSafeZone)
            {
                EnterReturnState();
                return;
            }

            // 脱战条件 3：玩家超出脱战范围
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer > loseTargetRange)
            {
                EnterReturnState();
                return;
            }

            // 脱战条件 4：敌人自己跑出活动区域
            if (!IsInsideActivityArea(transform.position))
            {
                EnterReturnState();
                return;
            }

            // 脱战条件 5：玩家跑出活动区域
            if (!IsInsideActivityArea(player.transform.position))
            {
                EnterReturnState();
                return;
            }

            BeginChasingPlayer();
            MoveToPosition(player.transform.position);
        }

        private void UpdateReturn()
        {
            // Return 状态也可被 lure 打断
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                StopChasingPlayer();
                MoveToPosition(lurePos);
                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= patrolPointReachDistance)
            {
                EnterPatrolState();
            }
        }

        // =========================
        // Core Checks
        // =========================

        private bool CanStartChase()
        {
            if (player == null) return false;
            if (player.IsDead) return false;
            if (player.IsInSafeZone) return false;

            // 玩家必须在活动区域里
            if (!IsInsideActivityArea(player.transform.position))
                return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= detectRange;
        }

        private bool IsInsideActivityArea(Vector3 worldPos)
        {
            return Vector3.Distance(worldPos, activityCenter.position) <= activityRadius;
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

        // =========================
        // Chase Tracker
        // =========================

        private void BeginChasingPlayer()
        {
            if (_isCurrentlyChasingPlayer) return;

            _isCurrentlyChasingPlayer = true;

            if (_chaseTracker != null)
                _chaseTracker.RegisterChaser(this);
        }

        private void StopChasingPlayer()
        {
            if (!_isCurrentlyChasingPlayer) return;

            _isCurrentlyChasingPlayer = false;

            if (_chaseTracker != null)
                _chaseTracker.UnregisterChaser(this);
        }

        // =========================
        // Movement Helpers
        // =========================

        private void MoveToPatrolPoint(int index)
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            if (index < 0 || index >= patrolPoints.Length) return;
            if (patrolPoints[index] == null) return;

            MoveToPosition(patrolPoints[index].position);
        }

        private void MoveToPosition(Vector3 targetPos)
        {
            _agent.isStopped = false;
            _agent.SetDestination(targetPos);
        }

        private Transform GetNearestPatrolPoint()
        {
            Transform nearest = null;
            float best = float.MaxValue;

            if (patrolPoints == null || patrolPoints.Length == 0)
                return homePoint;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;

                float d = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (d < best)
                {
                    best = d;
                    nearest = patrolPoints[i];
                }
            }

            return nearest != null ? nearest : homePoint;
        }

        // =========================
        // Gizmos
        // =========================

        private void OnDrawGizmosSelected()
        {
            // Detect Range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            // Lose Target Range
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);

            // Lure Detection Range
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, lureMaxConsiderDistance);

            // Activity Area
            if (activityCenter != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(activityCenter.position, activityRadius);
            }

            // Home Point
            if (homePoint != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(homePoint.position, 0.25f);
                Gizmos.DrawLine(transform.position, homePoint.position);
            }

            // Patrol Path
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.green;

                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;

                    Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);

                    Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                    if (next != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, next.position);
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"State: {_state} | Chasing: {_isCurrentlyChasingPlayer}"
            );
#endif
        }
    }
}