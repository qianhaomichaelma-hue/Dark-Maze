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

        [Header("Rules")]
        [SerializeField] private bool dismissOnlyOnce = true;

        private bool hasDismissed;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            if (dismissOnlyOnce && hasDismissed) return;

            hasDismissed = true;

            if (spiderSpawner == null)
            {
                Debug.LogWarning("[IntroSpiderSafeZoneDismiss] Spider Spawner is missing.");
                return;
            }

            spiderSpawner.DismissCurrentSpider(spiderExitPoint);
        }
    }
}