using System.Collections.Generic;
using UnityEngine;
using DarkMazeItems;

namespace DarkMazeMinimal
{
    public class LureZone : MonoBehaviour
    {
        private static readonly List<LureZone> _active = new List<LureZone>();
        public static IReadOnlyList<LureZone> Active => _active;

        [Header("Lifetime")]
        public float duration = 3f;

        [Header("Recoverable Pickup")]
        [Tooltip("诱饵结束后生成的可拾取物 prefab。这里应该拖 Pickup prefab，不要拖投掷物 prefab。")]
        [SerializeField] private GameObject recoverablePickupPrefab;

        [Tooltip("如果没有设置 recoverablePickupPrefab，就用这个 ItemData 自动生成一个简单 pickup。")]
        [SerializeField] private ItemData fallbackPickupItem;

        [SerializeField] private int fallbackPickupAmount = 1;

        [Tooltip("防止 pickup 卡进地面。")]
        [SerializeField] private float pickupSpawnUpOffset = 0.08f;

        [Header("Ground Placement")]
        [SerializeField] private bool snapPickupToGround = true;
        [SerializeField] private float groundSnapRayHeight = 1.5f;
        [SerializeField] private float groundSnapRayDistance = 4f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Optional")]
        public float gizmoRadius = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private float _dieTime;
        private bool _expired;

        private void OnEnable()
        {
            if (!_active.Contains(this))
                _active.Add(this);

            _dieTime = Time.time + Mathf.Max(0.01f, duration);
            _expired = false;

            Log($"Spawned: {name} | duration={duration}");
        }

        private void OnDisable()
        {
            _active.Remove(this);
        }

        private void Update()
        {
            if (_expired)
                return;

            if (Time.time >= _dieTime)
            {
                Expire();
            }
        }

        private void Expire()
        {
            _expired = true;

            SpawnRecoverablePickup();

            Log($"Expired and spawned pickup: {name}");

            Destroy(gameObject);
        }

        private void SpawnRecoverablePickup()
        {
            Vector3 spawnPos = GetPickupSpawnPosition();

            if (recoverablePickupPrefab != null)
            {
                Instantiate(recoverablePickupPrefab, spawnPos, Quaternion.identity);
                return;
            }

            if (fallbackPickupItem == null)
            {
                Debug.LogWarning("[LureZone] No recoverablePickupPrefab or fallbackPickupItem assigned. No pickup spawned.", this);
                return;
            }

            GameObject pickupGO = new GameObject($"Pickup_{fallbackPickupItem.displayName}");
            pickupGO.transform.position = spawnPos;

            SphereCollider col = pickupGO.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;

            PickupItem pickup = pickupGO.AddComponent<PickupItem>();
            pickup.item = fallbackPickupItem;
            pickup.amount = Mathf.Max(1, fallbackPickupAmount);
            pickup.autoHoldOnPickup = false;
        }

        private Vector3 GetPickupSpawnPosition()
        {
            Vector3 pos = transform.position;

            if (snapPickupToGround)
            {
                Vector3 rayStart = transform.position + Vector3.up * groundSnapRayHeight;

                if (Physics.Raycast(
                    rayStart,
                    Vector3.down,
                    out RaycastHit hit,
                    groundSnapRayDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
                {
                    pos = hit.point;
                }
            }

            pos += Vector3.up * pickupSpawnUpOffset;
            return pos;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[LureZone] {message}", this);
        }
    }
}