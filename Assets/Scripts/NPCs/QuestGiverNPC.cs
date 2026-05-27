using UnityEngine;
using DarkMazePlayer;
using DarkMazeItems;
using DarkMazeUI;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class QuestGiverNPC : MonoBehaviour, IInteractable
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Header("Torch Reward")]
        [SerializeField] private ItemData torchItem;
        [SerializeField] private int torchAmount = 1;
        [SerializeField] private bool giveTorchOnFirstDialogue = true;
        [SerializeField] private bool equipTorchImmediately = true;

        [Header("Reward Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip torchGainClip;
        [Range(0f, 1f)]
        [SerializeField] private float torchGainVolume = 1f;

        [Header("Optional Visuals")]
        [Tooltip("Player hand torch model, torch light, or any object that should appear after getting the torch.")]
        [SerializeField] private GameObject[] objectsToEnableAfterTorchGiven;

        [Header("Objective")]
        [SerializeField] private bool updateObjectiveAfterFirstDialogue = true;
        [SerializeField] private string objectiveAfterFirstDialogue = "Light the first campfire";

        [Header("Speaker")]
        [SerializeField] private string speakerName = "Old Survivor";

        [Header("Dialogue - First Time")]
        [TextArea(2, 5)]
        [SerializeField]
        private string[] firstDialogueLines =
        {
            "You made it to the fire.",
            "The Princess was taken deeper into the cardboard cave.",
            "Take this torch. Spiders fear fire.",
            "Light the campfires and follow the drawings."
        };

        [Header("Dialogue - Repeat")]
        [TextArea(2, 5)]
        [SerializeField]
        private string repeatBeforeRescue =
            "Light the campfires. The drawings will show you the way.";

        [TextArea(2, 5)]
        [SerializeField]
        private string repeatWhileEscorting =
            "Bring them here. Do not let the dark take you.";

        [TextArea(2, 5)]
        [SerializeField]
        private string repeatAfterComplete =
            "You brought them back. Thank you.";

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool hasGivenTorch = false;
        private bool isTalking = false;

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (quest == null)
            {
                LogWarning("Quest reference is missing.");
                return;
            }

            if (interactor == null)
            {
                LogWarning("Interactor is null.");
                return;
            }

            if (isTalking)
                return;

            if (DialogueUI.Instance == null)
            {
                HandleFirstDialogueFinished(interactor);
                return;
            }

            switch (quest.State)
            {
                case RescueQuestState.NotStarted:
                    isTalking = true;

                    DialogueUI.Instance.ShowLines(speakerName, firstDialogueLines, () =>
                    {
                        isTalking = false;
                        HandleFirstDialogueFinished(interactor);
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

        private void HandleFirstDialogueFinished(PlayerInteractor interactor)
        {
            if (giveTorchOnFirstDialogue)
                TryGiveTorch(interactor);

            if (updateObjectiveAfterFirstDialogue && ObjectiveUI.Instance != null)
                ObjectiveUI.Instance.SetObjective(objectiveAfterFirstDialogue);

            if (quest != null && quest.State == RescueQuestState.NotStarted)
                quest.StartQuest();

            Log("First dialogue finished. Torch given and quest assigned.");
        }

        private void TryGiveTorch(PlayerInteractor interactor)
        {
            if (hasGivenTorch)
                return;

            if (torchItem == null)
            {
                LogWarning("Torch ItemData is missing.");
                return;
            }

            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
            PlayerEquipment equipment = interactor.Equipment;

            bool success = false;

            if (inventory != null)
            {
                if (!inventory.Has(torchItem, 1))
                {
                    success = inventory.TryAdd(torchItem, torchAmount);
                }
                else
                {
                    success = true;
                }

                if (success && equipTorchImmediately)
                    inventory.EquipItem(torchItem);
            }
            else if (equipment != null)
            {
                equipment.Hold(torchItem);
                success = true;
            }

            if (!success)
            {
                LogWarning("Failed to give torch. Check PlayerInventory capacity.");
                return;
            }

            hasGivenTorch = true;

            EnableObjectsAfterTorchGiven();
            PlayTorchGainSound();

            if (ItemGainPopupUI.Instance != null)
                ItemGainPopupUI.Instance.ShowItemGain(torchItem.displayName, torchAmount);

            Log("Torch given to player.");
        }

        private void PlayTorchGainSound()
        {
            if (torchGainClip == null)
                return;

            if (audioSource != null)
            {
                audioSource.PlayOneShot(torchGainClip, torchGainVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(torchGainClip, transform.position, torchGainVolume);
            }
        }

        private void EnableObjectsAfterTorchGiven()
        {
            if (objectsToEnableAfterTorchGiven == null)
                return;

            for (int i = 0; i < objectsToEnableAfterTorchGiven.Length; i++)
            {
                if (objectsToEnableAfterTorchGiven[i] != null)
                    objectsToEnableAfterTorchGiven[i].SetActive(true);
            }
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[QuestGiverNPC] {message}", this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[QuestGiverNPC] {message}", this);
        }
    }
}