using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class EscortOnlyBlocker : MonoBehaviour
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Header("Blockers")]
        [Tooltip("If empty, the script will use all Collider components on this object and children.")]
        [SerializeField] private Collider[] blockerColliders;

        [Header("Rules")]
        [Tooltip("When true, the blocker stays disabled after the player has reached final dialogue / completed the rescue.")]
        [SerializeField] private bool stayOpenAfterEscortGoal = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool _permanentlyOpen;

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();

            if (blockerColliders == null || blockerColliders.Length == 0)
                blockerColliders = GetComponentsInChildren<Collider>(true);

            RefreshBlocker();
        }

        private void Update()
        {
            RefreshBlocker();
        }

        private void RefreshBlocker()
        {
            bool shouldBlock = ShouldBlockPlayer();

            SetBlockersEnabled(shouldBlock);
        }

        private bool ShouldBlockPlayer()
        {
            if (quest == null)
                return true;

            if (_permanentlyOpen)
                return false;

            if (quest.IsEscorting)
                return false;

            if (stayOpenAfterEscortGoal && (quest.IsWaitingFinalDialogue || quest.IsCompleted))
            {
                _permanentlyOpen = true;
                return false;
            }

            return true;
        }

        private void SetBlockersEnabled(bool enabled)
        {
            if (blockerColliders == null)
                return;

            for (int i = 0; i < blockerColliders.Length; i++)
            {
                if (blockerColliders[i] == null)
                    continue;

                if (blockerColliders[i].enabled != enabled)
                    blockerColliders[i].enabled = enabled;
            }

            if (debugLogs)
                Debug.Log($"[EscortOnlyBlocker] Blocker enabled = {enabled}", this);
        }
    }
}