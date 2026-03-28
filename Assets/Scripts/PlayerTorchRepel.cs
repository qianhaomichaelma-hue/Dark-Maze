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

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private int debugSegments = 6;

        private StarterAssetsInputs _inputs;
        private PlayerEquipment _equipment;
        private PlayerState _state;
        private Camera _cam;

        private float _nextReadyTime = 0f;

        public bool IsOnCooldown => Time.time < _nextReadyTime;
        public float CooldownRemaining => Mathf.Max(0f, _nextReadyTime - Time.time);

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
            _equipment = GetComponent<PlayerEquipment>();
            _state = GetComponent<PlayerState>();
            _cam = Camera.main;

            if (attackOrigin == null)
                attackOrigin = transform;
        }

        private void Update()
        {
            if (_inputs == null) return;
            if (_state != null && _state.IsDead) return;

            if (_inputs.attack)
            {
                _inputs.attack = false;
                TryRepel();
            }
        }

        private void TryRepel()
        {
            if (_equipment == null || torchItem == null)
                return;

            if (!_equipment.IsHolding(torchItem))
            {
                Debug.Log("[PlayerTorchRepel] Not holding torch.");
                return;
            }

            if (Time.time < _nextReadyTime)
            {
                Debug.Log($"[PlayerTorchRepel] Cooldown: {CooldownRemaining:F1}s");
                return;
            }

            if (_cam == null)
                _cam = Camera.main;

            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + Vector3.up;

            Vector3 forward = _cam != null ? _cam.transform.forward : transform.forward;
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

            foreach (EnemyChaser enemy in hitEnemies)
            {
                if (enemy == null) continue;

                bool success = enemy.TryRepelFrom(transform.position);
                if (success)
                    hitAny = true;
            }

            _nextReadyTime = Time.time + cooldown;

            Debug.Log(hitAny
                ? $"[PlayerTorchRepel] Hit {hitEnemies.Count} enemy(s)."
                : "[PlayerTorchRepel] Attack used, no valid enemy hit.");
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
    }
}