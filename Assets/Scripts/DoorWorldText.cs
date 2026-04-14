using System.Collections;
using TMPro;
using UnityEngine;

namespace DarkMazeMinimal
{
    public class DoorWorldText : MonoBehaviour
    {
        [SerializeField] private GameObject[] textRoots;
        [SerializeField] private TMP_Text[] textLabels;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            SetActiveAll(false);
        }

        public void Show(string message, float duration = 1.5f)
        {
            if (textRoots == null || textLabels == null) return;

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            for (int i = 0; i < textLabels.Length; i++)
            {
                if (textLabels[i] != null)
                    textLabels[i].text = message;
            }

            SetActiveAll(true);

            _hideRoutine = StartCoroutine(HideAfterDelay(duration));
        }

        public void HideNow()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            SetActiveAll(false);
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetActiveAll(false);
            _hideRoutine = null;
        }

        private void SetActiveAll(bool active)
        {
            for (int i = 0; i < textRoots.Length; i++)
            {
                if (textRoots[i] != null)
                    textRoots[i].SetActive(active);
            }
        }
    }
}