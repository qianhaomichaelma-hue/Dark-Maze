using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class IntroSpiderSafeZoneDismiss : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private string playerTag = "Player";

        [Header("Intro Spider")]
        [SerializeField] private IntroSpiderSpawner spiderSpawner;
        [SerializeField] private Transform spiderExitPoint;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
                col.isTrigger = true;

            if (spiderSpawner == null)
                spiderSpawner = FindFirstObjectByType<IntroSpiderSpawner>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            if (spiderSpawner == null)
            {
                Debug.LogWarning("[IntroSpiderSafeZoneDismiss] Spider Spawner is missing.", this);
                return;
            }

            // 注意：这里不再有 dismissOnlyOnce。
            // 只有当前真的存在蜘蛛时，才会触发撤退。
            spiderSpawner.SetPlayerInsideSafeZone(true, spiderExitPoint);

            Log("Player entered intro safe zone.");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            if (spiderSpawner == null)
                return;

            spiderSpawner.SetPlayerInsideSafeZone(false, spiderExitPoint);

            Log("Player exited intro safe zone.");
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[IntroSpiderSafeZoneDismiss] {message}", this);
        }
    }
}