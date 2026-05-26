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
            InvestigateLure,
            SuspiciousPlayer,
            Chase,
            Hit,
            Return
        }

        [Header("References")]
        [SerializeField] private PlayerState player;
        [SerializeField] private Transform homePoint;
        [SerializeField] private Transform activityCenter;
        [SerializeField] private EnemyAlertMark alertMark;
        [SerializeField] private Animator animator;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 1.2f;
        [SerializeField] private float patrolPointReachDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float suspiciousRange = 10f;
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float loseTargetRange = 10f;

        [Header("Suspicious / Investigation")]
        [SerializeField] private float suspiciousStayTime = 1.5f;
        [SerializeField] private float suspiciousScanSpeed = 100f;
        [SerializeField] private float suspiciousPointReachDistance = 0.8f;

        [Header("Activity Area")]
        [SerializeField] private float activityRadius = 12f;

        [Header("Lure")]
        [SerializeField] private float lureMaxConsiderDistance = 25f;

        [Tooltip("If true, a thrown lure can interrupt Chase.")]
        [SerializeField] private bool lureCanInterruptChase = true;

        [Tooltip("If true, the enemy temporarily ignores the player while investigating a lure.")]
        [SerializeField] private bool ignorePlayerWhileInvestigatingLure = true;

        [Tooltip("After being distracted by a lure, how long the enemy ignores player detection.")]
        [SerializeField] private float lureIgnorePlayerTime = 2.5f;

        [Header("Hit Reaction")]
        [SerializeField] private float hitBackDistance = 1.2f;

        [Tooltip("How long the enemy stays in Hit state before returning. Set this close to your hit animation length.")]
        [SerializeField] private float hitRecoverTime = 0.55f;

        [SerializeField] private bool triggerHitAnimation = true;

        [Header("Animation")]
        [SerializeField] private string stateParamName = "State";
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string hitTriggerName = "Hit";
        [SerializeField] private float speedDampTime = 0.08f;
        [SerializeField] private float maxAnimationSpeed = 6f;
        [SerializeField] private float idleSpeedThreshold = 0.05f;

        [Header("Audio - Enemy Events")]
        [SerializeField] private AudioSource eventAudioSource;

        [Header("Audio - Player Detected")]
        [SerializeField] private AudioClip playerDetectedSFX;
        [SerializeField] private float playerDetectedVolume = 0.9f;
        [SerializeField] private float detectedSFXCooldown = 1.0f;

        [Header("Audio - Enemy Hit")]
        [SerializeField] private AudioClip enemyHitSFX;
        [SerializeField] private float enemyHitVolume = 1.0f;
        [SerializeField] private float enemyHitSFXCooldown = 0.2f;

        [Header("Update")]
        [SerializeField] private float updateRate = 0.1f;

        private NavMeshAgent _agent;

        [SerializeField] private EnemyState _state = EnemyState.Patrol;

        private int _currentPatrolIndex = 0;
        private float _waitTimer = 0f;
        private float _updateTimer = 0f;

        private float _interestTimer = 0f;
        private Vector3 _interestTarget;
        private bool _hasInterestTarget = false;
        private bool _reachedInterestTarget = false;
        private int _scanDirection = 1;

        private float _hitRecoverTimer = 0f;
        private bool _isRepelling = false;

        private Quaternion _hitLockedRotation;
        private bool _hasHitLockedRotation = false;
        private bool _defaultAgentUpdateRotation = true;

        private PlayerChaseTracker _chaseTracker;
        private bool _isCurrentlyChasingPlayer = false;

        private int _stateHash;
        private int _speedHash;
        private int _hitHash;

        private float _lastDetectedSFXTime = -999f;
        private float _lastEnemyHitSFXTime = -999f;

        private float _ignorePlayerUntilTime = -999f;

        public bool IsChasingPlayer => _isCurrentlyChasingPlayer;

        public bool IsSuspicious =>
            _state == EnemyState.InvestigateLure ||
            _state == EnemyState.SuspiciousPlayer;

        public bool IsInvestigatingLure => _state == EnemyState.InvestigateLure;
        public bool IsSuspiciousPlayer => _state == EnemyState.SuspiciousPlayer;
        public bool IsHit => _state == EnemyState.Hit;

        public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (_agent != null)
                _defaultAgentUpdateRotation = _agent.updateRotation;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (eventAudioSource == null)
                eventAudioSource = GetComponent<AudioSource>();

            if (eventAudioSource != null)
            {
                eventAudioSource.playOnAwake = false;
                eventAudioSource.loop = false;
                eventAudioSource.dopplerLevel = 0f;
            }

            _stateHash = Animator.StringToHash(stateParamName);
            _speedHash = Animator.StringToHash(speedParamName);
            _hitHash = Animator.StringToHash(hitTriggerName);
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
                Debug.Log($"[EnemyChaser] No patrol points assigned on {name}. This enemy will idle at home in Patrol state.", this);

            if (alertMark == null)
                alertMark = GetComponentInChildren<EnemyAlertMark>(true);

            if (animator == null)
                Debug.LogWarning($"[EnemyChaser] Animator not found on {name}. Animation sync will be skipped.", this);

            RefreshAlertMark();
            EnterPatrolState();
            UpdateAnimator(true);
        }

        public void InitializeRuntimeReferences(
    PlayerState runtimePlayer,
    Transform runtimeHomePoint,
    Transform runtimeActivityCenter = null
)
        {
            if (runtimePlayer != null)
                player = runtimePlayer;

            if (runtimeHomePoint != null)
                homePoint = runtimeHomePoint;

            if (runtimeActivityCenter != null)
                activityCenter = runtimeActivityCenter;
            else if (activityCenter == null)
                activityCenter = homePoint;

            if (player != null)
                _chaseTracker = player.GetComponent<PlayerChaseTracker>();

            if (alertMark == null)
                alertMark = GetComponentInChildren<EnemyAlertMark>(true);

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            RefreshAlertMark();

            Debug.Log(
                $"[EnemyChaser] Runtime references initialized | enemy={name} | player={(player ? player.name : "NULL")} | home={(homePoint ? homePoint.name : "NULL")} | activity={(activityCenter ? activityCenter.name : "NULL")}",
                this
            );
        }
        private void Update()
        {
            if (player == null || homePoint == null || activityCenter == null)
            {
                UpdateAnimator(false);
                return;
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateRate)
            {
                _updateTimer = 0f;

                switch (_state)
                {
                    case EnemyState.Patrol:
                        UpdatePatrol();
                        break;

                    case EnemyState.InvestigateLure:
                        UpdateInvestigateLure();
                        break;

                    case EnemyState.SuspiciousPlayer:
                        UpdateSuspiciousPlayer();
                        break;

                    case EnemyState.Chase:
                        UpdateChase();
                        break;

                    case EnemyState.Hit:
                        UpdateHit();
                        break;

                    case EnemyState.Return:
                        UpdateReturn();
                        break;
                }

                RefreshAlertMark();
            }

            UpdateAnimator(false);
        }

        private void LateUpdate()
        {
            if (_state == EnemyState.Hit && _hasHitLockedRotation)
            {
                transform.rotation = _hitLockedRotation;
            }
        }

        private void OnDisable()
        {
            StopChasingPlayer();
            RestoreAgentRotationControl();
        }

        private void OnDestroy()
        {
            StopChasingPlayer();
            RestoreAgentRotationControl();
        }

        private void EnterPatrolState()
        {
            _state = EnemyState.Patrol;

            _waitTimer = 0f;
            ResetInterestData();
            ResetHitData();

            StopChasingPlayer();

            if (HasPatrolPoints)
                MoveToPatrolPoint(_currentPatrolIndex);
            else
                MoveToHomePoint();

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter PATROL | {name}", this);
        }

        private void EnterInvestigateLureState(Vector3 lurePos, bool interruptedChase)
        {
            _state = EnemyState.InvestigateLure;

            SetInterestTarget(lurePos);
            ResetHitData();

            StopChasingPlayer();

            if (interruptedChase || ignorePlayerWhileInvestigatingLure)
                StartIgnoringPlayerBecauseOfLure();

            MoveToPosition(_interestTarget);

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter INVESTIGATE LURE | {name} | target={lurePos} | interruptedChase={interruptedChase}", this);
        }

        private void EnterSuspiciousPlayerState(Vector3 playerLastKnownPos)
        {
            _state = EnemyState.SuspiciousPlayer;

            SetInterestTarget(playerLastKnownPos);
            ResetHitData();

            StopChasingPlayer();
            MoveToPosition(_interestTarget);

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter SUSPICIOUS PLAYER | {name} | target={playerLastKnownPos}", this);
        }

        private void EnterChaseState()
        {
            _state = EnemyState.Chase;

            ResetInterestData();
            ResetHitData();

            BeginChasingPlayer();

            PlayPlayerDetectedSFX();

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter CHASE | {name}", this);
        }

        private void EnterHitState(Vector3 repelTarget)
        {
            _state = EnemyState.Hit;

            ResetInterestData();

            _isRepelling = true;
            _hitRecoverTimer = Mathf.Max(0.01f, hitRecoverTime);

            _hitLockedRotation = transform.rotation;
            _hasHitLockedRotation = true;

            if (_agent != null)
                _agent.updateRotation = false;

            StopChasingPlayer();

            if (triggerHitAnimation && animator != null)
            {
                animator.ResetTrigger(_hitHash);
                animator.SetTrigger(_hitHash);
            }

            PlayEnemyHitSFX();

            MoveToPosition(repelTarget);

            transform.rotation = _hitLockedRotation;

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter HIT | {name} | target={repelTarget}", this);
        }

        private void EnterReturnState()
        {
            _state = EnemyState.Return;

            ResetInterestData();
            ResetHitData();

            StopChasingPlayer();

            if (HasPatrolPoints)
            {
                Transform target = GetNearestPatrolPoint();
                if (target != null)
                    MoveToPosition(target.position);
                else
                    MoveToHomePoint();
            }
            else
            {
                MoveToHomePoint();
            }

            RefreshAlertMark();
            UpdateAnimator(true);

            Debug.Log($"[EnemyChaser] Enter RETURN | {name}", this);
        }

        private void UpdatePatrol()
        {
            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterInvestigateLureState(lurePos, false);
                return;
            }

            if (CanStartSuspiciousPlayer(out Vector3 suspiciousPos))
            {
                EnterSuspiciousPlayerState(suspiciousPos);
                return;
            }

            if (!HasPatrolPoints)
            {
                HoldAtHomePoint();
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

        private void UpdateInvestigateLure()
        {
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

            if (!IsInsideActivityArea(transform.position))
            {
                EnterReturnState();
                return;
            }

            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (TryGetNearestLure(out Vector3 lurePos))
            {
                if ((_interestTarget - lurePos).sqrMagnitude > 0.05f)
                {
                    SetInterestTarget(lurePos);
                    StartIgnoringPlayerBecauseOfLure();
                    MoveToPosition(_interestTarget);
                }
            }

            if (UpdateMoveToInterestAndScan())
            {
                EnterReturnState();
            }
        }

        private void UpdateSuspiciousPlayer()
        {
            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterInvestigateLureState(lurePos, false);
                return;
            }

            if (player.IsDead || player.IsInSafeZone || !IsInsideActivityArea(player.transform.position))
            {
                EnterReturnState();
                return;
            }

            if (UpdateMoveToInterestAndScan())
            {
                EnterReturnState();
            }
        }

        private void UpdateChase()
        {
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

            if (lureCanInterruptChase && TryGetNearestLure(out Vector3 lurePos))
            {
                EnterInvestigateLureState(lurePos, true);
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

            MoveToPosition(player.transform.position);
        }

        private void UpdateHit()
        {
            _hitRecoverTimer -= updateRate;

            if (_hitRecoverTimer > 0f)
                return;

            _hitRecoverTimer = 0f;
            _isRepelling = false;

            EnterReturnState();
        }

        private void UpdateReturn()
        {
            if (CanStartChase())
            {
                EnterChaseState();
                return;
            }

            if (TryGetNearestLure(out Vector3 lurePos))
            {
                EnterInvestigateLureState(lurePos, false);
                return;
            }

            if (CanStartSuspiciousPlayer(out Vector3 suspiciousPos))
            {
                EnterSuspiciousPlayerState(suspiciousPos);
                return;
            }

            if (!HasPatrolPoints)
            {
                if (IsNearHomePoint())
                    EnterPatrolState();
                else
                    MoveToHomePoint();

                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= patrolPointReachDistance)
            {
                EnterPatrolState();
            }
        }

        private bool UpdateMoveToInterestAndScan()
        {
            if (!_hasInterestTarget)
                return true;

            if (!_reachedInterestTarget)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= suspiciousPointReachDistance)
                {
                    _reachedInterestTarget = true;
                    _agent.isStopped = true;
                }

                return false;
            }

            _interestTimer += updateRate;

            float turnAmount = suspiciousScanSpeed * updateRate * _scanDirection;
            transform.Rotate(0f, turnAmount, 0f);

            if (_interestTimer >= suspiciousStayTime * 0.5f && _scanDirection > 0)
                _scanDirection = -1;

            if (_interestTimer >= suspiciousStayTime)
            {
                _agent.isStopped = false;
                return true;
            }

            return false;
        }

        private bool CanStartChase()
        {
            if (IsIgnoringPlayerBecauseOfLure()) return false;

            if (player == null) return false;
            if (player.IsDead) return false;
            if (player.IsInSafeZone) return false;
            if (!IsInsideActivityArea(player.transform.position)) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= detectRange;
        }

        private bool CanStartSuspiciousPlayer(out Vector3 suspiciousPos)
        {
            suspiciousPos = default;

            if (IsIgnoringPlayerBecauseOfLure()) return false;

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

        private void StartIgnoringPlayerBecauseOfLure()
        {
            if (lureIgnorePlayerTime <= 0f)
                return;

            _ignorePlayerUntilTime = Mathf.Max(
                _ignorePlayerUntilTime,
                Time.time + lureIgnorePlayerTime
            );

            Debug.Log($"[EnemyChaser] Ignore player because of lure until {_ignorePlayerUntilTime:F2} | {name}", this);
        }

        private bool IsIgnoringPlayerBecauseOfLure()
        {
            return Time.time < _ignorePlayerUntilTime;
        }

        private bool IsInsideActivityArea(Vector3 worldPos)
        {
            if (activityCenter == null) return true;
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

                if (!IsInsideActivityArea(l.transform.position))
                    continue;

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
            if (!HasPatrolPoints) return;
            if (index < 0 || index >= patrolPoints.Length) return;
            if (patrolPoints[index] == null) return;

            MoveToPosition(patrolPoints[index].position);
        }

        private void MoveToHomePoint()
        {
            if (homePoint == null) return;
            MoveToPosition(homePoint.position);
        }

        private void HoldAtHomePoint()
        {
            if (homePoint == null) return;

            float dist = Vector3.Distance(transform.position, homePoint.position);
            if (dist > patrolPointReachDistance)
            {
                MoveToHomePoint();
            }
            else
            {
                _agent.isStopped = true;
            }
        }

        private bool IsNearHomePoint()
        {
            if (homePoint == null) return true;
            return Vector3.Distance(transform.position, homePoint.position) <= patrolPointReachDistance;
        }

        private void MoveToPosition(Vector3 targetPos)
        {
            if (_agent == null) return;

            _agent.isStopped = false;
            _agent.SetDestination(targetPos);
        }

        private Transform GetNearestPatrolPoint()
        {
            Transform nearest = null;
            float best = float.MaxValue;

            if (!HasPatrolPoints)
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

        private void SetInterestTarget(Vector3 targetPos)
        {
            _interestTarget = targetPos;
            _hasInterestTarget = true;
            _reachedInterestTarget = false;
            _interestTimer = 0f;
            _scanDirection = 1;
        }

        private void ResetInterestData()
        {
            _interestTimer = 0f;
            _hasInterestTarget = false;
            _reachedInterestTarget = false;
            _scanDirection = 1;
        }

        private void ResetHitData()
        {
            _isRepelling = false;
            _hitRecoverTimer = 0f;
            _hasHitLockedRotation = false;

            RestoreAgentRotationControl();
        }

        private void RestoreAgentRotationControl()
        {
            if (_agent != null)
                _agent.updateRotation = _defaultAgentUpdateRotation;
        }

        public bool TryRepelFrom(Vector3 sourcePosition)
        {
            if (_state != EnemyState.Chase)
            {
                Debug.Log($"[EnemyChaser] Repel ignored because enemy is not chasing | {name} | state={_state}", this);
                return false;
            }

            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)
                away = -transform.forward;

            away.Normalize();

            Vector3 rawTarget = transform.position + away * hitBackDistance;

            if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                EnterHitState(hit.position);

                Debug.Log($"[EnemyChaser] Repelled / Hit | {name} | hitBackDistance={hitBackDistance} | target={hit.position}", this);
                return true;
            }

            Debug.Log($"[EnemyChaser] Repel failed to find navmesh point | {name}", this);
            return false;
        }

        private void PlayPlayerDetectedSFX()
        {
            if (eventAudioSource == null || playerDetectedSFX == null)
                return;

            if (Time.time - _lastDetectedSFXTime < detectedSFXCooldown)
                return;

            _lastDetectedSFXTime = Time.time;

            eventAudioSource.PlayOneShot(playerDetectedSFX, playerDetectedVolume);
        }

        private void PlayEnemyHitSFX()
        {
            if (eventAudioSource == null || enemyHitSFX == null)
                return;

            if (Time.time - _lastEnemyHitSFXTime < enemyHitSFXCooldown)
                return;

            _lastEnemyHitSFXTime = Time.time;

            eventAudioSource.PlayOneShot(enemyHitSFX, enemyHitVolume);
        }

        private void RefreshAlertMark()
        {
            if (alertMark == null) return;

            alertMark.SetVisible(
                _state == EnemyState.InvestigateLure ||
                _state == EnemyState.SuspiciousPlayer
            );
        }

        private void UpdateAnimator(bool forceInstant)
        {
            if (animator == null) return;

            animator.SetInteger(_stateHash, GetAnimatorStateValue());

            float rawSpeed = _agent != null ? _agent.velocity.magnitude : 0f;

            if (rawSpeed < idleSpeedThreshold)
                rawSpeed = 0f;

            float clampedSpeed = Mathf.Clamp(rawSpeed, 0f, maxAnimationSpeed);

            if (forceInstant)
                animator.SetFloat(_speedHash, clampedSpeed);
            else
                animator.SetFloat(_speedHash, clampedSpeed, speedDampTime, Time.deltaTime);
        }

        private int GetAnimatorStateValue()
        {
            switch (_state)
            {
                case EnemyState.Patrol:
                    return 0;

                case EnemyState.InvestigateLure:
                    return 1;

                case EnemyState.SuspiciousPlayer:
                    return 1;

                case EnemyState.Chase:
                    return 2;

                case EnemyState.Return:
                    return 3;

                case EnemyState.Hit:
                    return 4;

                default:
                    return 0;
            }
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