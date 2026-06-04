using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class CinematicCameraMover : MonoBehaviour
    {
        [Header("Move Points")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;

        [Header("Timing")]
        [SerializeField] private float moveDuration = 3.0f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Settings")]
        [Tooltip("When this camera object becomes active, movement restarts from Start Point.")]
        [SerializeField] private bool restartOnEnable = true;

        [Tooltip("If true, camera will instantly snap to Start Point when enabled.")]
        [SerializeField] private bool snapToStartOnEnable = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private float _timer;
        private bool _moving;

        private void OnEnable()
        {
            if (!restartOnEnable)
                return;

            RestartMove();
        }

        public void RestartMove()
        {
            if (startPoint == null || endPoint == null)
            {
                if (debugLogs)
                    Debug.LogWarning("[CinematicCameraMover] Start Point or End Point missing.", this);

                return;
            }

            _timer = 0f;
            _moving = true;

            if (snapToStartOnEnable)
            {
                transform.position = startPoint.position;
                transform.rotation = startPoint.rotation;
            }
        }

        private void Update()
        {
            if (!_moving)
                return;

            if (startPoint == null || endPoint == null)
                return;

            float duration = Mathf.Max(0.01f, moveDuration);

            _timer += Time.deltaTime;

            float t = Mathf.Clamp01(_timer / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            transform.position = Vector3.LerpUnclamped(startPoint.position, endPoint.position, curvedT);
            transform.rotation = Quaternion.SlerpUnclamped(startPoint.rotation, endPoint.rotation, curvedT);

            if (t >= 1f)
                _moving = false;
        }
    }
}