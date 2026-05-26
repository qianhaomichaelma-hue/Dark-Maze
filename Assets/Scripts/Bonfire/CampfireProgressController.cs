using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DarkMazeUI;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class CampfireProgressController : MonoBehaviour
    {
        [Serializable]
        public class CampfireStep
        {
            [Header("Target Campfire")]
            public Bonfire bonfire;

            [Header("Objective After Ignite")]
            [TextArea(1, 3)]
            public string objectiveAfterIgnite = "Find the next campfire";

            [Header("Messages")]
            public bool showCampfireLitMessage = true;
            public string campfireLitMessage = "篝火已点燃";

            public bool showCheckpointMessage = true;
            public string checkpointMessage = "重生点更新";

            [Header("Events")]
            public UnityEvent onStepReached;
        }

        [Header("Campfire Steps")]
        [SerializeField] private CampfireStep[] steps;

        [Header("Rules")]
        [Tooltip("If true, only campfires listed in Steps will count.")]
        [SerializeField] private bool ignoreUnregisteredBonfires = true;

        [Tooltip("If true, objective text will include progress count after the objective.")]
        [SerializeField] private bool appendProgressToObjective = false;

        [SerializeField] private string progressFormat = "Campfires lit: {0}/{1}";

        [Header("Global Events")]
        public UnityEvent onAnyCampfireIgnited;
        public UnityEvent onAllCampfiresIgnited;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private readonly HashSet<Bonfire> _processedBonfires = new HashSet<Bonfire>();
        private bool _allCompleteTriggered = false;

        public int LitCount => _processedBonfires.Count;
        public int TotalRequired => GetRegisteredStepCount();

        public void NotifyBonfireIgnited(Bonfire bonfire)
        {
            if (bonfire == null)
                return;

            if (_processedBonfires.Contains(bonfire))
            {
                Log($"Ignored duplicate bonfire: {bonfire.name}");
                return;
            }

            int stepIndex = FindStepIndex(bonfire);

            if (stepIndex < 0 && ignoreUnregisteredBonfires)
            {
                LogWarning($"Bonfire {bonfire.name} is not registered in CampfireProgressController. Ignored.");
                return;
            }

            _processedBonfires.Add(bonfire);

            CampfireStep step = GetStepForBonfireOrProgressIndex(stepIndex);

            if (step != null)
            {
                ShowMessages(step);
                UpdateObjective(step);
                step.onStepReached?.Invoke();
            }

            onAnyCampfireIgnited?.Invoke();

            Log($"Bonfire progress updated: {LitCount}/{TotalRequired} | bonfire={bonfire.name}");

            if (!_allCompleteTriggered && TotalRequired > 0 && LitCount >= TotalRequired)
            {
                _allCompleteTriggered = true;
                onAllCampfiresIgnited?.Invoke();

                Log("All required campfires ignited.");
            }
        }

        private void ShowMessages(CampfireStep step)
        {
            if (GameMessageUI.Instance == null)
                return;

            if (step.showCampfireLitMessage)
                GameMessageUI.Instance.ShowMessage(step.campfireLitMessage);

            if (step.showCheckpointMessage)
                GameMessageUI.Instance.ShowMessage(step.checkpointMessage);
        }

        private void UpdateObjective(CampfireStep step)
        {
            if (ObjectiveUI.Instance == null)
                return;

            string objective = step.objectiveAfterIgnite;

            if (appendProgressToObjective)
            {
                string progress = string.Format(progressFormat, LitCount, TotalRequired);

                if (!string.IsNullOrWhiteSpace(objective))
                    objective += "\n" + progress;
                else
                    objective = progress;
            }

            ObjectiveUI.Instance.SetObjective(objective);
        }

        private CampfireStep GetStepForBonfireOrProgressIndex(int stepIndex)
        {
            if (steps == null || steps.Length == 0)
                return null;

            if (stepIndex >= 0 && stepIndex < steps.Length)
                return steps[stepIndex];

            int progressIndex = Mathf.Clamp(LitCount - 1, 0, steps.Length - 1);
            return steps[progressIndex];
        }

        private int FindStepIndex(Bonfire bonfire)
        {
            if (steps == null)
                return -1;

            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && steps[i].bonfire == bonfire)
                    return i;
            }

            return -1;
        }

        private int GetRegisteredStepCount()
        {
            if (steps == null)
                return 0;

            int count = 0;

            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && steps[i].bonfire != null)
                    count++;
            }

            return count;
        }

        [ContextMenu("Reset Progress For Testing")]
        public void ResetProgressForTesting()
        {
            _processedBonfires.Clear();
            _allCompleteTriggered = false;

            Log("Progress reset for testing.");
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[CampfireProgressController] {message}", this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[CampfireProgressController] {message}", this);
        }
    }
}