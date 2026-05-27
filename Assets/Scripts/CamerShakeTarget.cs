using System.Collections;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class CameraShakeTarget : MonoBehaviour
    {
        public static CameraShakeTarget Instance { get; private set; }

        [Header("Default Shake")]
        [SerializeField] private float defaultDuration = 0.45f;
        [SerializeField] private float defaultStrength = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private Vector3 _baseLocalPosition;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            Instance = this;
            _baseLocalPosition = transform.localPosition;

            Log($"Awake. Instance registered. Base local position = {_baseLocalPosition}");
        }

        private void OnDisable()
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeRoutine = null;
            }

            transform.localPosition = _baseLocalPosition;

            Log("OnDisable. Position reset.");
        }

        public void PlayDefaultShake()
        {
            Shake(defaultDuration, defaultStrength);
        }

        public void Shake(float duration, float strength)
        {
            if (duration <= 0f || strength <= 0f)
            {
                Log($"Shake ignored. duration={duration}, strength={strength}");
                return;
            }

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                Log("Previous shake routine stopped.");
            }

            Log($"Shake called. duration={duration}, strength={strength}");

            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(timer / duration);
                float fade = 1f - progress;

                Vector2 offset = Random.insideUnitCircle * strength * fade;

                transform.localPosition = _baseLocalPosition + new Vector3(
                    offset.x,
                    offset.y,
                    0f
                );

                yield return null;
            }

            transform.localPosition = _baseLocalPosition;
            _shakeRoutine = null;

            Log("Shake finished. Position reset.");
        }

        [ContextMenu("DEBUG / Test Big Shake")]
        private void TestBigShake()
        {
            Shake(0.6f, 0.35f);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[CameraShakeTarget] {message}", this);
        }
    }
}