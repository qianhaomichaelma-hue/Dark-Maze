using UnityEngine;

public class TorchAttackTutorialTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TutorialPopupUI tutorialUI;

    [Header("Message")]
    [TextArea]
    [SerializeField] private string message = "Press Attack to repel enemies with your torch.";
    [SerializeField] private float showDuration = 3f;

    [Header("Rules")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool _hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (tutorialUI == null)
        {
            Debug.LogWarning("[TorchAttackTutorialTrigger] Tutorial UI is missing.");
            return;
        }

        tutorialUI.ShowMessage(message, showDuration);

        if (triggerOnlyOnce)
            _hasTriggered = true;
    }
}
