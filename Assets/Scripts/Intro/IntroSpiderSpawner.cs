using System.Collections.Generic;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class IntroSpiderSpawner : MonoBehaviour
    {
        private static readonly List<IntroSpiderSpawner> ActiveSpawners = new List<IntroSpiderSpawner>();

        [Header("Player")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private PlayerState player;

        [Header("Spawn")]
        [SerializeField] private GameObject spiderPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Enemy Runtime References")]
        [Tooltip("Usually the spider spawn point or a nearby empty object. Used as the enemy's home point.")]
        [SerializeField] private Transform enemyHomePoint;

        [Tooltip("Center of the intro enemy's allowed activity area. If empty, enemyHomePoint will be used.")]
        [SerializeField] private Transform enemyActivityCenter;

        [Tooltip("If true, the spawner will initialize EnemyChaser references after spawning.")]
        [SerializeField] private bool initializeEnemyChaser = true;

        [Tooltip("If you already placed the spider in the scene, drag it here instead of using a prefab.")]
        [SerializeField] private IntroSpiderActor sceneSpider;

        [Header("Rules")]
        [SerializeField] private bool spawnOnlyOnce = true;
        [SerializeField] private bool activateSceneSpiderOnStart = false;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private IntroSpiderActor currentSpider;
        private bool hasSpawned;

        private bool playerInsideSafeZone;
        private Transform lastExitPoint;

        public IntroSpiderActor CurrentSpider => currentSpider;

        private void OnEnable()
        {
            if (!ActiveSpawners.Contains(this))
                ActiveSpawners.Add(this);
        }

        private void OnDisable()
        {
            ActiveSpawners.Remove(this);
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
                col.isTrigger = true;

            FindPlayerIfNeeded();

            if (enemyHomePoint == null)
                enemyHomePoint = spawnPoint;

            if (enemyActivityCenter == null)
                enemyActivityCenter = enemyHomePoint;

            if (sceneSpider != null)
            {
                currentSpider = sceneSpider;
                RegisterSpider(currentSpider);
                InitializeSpawnedEnemy(sceneSpider.gameObject);

                sceneSpider.SetDestroyOnDismiss(false);

                if (!activateSceneSpiderOnStart)
                    sceneSpider.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            SpawnSpider();
        }

        public void SpawnSpider()
        {
            if (spawnOnlyOnce && hasSpawned)
            {
                Log("Spawn ignored because hasSpawned is true.");
                return;
            }

            if (currentSpider != null && currentSpider.gameObject.activeInHierarchy)
            {
                Log("Spawn ignored because currentSpider already exists.");
                return;
            }

            hasSpawned = true;

            FindPlayerIfNeeded();

            if (sceneSpider != null)
            {
                currentSpider = sceneSpider;
                RegisterSpider(currentSpider);

                currentSpider.ResetForReuse();
                InitializeSpawnedEnemy(currentSpider.gameObject);

                currentSpider.gameObject.SetActive(true);

                Log("Scene spider activated.");

                if (playerInsideSafeZone)
                    DismissCurrentSpider(lastExitPoint);

                return;
            }

            if (spiderPrefab == null)
            {
                Debug.LogWarning("[IntroSpiderSpawner] Spider Prefab is missing.", this);
                return;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            GameObject spiderObj = Instantiate(spiderPrefab, pos, rot);

            currentSpider = spiderObj.GetComponent<IntroSpiderActor>();

            if (currentSpider == null)
            {
                currentSpider = spiderObj.AddComponent<IntroSpiderActor>();
                Debug.LogWarning("[IntroSpiderSpawner] Spawned spider did not have IntroSpiderActor. Added automatically.", spiderObj);
            }

            RegisterSpider(currentSpider);
            InitializeSpawnedEnemy(spiderObj);

            Log("Prefab spider spawned and initialized.");

            if (playerInsideSafeZone)
                DismissCurrentSpider(lastExitPoint);
        }

        private void InitializeSpawnedEnemy(GameObject spiderObj)
        {
            if (!initializeEnemyChaser)
                return;

            if (spiderObj == null)
                return;

            FindPlayerIfNeeded();

            if (enemyHomePoint == null)
                enemyHomePoint = spawnPoint;

            if (enemyActivityCenter == null)
                enemyActivityCenter = enemyHomePoint;

            EnemyChaser enemy = spiderObj.GetComponent<EnemyChaser>();

            if (enemy == null)
                enemy = spiderObj.GetComponentInChildren<EnemyChaser>(true);

            if (enemy == null)
            {
                Debug.LogWarning("[IntroSpiderSpawner] Spawned spider has no EnemyChaser.", spiderObj);
                return;
            }

            enemy.InitializeRuntimeReferences(
                player,
                enemyHomePoint,
                enemyActivityCenter
            );

            Log(
                $"Initialized EnemyChaser | player={(player ? player.name : "NULL")} | home={(enemyHomePoint ? enemyHomePoint.name : "NULL")} | activity={(enemyActivityCenter ? enemyActivityCenter.name : "NULL")}"
            );
        }

        private void FindPlayerIfNeeded()
        {
            if (player != null)
                return;

            GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);

            if (playerGO != null)
                player = playerGO.GetComponent<PlayerState>();

            if (player == null)
                Debug.LogWarning("[IntroSpiderSpawner] Could not find PlayerState. Check Player tag and PlayerState component.", this);
        }

        public void SetPlayerInsideSafeZone(bool inside, Transform exitPoint)
        {
            playerInsideSafeZone = inside;

            if (exitPoint != null)
                lastExitPoint = exitPoint;

            Log($"SetPlayerInsideSafeZone = {inside}");

            if (inside)
                DismissCurrentSpider(lastExitPoint);
        }

        public bool DismissCurrentSpider(Transform exitPoint)
        {
            if (currentSpider == null)
            {
                Log("Dismiss ignored because currentSpider is null.");
                return false;
            }

            if (!currentSpider.gameObject.activeInHierarchy)
            {
                Log("Dismiss ignored because currentSpider is inactive.");
                return false;
            }

            if (currentSpider.IsDismissing)
            {
                Log("Dismiss ignored because spider is already dismissing.");
                return false;
            }

            if (exitPoint != null)
                lastExitPoint = exitPoint;

            currentSpider.Dismiss(lastExitPoint);

            Log("Dismiss current spider.");

            return true;
        }

        public void ResetForNewLife()
        {
            if (currentSpider != null)
            {
                currentSpider.OnRemoved -= HandleSpiderRemoved;
                currentSpider.ForceRemoveImmediately();
                currentSpider = null;
            }

            hasSpawned = false;
            playerInsideSafeZone = false;

            Log("ResetForNewLife.");
        }

        public static void ResetAllForNewLife()
        {
            for (int i = ActiveSpawners.Count - 1; i >= 0; i--)
            {
                if (ActiveSpawners[i] != null)
                    ActiveSpawners[i].ResetForNewLife();
            }
        }

        private void RegisterSpider(IntroSpiderActor spider)
        {
            if (spider == null)
                return;

            spider.OnRemoved -= HandleSpiderRemoved;
            spider.OnRemoved += HandleSpiderRemoved;
        }

        private void HandleSpiderRemoved(IntroSpiderActor spider)
        {
            if (currentSpider == spider)
                currentSpider = null;

            Log("Spider removed callback.");
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[IntroSpiderSpawner] {message}", this);
        }
    }
}