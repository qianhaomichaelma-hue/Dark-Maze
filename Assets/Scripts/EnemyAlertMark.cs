using UnityEngine;

namespace DarkMazeMinimal
{
    public class EnemyAlertMark : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private bool faceCamera = true;

        private Camera _cam;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = gameObject;

            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (!faceCamera) return;

            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null) return;

            Vector3 dir = transform.position - _cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot.activeSelf != visible)
                visualRoot.SetActive(visible);
        }
    }
}