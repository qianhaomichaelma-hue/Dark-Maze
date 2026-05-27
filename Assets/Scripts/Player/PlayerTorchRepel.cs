using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using DarkMazeItems;
using DarkMazeMinimal;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerTorchRepel : MonoBehaviour
    {
        [Header("Requirement")]
        [SerializeField] private ItemData torchItem;

        [Header("Attack")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private float maxAttackAngle = 65f;
        [SerializeField] private LayerMask enemyLayers = ~0;
        [SerializeField] private float cooldown = 5f;

        [Header("Attack Timing")]
        [Tooltip("Delay before the hit check happens. Use this to match the moment when the torch visually reaches the enemy.")]
        [SerializeField] private float hitDelay = 0.16f;

        [Tooltip("If true, the attack direction is locked when the swing starts. This usually feels better for melee attacks.")]
        [SerializeField] private bool lockAttackDirectionOnSwing = true;

        [Tooltip("If true, the player must still be holding the torch when the delayed hit frame happens.")]
        [SerializeField] private bool requireTorchAtHitMoment = true;

        [Header("Visual")]
        [SerializeField] private PlayerTorchVisual torchVisual;

        [Header("Audio - Torch Swing")]
        [SerializeField] private AudioSource torchSwingAudioSource;
        [SerializeField] private AudioClip torchSwingSFX;
        [SerializeField] private float torchSwingVolume = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private int debugSegments = 6;

        private StarterAssetsInputs _inputs;
        private PlayerEquipment _equipment;
        private PlayerState _state;
        private Camera _cam;

        private float _nextReadyTime = 0f;
        private Coroutine _attackRoutine;

        public bool IsOnCooldown => Time.time < _nextReadyTime;
        public float CooldownRemaining => Mathf.Max(0f, _nextReadyTime - Time.time);
        public bool IsAttacking => _attackRoutine != null;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
            _equipment = GetComponent<PlayerEquipment>();
            _state = GetComponent<PlayerState>();
            _cam = Camera.main;

            if (torchVisual == null)
                torchVisual = GetComponent<PlayerTorchVisual>();

            if (attackOrigin == null)
                attackOrigin = transform;

            if (torchSwingAudioSource == null)
                torchSwingAudioSource = GetComponent<AudioSource>();

            if (torchSwingAudioSource != null)
            {
                torchSwingAudioSource.playOnAwake = false;
                torchSwingAudioSource.loop = false;
            }
        }

        private void Update()
        {
            if (_inputs == null) return;
            if (_state != null && _state.IsDead) return;

            if (_inputs.attack)
            {
                _inputs.attack = false;
                TryStartAttack();
            }
        }

        private void TryStartAttack()
        {
            if (_equipment == null || torchItem == null)
                return;

            if (!_equipment.IsHolding(torchItem))
            {
                Log("Not holding torch.");
                return;
            }

            if (Time.time < _nextReadyTime)
            {
                Log($"Cooldown: {CooldownRemaining:F1}s");
                return;
            }

            if (_attackRoutine != null)
            {
                Log("Attack ignored. Already attacking.");
                return;
            }

            Vector3 lockedForward = GetAttackForward();

            _nextReadyTime = Time.time + cooldown;
            _attackRoutine = StartCoroutine(AttackRoutine(lockedForward));
        }

        private IEnumerator AttackRoutine(Vector3 lockedForward)
        {
            Log($"Attack started. Hit will happen after {hitDelay:F2}s.");

            if (torchVisual != null)
                torchVisual.PlaySwing();

            PlayTorchSwingSFX();

            float delay = Mathf.Max(0f, hitDelay);

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (_state != null && _state.IsDead)
            {
                Log("Attack hit frame cancelled. Player is dead.");
                _attackRoutine = null;
                yield break;
            }

            if (requireTorchAtHitMoment)
            {
                if (_equipment == null || torchItem == null || !_equipment.IsHolding(torchItem))
                {
                    Log("Attack hit frame cancelled. Player is no longer holding torch.");
                    _attackRoutine = null;
                    yield break;
                }
            }

            Vector3 forward = lockAttackDirectionOnSwing
                ? lockedForward
                : GetAttackForward();

            PerformRepelCheck(forward);

            _attackRoutine = null;
        }

        private void PerformRepelCheck(Vector3 forward)
        {
            Vector3 origin = attackOrigin != null
                ? attackOrigin.position
                : transform.position + Vector3.up;

            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            forward.Normalize();

            HashSet<EnemyChaser> hitEnemies = new HashSet<EnemyChaser>();

            int segments = Mathf.Max(1, debugSegments);
            float step = attackRange / segments;

            for (int i = 1; i <= segments; i++)
            {
                Vector3 sampleCenter = origin + forward * (step * i);

                Collider[] hits = Physics.OverlapSphere(
                    sampleCenter,
                    attackRadius,
                    enemyLayers,
                    QueryTriggerInteraction.Ignore
                );

                for (int h = 0; h < hits.Length; h++)
                {
                    EnemyChaser enemy = hits[h].GetComponentInParent<EnemyChaser>();
                    if (enemy == null) continue;

                    Vector3 toEnemy = enemy.transform.position - origin;
                    toEnemy.y = 0f;

                    if (toEnemy.sqrMagnitude <= 0.001f)
                        continue;

                    float angle = Vector3.Angle(forward, toEnemy.normalized);
                    if (angle > maxAttackAngle)
                        continue;

                    hitEnemies.Add(enemy);
                }
            }

            bool hitAny = false;
            int successCount = 0;

            foreach (EnemyChaser enemy in hitEnemies)
            {
                if (enemy == null) continue;

                bool success = enemy.TryRepelFrom(transform.position);
                if (success)
                {
                    hitAny = true;
                    successCount++;
                }
            }

            Log(hitAny
                ? $"Delayed hit frame SUCCESS. Valid enemies found={hitEnemies.Count}, repelled={successCount}."
                : $"Delayed hit frame missed. Valid enemies found={hitEnemies.Count}.");
        }

        private Vector3 GetAttackForward()
        {
            if (_cam == null)
                _cam = Camera.main;

            Vector3 forward = _cam != null
                ? _cam.transform.forward
                : transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            forward.Normalize();

            return forward;
        }

        private void PlayTorchSwingSFX()
        {
            if (torchSwingAudioSource == null || torchSwingSFX == null)
                return;

            torchSwingAudioSource.PlayOneShot(torchSwingSFX, torchSwingVolume);
        }

        private void OnDisable()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug) return;

            Transform originTf = attackOrigin != null ? attackOrigin : transform;
            Vector3 origin = originTf.position;

            Camera cam = Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            forward.Normalize();

            int segments = Mathf.Max(1, debugSegments);
            float step = attackRange / segments;

            Gizmos.color = Color.red;

            for (int i = 1; i <= segments; i++)
            {
                Vector3 sampleCenter = origin + forward * (step * i);
                Gizmos.DrawWireSphere(sampleCenter, attackRadius);
            }

            Gizmos.DrawLine(origin, origin + forward * attackRange);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[PlayerTorchRepel] {message}", this);
        }
    }
}