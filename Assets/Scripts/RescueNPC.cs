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

        [Header("Dialogue")]
        [TextArea(2, 5)]
        [SerializeField] private string notAssignedLine = "I do not know you. Please speak to the one outside first.";

        [TextArea(2, 5)]
        [SerializeField] private string[] rescueDialogueLines;

        [TextArea(2, 5)]
        [SerializeField] private string alreadyFollowingLine = "I am right behind you.";

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

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();

            _originalParent = transform.parent;
            _trappedPosition = transform.position;
            _trappedRotation = transform.rotation;

            _colliders = GetComponentsInChildren<Collider>(true);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (quest == null)
                return;

            if (DialogueUI.Instance == null)
            {
                TryStartEscort();
                return;
            }

            switch (quest.State)
            {
                case RescueQuestState.NotStarted:
                    DialogueUI.Instance.ShowSingleLine(speakerName, notAssignedLine);
                    break;

                case RescueQuestState.TaskAssigned:
                    DialogueUI.Instance.ShowLines(speakerName, rescueDialogueLines, TryStartEscort);
                    break;

                case RescueQuestState.Escorting:
                    DialogueUI.Instance.ShowSingleLine(speakerName, alreadyFollowingLine);
                    break;

                case RescueQuestState.Completed:
                    DialogueUI.Instance.ShowSingleLine(speakerName, afterCompleteLine);
                    break;
            }
        }

        private void TryStartEscort()
        {
            if (quest == null)
                return;

            quest.StartEscort(this);
            Log("Rescue dialogue finished. Escort started.");
        }

        public void AttachToCarryPoint(Transform carryPoint)
        {
            if (carryPoint == null)
                return;

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

            Log("Reset to trapped position.");
        }

        public void SetCompleted(Transform standPoint)
        {
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