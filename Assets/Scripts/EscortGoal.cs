using UnityEngine;
using DarkMazePlayer;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class EscortGoal : MonoBehaviour
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Header("NPC Final Position")]
        [Tooltip("Optional. If assigned, rescued NPC will be placed here after quest completion.")]
        [SerializeField] private Transform rescuedNpcStandPoint;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private void Awake()
        {
            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (quest == null)
                return;

            if (!quest.IsEscorting)
                return;

            PlayerState player = other.GetComponentInParent<PlayerState>();
            if (player == null)
                return;

            quest.CompleteQuest(rescuedNpcStandPoint);

            if (debugLogs)
                Debug.Log("[EscortGoal] Quest completed.", this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            Collider col = GetComponent<Collider>();
            if (col == null)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            else
                Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }
    }
}