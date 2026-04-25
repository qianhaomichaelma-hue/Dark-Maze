using UnityEngine;
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class QuestGiverNPC : MonoBehaviour, IInteractable
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Header("Speaker")]
        [SerializeField] private string speakerName = "Old Survivor";

        [Header("Dialogue - First Time")]
        [TextArea(2, 5)]
        [SerializeField] private string[] firstDialogueLines;

        [Header("Dialogue - Repeat")]
        [TextArea(2, 5)]
        [SerializeField] private string repeatBeforeRescue = "Please, find the one trapped in the enemy cave.";

        [TextArea(2, 5)]
        [SerializeField] private string repeatWhileEscorting = "Bring them here. Do not let the dark take you.";

        [TextArea(2, 5)]
        [SerializeField] private string repeatAfterComplete = "You brought them back. Thank you.";

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (quest == null)
                return;

            if (DialogueUI.Instance == null)
            {
                if (quest.State == RescueQuestState.NotStarted)
                    quest.StartQuest();

                return;
            }

            switch (quest.State)
            {
                case RescueQuestState.NotStarted:
                    DialogueUI.Instance.ShowLines(speakerName, firstDialogueLines, () =>
                    {
                        quest.StartQuest();
                        Log("First dialogue finished. Quest assigned.");
                    });
                    break;

                case RescueQuestState.TaskAssigned:
                    DialogueUI.Instance.ShowSingleLine(speakerName, repeatBeforeRescue);
                    break;

                case RescueQuestState.Escorting:
                    DialogueUI.Instance.ShowSingleLine(speakerName, repeatWhileEscorting);
                    break;

                case RescueQuestState.Completed:
                    DialogueUI.Instance.ShowSingleLine(speakerName, repeatAfterComplete);
                    break;
            }
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[QuestGiverNPC] {message}", this);
        }
    }
}