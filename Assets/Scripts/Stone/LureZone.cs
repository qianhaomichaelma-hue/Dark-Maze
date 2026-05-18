using System.Collections.Generic;
using UnityEngine;

namespace DarkMazeMinimal
{
    /// <summary>
    /// A temporary lure target in world space.
    /// Enemies can query active lures without expensive scene searches.
    /// </summary>
    public class LureZone : MonoBehaviour
    {
        private static readonly List<LureZone> _active = new List<LureZone>();
        public static IReadOnlyList<LureZone> Active => _active;

        [Header("Lifetime")]
        public float duration = 3f;

        [Header("Optional")]
        public float gizmoRadius = 0.6f;

        private float _dieTime;

        private void OnEnable()
        {
            if (!_active.Contains(this)) _active.Add(this);
            _dieTime = Time.time + Mathf.Max(0.01f, duration);

            Debug.Log($"[LureZone] Spawned: {name} | duration={duration}", this);
        }

        private void OnDisable()
        {
            _active.Remove(this);
        }

        private void Update()
        {
            if (Time.time >= _dieTime)
            {
                Debug.Log($"[LureZone] Expired: {name}", this);
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}

