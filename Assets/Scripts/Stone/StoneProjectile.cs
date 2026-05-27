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

        [Header("Spawn Offset")]
        [SerializeField] private float lureSpawnUpOffset = 0.05f;

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

            Vector3 spawnPos = transform.position;

            if (collision.contactCount > 0)
            {
                spawnPos = collision.GetContact(0).point + Vector3.up * lureSpawnUpOffset;
            }

            Debug.Log($"[StoneProjectile] Landed on {collision.collider.name}", this);

            if (lurePrefab != null)
            {
                GameObject lureGO = Instantiate(lurePrefab, spawnPos, Quaternion.identity);

                LureZone lure = lureGO.GetComponent<LureZone>();
                if (lure != null)
                {
                    lure.duration = lureDuration;
                }
                else
                {
                    Debug.LogWarning("[StoneProjectile] Spawned lurePrefab has no LureZone component.", lureGO);
                }

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