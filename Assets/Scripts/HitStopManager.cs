using System.Collections;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        [Header("Default Hit Stop")]
        [SerializeField] private float defaultDuration = 0.18f;
        [SerializeField] private float defaultTimeScale = 0.01f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private Coroutine _routine;
        private float _defaultFixedDeltaTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[HitStopManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;

            Log("Awake. Instance registered.");
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;

            Log("OnDisable. Time scale reset.");
        }

        public void PlayDefaultHitStop()
        {
            PlayHitStop(defaultDuration, defaultTimeScale);
        }

        public void PlayHitStop(float duration, float timeScale)
        {
            if (duration <= 0f)
            {
                Log("PlayHitStop ignored. Duration <= 0.");
                return;
            }

            timeScale = Mathf.Clamp(timeScale, 0.01f, 1f);

            if (_routine != null)
            {
                StopCoroutine(_routine);
                Log("Previous hit stop routine stopped.");
            }

            Log($"PlayHitStop called. duration={duration}, timeScale={timeScale}");

            _routine = StartCoroutine(HitStopRoutine(duration, timeScale));
        }

        private IEnumerator HitStopRoutine(float duration, float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * timeScale;

            Log($"Hit stop START. Time.timeScale={Time.timeScale}");

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;

            Log($"Hit stop END. Time.timeScale={Time.timeScale}");

            _routine = null;
        }

        [ContextMenu("DEBUG / Test Big Hit Stop")]
        private void TestBigHitStop()
        {
            PlayHitStop(0.35f, 0.01f);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[HitStopManager] {message}", this);
        }
    }
}