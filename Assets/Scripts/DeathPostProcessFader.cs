using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class DeathPostProcessFader : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Volume deathVolume;

        [Header("Timing")]
        [SerializeField] private float fadeToBlackTime = 0.7f;
        [SerializeField] private float fadeFromBlackTime = 0.8f;

        [Header("Curve")]
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Options")]
        [SerializeField] private bool resetWeightOnAwake = true;
        [SerializeField] private bool useUnscaledTime = false;

        private void Awake()
        {
            if (deathVolume == null)
                deathVolume = GetComponent<Volume>();

            if (deathVolume != null && resetWeightOnAwake)
                deathVolume.weight = 0f;
        }

        public IEnumerator FadeToBlack()
        {
            if (deathVolume == null)
                yield break;

            yield return FadeWeight(deathVolume.weight, 1f, fadeToBlackTime);
        }

        public IEnumerator FadeFromBlack()
        {
            if (deathVolume == null)
                yield break;

            yield return FadeWeight(deathVolume.weight, 0f, fadeFromBlackTime);
        }

        public void SetBlackInstant()
        {
            if (deathVolume != null)
                deathVolume.weight = 1f;
        }

        public void SetClearInstant()
        {
            if (deathVolume != null)
                deathVolume.weight = 0f;
        }

        private IEnumerator FadeWeight(float from, float to, float duration)
        {
            if (deathVolume == null)
                yield break;

            if (duration <= 0f)
            {
                deathVolume.weight = to;
                yield break;
            }

            float timer = 0f;

            while (timer < duration)
            {
                timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float t = Mathf.Clamp01(timer / duration);
                float curvedT = fadeCurve != null ? fadeCurve.Evaluate(t) : t;

                deathVolume.weight = Mathf.Lerp(from, to, curvedT);

                yield return null;
            }

            deathVolume.weight = to;
        }
    }
}
