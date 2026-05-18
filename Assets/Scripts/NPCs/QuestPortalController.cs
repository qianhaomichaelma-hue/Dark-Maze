using UnityEngine;
using UnityEngine.Events;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class QuestPortalController : MonoBehaviour
    {
        [Header("Portal")]
        [SerializeField] private GameObject portalRoot;

        [Header("Settings")]
        [SerializeField] private bool hideOnStart = true;

        [Header("Events")]
        public UnityEvent onPortalShown;

        private void Awake()
        {
            if (portalRoot == null)
                portalRoot = gameObject;

            if (hideOnStart)
                portalRoot.SetActive(false);
        }

        public void ShowPortal()
        {
            if (portalRoot != null)
                portalRoot.SetActive(true);

            onPortalShown?.Invoke();
        }

        public void HidePortal()
        {
            if (portalRoot != null)
                portalRoot.SetActive(false);
        }
    }
}