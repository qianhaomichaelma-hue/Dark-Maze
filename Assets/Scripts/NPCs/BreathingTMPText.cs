using TMPro;
using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class BreathingTMPText : MonoBehaviour
    {
        [Header("Breathing")]
        [SerializeField] private float minAlpha = 0.25f;
        [SerializeField] private float maxAlpha = 1.0f;
        [SerializeField] private float speed = 2.2f;

        [Header("Rules")]
        [Tooltip("If true, the text becomes fully invisible when its content is empty.")]
        [SerializeField] private bool hideWhenEmpty = true;

        private TMP_Text _text;
        private Color _baseColor;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseColor = _text.color;
        }

        private void Update()
        {
            if (_text == null)
                return;

            if (hideWhenEmpty && string.IsNullOrWhiteSpace(_text.text))
            {
                SetAlpha(0f);
                return;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            Color c = _baseColor;
            c.a = alpha;
            _text.color = c;
        }
    }
}