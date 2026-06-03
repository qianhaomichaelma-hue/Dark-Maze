using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class FinalAreaRescueHelpTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RescueNPC rescueNpc;

        [Header("Settings")]
        [SerializeField] private bool onlyOnce = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool _activated;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (rescueNpc == null)
                rescueNpc = FindFirstObjectByType<RescueNPC>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (onlyOnce && _activated)
                return;

            PlayerState player = other.GetComponentInParent<PlayerState>();
            if (player == null)
                return;

            _activated = true;

            if (rescueNpc != null)
                rescueNpc.ActivateFinalAreaHelpCall();

            if (debugLogs)
                Debug.Log("[FinalAreaRescueHelpTrigger] Final area help call activated.", this);
        }
    }
}