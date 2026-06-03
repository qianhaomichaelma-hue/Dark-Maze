using UnityEngine;
using UnityEngine.Events;
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    public enum RescueQuestState
    {
        NotStarted,
        TaskAssigned,
        Escorting,
        WaitingFinalDialogue,
        Completed
    }

    [DisallowMultipleComponent]
    public class RescueQuestController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerState player;
        [SerializeField] private Transform playerCarryPoint;
        [SerializeField] private RescueNPC rescueNpc;

        [Header("Death Rule")]
        [Tooltip("If true, dying while carrying the NPC sends the NPC back to the trapped position, but the quest remains assigned.")]
        [SerializeField] private bool resetEscortOnPlayerDeath = true;

        [Header("Events")]
        public UnityEvent onQuestStarted;
        public UnityEvent onEscortStarted;
        public UnityEvent onEscortFailedByDeath;

        [Tooltip("Called when the player reaches the escort goal, but before final dialogue.")]
        public UnityEvent onEscortArrivedAtGoal;

        [Tooltip("Called only after the player talks to the rescued NPC at the goal.")]
        public UnityEvent onQuestCompleted;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        [SerializeField] private RescueQuestState state = RescueQuestState.NotStarted;

        private bool _handledCurrentDeath = false;
        private Transform _lastRescuedNpcStandPoint;

        public RescueQuestState State => state;

        public bool IsQuestAssigned => state == RescueQuestState.TaskAssigned;
        public bool IsEscorting => state == RescueQuestState.Escorting;
        public bool IsWaitingFinalDialogue => state == RescueQuestState.WaitingFinalDialogue;
        public bool IsCompleted => state == RescueQuestState.Completed;

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerState>();

            if (rescueNpc == null)
                rescueNpc = FindFirstObjectByType<RescueNPC>();

            if (playerCarryPoint == null && player != null)
                playerCarryPoint = CreateAutoCarryPoint(player.transform);
        }

        private void Update()
        {
            if (player == null)
                return;

            if (!player.IsDead)
            {
                _handledCurrentDeath = false;
                return;
            }

            if (_handledCurrentDeath)
                return;

            _handledCurrentDeath = true;

            if (state == RescueQuestState.Escorting && resetEscortOnPlayerDeath)
                FailEscortByDeath();
        }

        public void StartQuest()
        {
            if (state != RescueQuestState.NotStarted)
                return;

            state = RescueQuestState.TaskAssigned;

            Log("Quest started.");
            onQuestStarted?.Invoke();
        }

        public void StartEscort(RescueNPC npc)
        {
            if (state != RescueQuestState.TaskAssigned)
                return;

            if (npc == null)
                return;

            rescueNpc = npc;

            if (playerCarryPoint == null && player != null)
                playerCarryPoint = CreateAutoCarryPoint(player.transform);

            if (playerCarryPoint == null)
                return;

            state = RescueQuestState.Escorting;

            rescueNpc.AttachToCarryPoint(playerCarryPoint);

            Log("Escort started.");
            onEscortStarted?.Invoke();
        }

        public void ArriveAtEscortGoal(Transform rescuedNpcStandPoint = null)
        {
            if (state != RescueQuestState.Escorting)
                return;

            state = RescueQuestState.WaitingFinalDialogue;
            _lastRescuedNpcStandPoint = rescuedNpcStandPoint;

            if (rescueNpc != null)
                rescueNpc.SetWaitingFinalDialogue(rescuedNpcStandPoint);

            Log("Escort arrived at goal. Waiting for final NPC dialogue.");
            onEscortArrivedAtGoal?.Invoke();
        }

        public void CompleteQuestAfterFinalDialogue()
        {
            if (state != RescueQuestState.WaitingFinalDialogue)
                return;

            state = RescueQuestState.Completed;

            if (rescueNpc != null)
                rescueNpc.SetCompleted(_lastRescuedNpcStandPoint);

            Log("Quest completed after final dialogue.");
            onQuestCompleted?.Invoke();
        }

        public void FailEscortByDeath()
        {
            if (state != RescueQuestState.Escorting)
                return;

            state = RescueQuestState.TaskAssigned;

            if (rescueNpc != null)
                rescueNpc.ResetToTrappedPosition();

            Log("Escort failed by death. NPC reset to trapped position.");
            onEscortFailedByDeath?.Invoke();
        }

        private Transform CreateAutoCarryPoint(Transform playerTransform)
        {
            GameObject go = new GameObject("NpcCarryPoint_Auto");
            go.transform.SetParent(playerTransform);
            go.transform.localPosition = new Vector3(0f, 1.15f, -0.55f);
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RescueQuestController] {message}", this);
        }
    }
}