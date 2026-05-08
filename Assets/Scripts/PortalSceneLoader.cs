using UnityEngine;
using UnityEngine.SceneManagement;
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class PortalSceneLoader : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string sceneToLoad = "EndingStoryScene";

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered)
                return;

            PlayerState player = other.GetComponentInParent<PlayerState>();
            if (player == null)
                return;

            _hasTriggered = true;

            if (debugLogs)
                Debug.Log($"[PortalSceneLoader] Loading scene: {sceneToLoad}", this);

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}