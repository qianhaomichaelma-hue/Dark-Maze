using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text tutorialText;

    private Coroutine _hideRoutine;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void ShowMessage(string message, float duration = 3f)
    {
        if (panel == null || tutorialText == null)
        {
            Debug.LogWarning("[TutorialPopupUI] Panel or Text is missing.");
            return;
        }

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        tutorialText.text = message;
        panel.SetActive(true);
        _hideRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    public void HideNow()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (panel != null)
            panel.SetActive(false);

        _hideRoutine = null;
    }
}