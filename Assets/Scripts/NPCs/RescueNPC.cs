using UnityEngine;
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class RescueNPC : MonoBehaviour, IInteractable
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Header("Speaker")]
        [SerializeField] private string speakerName = "Trapped Survivor";

        [Header("Dialogue Audio")]
        [SerializeField] private DialogueAudioProfile dialogueAudio = new DialogueAudioProfile();

        [Header("Objective Update")]
        [SerializeField] private bool updateObjectiveAfterFirstRescueDialogue = true;

        [TextArea(1, 3)]
        [SerializeField] private string objectiveAfterFirstRescueDialogue = "Carry the teddy bear back to safety";

        [Tooltip("If true, this objective is updated only once, after the first rescue dialogue.")]
        [SerializeField] private bool updateObjectiveOnlyOnce = true;

        [Header("Help Call Audio")]
        [SerializeField] private AudioSource helpAudioSource;
        [SerializeField] private AudioClip helpLoopSFX;

        [Range(0f, 1f)]
        [SerializeField] private float helpLoopVolume = 0.8f;

        [Tooltip("If true, help voice is 3D and comes from the trapped NPC position.")]
        [SerializeField] private bool helpVoiceAs3D = true;

        [SerializeField] private float helpMinDistance = 3f;
        [SerializeField] private float helpMaxDistance = 22f;

        [Header("Dialogue")]
        [TextArea(2, 5)]
        [SerializeField] private string notAssignedLine = "I do not know you. Please speak to the one outside first.";

        [TextArea(2, 5)]
        [SerializeField] private string[] rescueDialogueLines;

        [TextArea(2, 5)]
        [SerializeField] private string alreadyFollowingLine = "I am right behind you.";

        [TextArea(2, 5)]
        [SerializeField] private string waitingFinalDialogueLine = "Thank you... I can stand now. Please listen.";

        [TextArea(2, 5)]
        [SerializeField]
        private string[] finalDialogueLines =
        {
            "I thought the dark would keep me here forever.",
            "You came back for me.",
            "The way out is opening now."
        };

        [TextArea(2, 5)]
        [SerializeField] private string afterCompleteLine = "I am safe now.";

        [Header("Carry Visual")]
        [SerializeField] private Vector3 carriedLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 carriedLocalEuler = Vector3.zero;
        [SerializeField] private bool disableCollidersWhileCarried = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Transform _originalParent;
        private Vector3 _trappedPosition;
        private Quaternion _trappedRotation;
        private Collider[] _colliders;

        private bool _finalAreaActivated;
        private bool _isTalking;
        private bool _hasUpdatedObjectiveAfterFirstRescueDialogue;

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();

            _originalParent = transform.parent;
            _trappedPosition = transform.position;
            _trappedRotation = transform.rotation;

            _colliders = GetComponentsInChildren<Collider>(true);

            SetupHelpAudio();
            RefreshHelpLoop();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (quest == null)
                return;

            if (DialogueUI.Instance == null)
            {
                if (quest.State == RescueQuestState.TaskAssigned)
                {
                    UpdateObjectiveAfterFirstRescueDialogue();
                    TryStartEscort();
                }

                return;
            }

            switch (quest.State)
            {
                case RescueQuestState.NotStarted:
                    BeginTalking();

                    DialogueUI.Instance.ShowSingleLine(
                        speakerName,
                        notAssignedLine,
                        () =>
                        {
                            EndTalking();
                        },
                        dialogueAudio
                    );
                    break;

                case RescueQuestState.TaskAssigned:
                    BeginTalking();

                    DialogueUI.Instance.ShowLines(
                        speakerName,
                        rescueDialogueLines,
                        () =>
                        {
                            EndTalking();
                            UpdateObjectiveAfterFirstRescueDialogue();
                            TryStartEscort();
                        },
                        dialogueAudio
                    );
                    break;

                case RescueQuestState.Escorting:
                    DialogueUI.Instance.ShowSingleLine(
                        speakerName,
                        alreadyFollowingLine,
                        audioProfile: dialogueAudio
                    );
                    break;

                case RescueQuestState.WaitingFinalDialogue:
                    BeginTalking();

                    string[] linesToUse = finalDialogueLines;

                    if (linesToUse == null || linesToUse.Length == 0)
                        linesToUse = new[] { waitingFinalDialogueLine };

                    DialogueUI.Instance.ShowLines(
                        speakerName,
                        linesToUse,
                        () =>
                        {
                            EndTalking();

                            if (quest != null)
                                quest.CompleteQuestAfterFinalDialogue();
                        },
                        dialogueAudio
                    );
                    break;

                case RescueQuestState.Completed:
                    DialogueUI.Instance.ShowSingleLine(
                        speakerName,
                        afterCompleteLine,
                        audioProfile: dialogueAudio
                    );
                    break;
            }
        }

        private void UpdateObjectiveAfterFirstRescueDialogue()
        {
            if (!updateObjectiveAfterFirstRescueDialogue)
                return;

            if (updateObjectiveOnlyOnce && _hasUpdatedObjectiveAfterFirstRescueDialogue)
                return;

            if (ObjectiveUI.Instance == null)
            {
                Log("ObjectiveUI.Instance is missing. Objective not updated.");
                return;
            }

            ObjectiveUI.Instance.SetObjective(objectiveAfterFirstRescueDialogue);

            _hasUpdatedObjectiveAfterFirstRescueDialogue = true;

            Log($"Objective updated after first rescue dialogue: {objectiveAfterFirstRescueDialogue}");
        }

        public void ActivateFinalAreaHelpCall()
        {
            _finalAreaActivated = true;
            RefreshHelpLoop();

            Log("Final area activated. Help loop can now play.");
        }

        private void TryStartEscort()
        {
            if (quest == null)
                return;

            StopHelpLoop();

            quest.StartEscort(this);

            Log("Rescue dialogue finished. Escort started.");
        }

        public void AttachToCarryPoint(Transform carryPoint)
        {
            if (carryPoint == null)
                return;

            StopHelpLoop();

            transform.SetParent(carryPoint);
            transform.localPosition = carriedLocalPosition;
            transform.localRotation = Quaternion.Euler(carriedLocalEuler);

            SetCollidersEnabled(!disableCollidersWhileCarried);

            Log("Attached to player carry point.");
        }

        public void ResetToTrappedPosition()
        {
            transform.SetParent(_originalParent);
            transform.position = _trappedPosition;
            transform.rotation = _trappedRotation;

            SetCollidersEnabled(true);
            gameObject.SetActive(true);

            RefreshHelpLoop();

            Log("Reset to trapped position.");
        }

        public void SetWaitingFinalDialogue(Transform standPoint)
        {
            StopHelpLoop();

            if (standPoint != null)
            {
                transform.SetParent(standPoint);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.SetParent(_originalParent);
            }

            SetCollidersEnabled(true);

            Log("Reached escort goal. Waiting final dialogue.");
        }

        public void SetCompleted(Transform standPoint)
        {
            StopHelpLoop();

            if (standPoint != null)
            {
                transform.SetParent(standPoint);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.SetParent(_originalParent);
            }

            SetCollidersEnabled(true);

            Log("Rescue completed.");
        }

        private void BeginTalking()
        {
            _isTalking = true;
            RefreshHelpLoop();
        }

        private void EndTalking()
        {
            _isTalking = false;
            RefreshHelpLoop();
        }

        private bool ShouldPlayHelpLoop()
        {
            if (!_finalAreaActivated)
                return false;

            if (_isTalking)
                return false;

            if (!gameObject.activeInHierarchy)
                return false;

            if (quest == null)
                return true;

            if (quest.State == RescueQuestState.Escorting)
                return false;

            if (quest.State == RescueQuestState.WaitingFinalDialogue)
                return false;

            if (quest.State == RescueQuestState.Completed)
                return false;

            return true;
        }

        private void RefreshHelpLoop()
        {
            if (ShouldPlayHelpLoop())
                PlayHelpLoop();
            else
                StopHelpLoop();
        }

        private void PlayHelpLoop()
        {
            if (helpAudioSource == null || helpLoopSFX == null)
                return;

            if (helpAudioSource.clip != helpLoopSFX)
                helpAudioSource.clip = helpLoopSFX;

            helpAudioSource.volume = helpLoopVolume;

            if (!helpAudioSource.isPlaying)
                helpAudioSource.Play();
        }

        private void StopHelpLoop()
        {
            if (helpAudioSource == null)
                return;

            if (helpAudioSource.isPlaying)
                helpAudioSource.Stop();
        }

        private void SetupHelpAudio()
        {
            if (helpAudioSource == null)
                helpAudioSource = GetComponent<AudioSource>();

            if (helpAudioSource == null)
                helpAudioSource = gameObject.AddComponent<AudioSource>();

            helpAudioSource.playOnAwake = false;
            helpAudioSource.loop = true;
            helpAudioSource.volume = helpLoopVolume;
            helpAudioSource.clip = helpLoopSFX;

            if (helpVoiceAs3D)
            {
                helpAudioSource.spatialBlend = 1f;
                helpAudioSource.minDistance = helpMinDistance;
                helpAudioSource.maxDistance = helpMaxDistance;
                helpAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                helpAudioSource.spatialBlend = 0f;
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (_colliders == null)
                return;

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = enabled;
            }
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RescueNPC] {message}", this);
        }
    }
}