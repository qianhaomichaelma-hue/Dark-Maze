using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class InteractPromptTarget : MonoBehaviour
    {
        [Header("Prompt")]
        [SerializeField] private GameObject promptRoot;

        [Header("Billboard")]
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool hideOnStart = true;

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;

            if (promptRoot != null && hideOnStart)
                promptRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!faceCamera) return;
            if (promptRoot == null || !promptRoot.activeSelf) return;

            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null)
                return;

            Vector3 dir = promptRoot.transform.position - _cam.transform.position;

            if (dir.sqrMagnitude < 0.001f)
                return;

            promptRoot.transform.rotation = Quaternion.LookRotation(dir);
        }

        public void Show()
        {
            if (promptRoot != null && !promptRoot.activeSelf)
                promptRoot.SetActive(true);
        }

        public void Hide()
        {
            if (promptRoot != null && promptRoot.activeSelf)
                promptRoot.SetActive(false);
        }
    }
}