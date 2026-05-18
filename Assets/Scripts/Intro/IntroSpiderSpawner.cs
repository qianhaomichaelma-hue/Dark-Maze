using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class IntroSpiderSpawner : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private string playerTag = "Player";

        [Header("Spawn")]
        [SerializeField] private GameObject spiderPrefab;
        [SerializeField] private Transform spawnPoint;

        [Tooltip("If you already placed the spider in the scene, drag it here instead of using a prefab.")]
        [SerializeField] private IntroSpiderActor sceneSpider;

        [SerializeField] private bool spawnOnlyOnce = true;
        [SerializeField] private bool activateSceneSpiderOnStart = false;

        private IntroSpiderActor currentSpider;
        private bool hasSpawned;

        public IntroSpiderActor CurrentSpider => currentSpider;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (sceneSpider != null)
            {
                currentSpider = sceneSpider;

                if (!activateSceneSpiderOnStart)
                    sceneSpider.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            SpawnSpider();
        }

        public void SpawnSpider()
        {
            if (spawnOnlyOnce && hasSpawned) return;

            hasSpawned = true;

            if (sceneSpider != null)
            {
                sceneSpider.gameObject.SetActive(true);
                currentSpider = sceneSpider;
                return;
            }

            if (spiderPrefab == null)
            {
                Debug.LogWarning("[IntroSpiderSpawner] Spider Prefab is missing.");
                return;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            GameObject spiderObj = Instantiate(spiderPrefab, pos, rot);

            currentSpider = spiderObj.GetComponent<IntroSpiderActor>();

            if (currentSpider == null)
            {
                currentSpider = spiderObj.AddComponent<IntroSpiderActor>();
                Debug.LogWarning("[IntroSpiderSpawner] Spawned spider did not have IntroSpiderActor. Added automatically.");
            }
        }

        public void DismissCurrentSpider(Transform exitPoint)
        {
            if (currentSpider == null) return;
            currentSpider.Dismiss(exitPoint);
        }
    }
}