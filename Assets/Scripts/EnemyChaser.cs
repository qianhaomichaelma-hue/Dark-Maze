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
            Suspicious,
            Chase,
            Return
        }

        [Header("References")]
        [SerializeField] private PlayerState player;
        [SerializeField] private Transform homePoint;
        [SerializeField] private Transform activityCenter;
        [SerializeField] private EnemyAlertMark alertMark;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 1.2f;
        [SerializeField] private float patrolPointReachDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float suspiciousRange = 10f;
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float loseTargetRange = 10f;

        [Header("Suspicious")]
        [SerializeField] private float suspiciousStayTime = 1.5f;
        [SerializeField] private float suspiciousScanSpeed = 100f;
        [SerializeField] private float suspiciousPointReachDistance = 0.8f;

        [Header("Activity Area")]
        [SerializeField] private float activityRadius = 12f;

        [Header("Lure")]
        [SerializeField] private float lureMaxConsiderDistance = 25f;

        [Header("Hit Reaction")]
        [SerializeField] private float hitBackDistance = 1.2f;
        [SerializeField] private float hitRecoverTime = 0.25f;

        [Header("Update")]
        [SerializeField] private float updateRate = 0.1f;

        private NavMeshAgent _agent;
        private EnemyState _state = EnemyState.Patrol;

        private int _currentPatrolIndex = 0;
        private float _waitTimer = 0f;
        private float _updateTimer = 0f;
        private float _suspiciousTimer = 0f;
        private float _hitRecoverTimer = 0f;

        private Vector3 _suspiciousTarget;
        private bool _hasSuspiciousTarget = false;
        private bool _reachedSuspiciousTarget = false;
        private int _scanDirection = 1;

        private PlayerChaseTracker _chaseTracker;
        private bool _isCurrentlyChasingPlayer = false;

        // 新增：击退保护窗口，避免刚击退就立刻被 Chase 覆盖 destination
        private bool _isRepelling = false;

        public bool IsChasingPlayer => _isCurrentlyChasingPlayer;
        public bool IsSuspicious => _state == EnemyState.Suspicious;

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

            if (alertMark == null)
                alertMark = GetComponentInChildren<EnemyAlertMark>(true);

            RefreshAlertMark();
            EnterPatrolState();
        }

        private void Update()
        {
            if (player == null || homePoint == null || activityCenter == null)
                return;

            if (_hitRecoverTimer > 0f)
            {
                _hitRecoverTimer -= Time.deltaTime;

                if (_hitRecoverTimer <= 0f)
                {
                    _hitRecoverTimer = 0f;
                    _isRepelling = false;
                }
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer < updateRate) return;
            _updateTimer = 0f;

            switch (_state)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;

                case EnemyState.Suspicious:
                    UpdateSuspicious();
                    break;

                case EnemyState.Chase:
                    UpdateChase();
                    break;

                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }

            RefreshAlertMark();
        }

        private void OnDisable()
        {
            StopChasingPlayer();
        }

        private void OnDestroy()
        {
            StopChasingPlayer();
        }

        private void EnterPatrolState()
        {
            _state = EnemyState.Patrol;
            _waitTimer = 0f;
            _suspiciousTimer = 0f;
            _hasSuspiciousTarget = false;
            _reachedSuspiciousTarget = false;
            _isRepelling = false;
            _hitRecoverTimer = 0f;

            StopChasingPlayer();

            if (patrolPoints != null && patrolPoints.Length > 0)
                MoveToPatrolPoint(_currentPatrolIndex);
            else
                MoveToPosition(homePoint.position);

            RefreshAlertMark();
            Debug.Log($"[EnemyChaser] Enter PATROL | {name}", this);
        }

        private void EnterSuspiciousState(Vector3 targetPos)
        {
            _state = EnemyState.Suspicious;
            _suspiciousTarget = targetPos;
            _hasSuspiciousTarget = true;
            _reachedSuspiciousTarget = false;
            _suspiciousTimer = 0f;
            _scanDirection = 1;
            _isRepelling = false;
            _hitRecoverTimer = 0f;

            StopChasingPlayer();
            MoveToPosition(_suspiciousTarget);

            RefreshAlertMark();
            Debug.Log($"[EnemyChaser] Enter SUSPICIOUS | {name}", this);
        }

        private void EnterChaseState()
        {
            _state = EnemyState.Chase;
            _isRepelling = false;
            _hitRecoverTimer = 0f;

            BeginChasingPlayer();

            RefreshAlertMark();
            Debug.Log($"[EnemyChaser] Enter CHASE | {name}", this);
        }

        private void EnterReturnState()
        {
            _state = EnemyState.Return;
            _suspiciousTimer = 0f;
            _hasSuspiciousTarget = false;
            _reachedSuspiciousTarget = false;
            _isRepelling = false;
            _hitRecoverTimer = 0f;

            StopChasingPlayer();

            Transform target = GetNearestPatrolPoint();
            if (target != null)
                MoveToPosition(target.position);
            else
                MoveToPosition(homePoint.position);

            RefreshAlertMark();
            Debug.Log($"[EnemyChaser] Enter RETURN | {name}", this);
        }

        private void UpdatePatrol()
        {
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterSuspiciousState(lurePos);
                return;
            }

            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (CanStartSuspicious(out Vector3 suspiciousPos))
            {
                EnterSuspiciousState(suspiciousPos);
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                MoveToPosition(homePoint.position);
                return;
            }

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

        private void UpdateSuspicious()
        {
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                _suspiciousTarget = lurePos;
                _hasSuspiciousTarget = true;
                _reachedSuspiciousTarget = false;
                _suspiciousTimer = 0f;
                MoveToPosition(_suspiciousTarget);
            }

            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (player.IsDead || player.IsInSafeZone || !IsInsideActivityArea(player.transform.position))
            {
                EnterReturnState();
                return;
            }

            if (!_hasSuspiciousTarget)
            {
                EnterReturnState();
                return;
            }

            if (!_reachedSuspiciousTarget)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= suspiciousPointReachDistance)
                {
                    _reachedSuspiciousTarget = true;
                    _agent.isStopped = true;
                }

                return;
            }

            _suspiciousTimer += updateRate;

            float turnAmount = suspiciousScanSpeed * updateRate * _scanDirection;
            transform.Rotate(0f, turnAmount, 0f);

            if (_suspiciousTimer >= suspiciousStayTime * 0.5f && _scanDirection > 0)
                _scanDirection = -1;

            if (_suspiciousTimer >= suspiciousStayTime)
            {
                _agent.isStopped = false;
                EnterReturnState();
            }
        }

        private void UpdateChase()
        {
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterSuspiciousState(lurePos);
                return;
            }

            if (player.IsDead)
            {
                EnterReturnState();
                return;
            }

            if (player.IsInSafeZone)
            {
                EnterReturnState();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer > loseTargetRange)
            {
                EnterReturnState();
                return;
            }

            if (!IsInsideActivityArea(transform.position))
            {
                EnterReturnState();
                return;
            }

            if (!IsInsideActivityArea(player.transform.position))
            {
                EnterReturnState();
                return;
            }

            BeginChasingPlayer();

            // 击退后的短暂恢复期，不立刻覆盖 destination
            if (_isRepelling)
                return;

            MoveToPosition(player.transform.position);
        }

        private void UpdateReturn()
        {
            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterSuspiciousState(lurePos);
                return;
            }

            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (CanStartSuspicious(out Vector3 suspiciousPos))
            {
                EnterSuspiciousState(suspiciousPos);
                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= patrolPointReachDistance)
            {
                EnterPatrolState();
            }
        }

        private bool CanStartChase()
        {
            if (player == null) return false;
            if (player.IsDead) return false;
            if (player.IsInSafeZone) return false;
            if (!IsInsideActivityArea(player.transform.position)) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= detectRange;
        }

        private bool CanStartSuspicious(out Vector3 suspiciousPos)
        {
            suspiciousPos = default;

            if (player == null) return false;
            if (player.IsDead) return false;
            if (player.IsInSafeZone) return false;
            if (!IsInsideActivityArea(player.transform.position)) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > suspiciousRange) return false;
            if (dist <= detectRange) return false;

            suspiciousPos = player.transform.position;
            return true;
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

        public bool TryRepelFrom(Vector3 sourcePosition)
        {
            if (_state != EnemyState.Chase)
                return false;

            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)
                away = -transform.forward;

            away.Normalize();

            Vector3 rawTarget = transform.position + away * hitBackDistance;

            if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _isRepelling = true;
                _hitRecoverTimer = hitRecoverTime;

                _agent.isStopped = false;
                _agent.SetDestination(hit.position);

                Debug.Log($"[EnemyChaser] Repelled | {name} | hitBackDistance={hitBackDistance} | target={hit.position}", this);
                return true;
            }

            Debug.Log($"[EnemyChaser] Repel failed to find navmesh point | {name}", this);
            return false;
        }

        private void RefreshAlertMark()
        {
            if (alertMark == null) return;
            alertMark.SetVisible(_state == EnemyState.Suspicious);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, suspiciousRange);

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, lureMaxConsiderDistance);

            if (activityCenter != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(activityCenter.position, activityRadius);
            }

            if (homePoint != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(homePoint.position, 0.25f);
                Gizmos.DrawLine(transform.position, homePoint.position);
            }
        }
    }
}