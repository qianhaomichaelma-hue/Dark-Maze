using UnityEngine;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(Rigidbody))]
    public class StoneProjectile : MonoBehaviour
    {
        [Header("Lure Spawn")]
        public GameObject lurePrefab;
        public float lureDuration = 3f;

        [Header("Safety")]
        public float selfDestructAfter = 8f;

        private bool _spawned;

        private void Start()
        {
            if (selfDestructAfter > 0.1f)
                Destroy(gameObject, selfDestructAfter);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_spawned) return;
            _spawned = true;

            Debug.Log($"[StoneProjectile] Landed on {collision.collider.name}", this);

            if (lurePrefab != null)
            {
                GameObject lureGO = Instantiate(lurePrefab, transform.position, Quaternion.identity);
                var lure = lureGO.GetComponent<LureZone>();
                if (lure != null) lure.duration = lureDuration;

                Debug.Log($"[StoneProjectile] Spawned LureZone: {lureGO.name} | duration={lureDuration}", lureGO);
            }
            else
            {
                Debug.LogWarning("[StoneProjectile] lurePrefab not assigned!", this);
            }

            Destroy(gameObject);
        }
    }
}

