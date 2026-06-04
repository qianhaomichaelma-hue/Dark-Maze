using UnityEngine;
using StarterAssets;
using DarkMazeMinimal;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask interactLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private bool drawDebugRay = true;

        private Camera _cam;
        private PlayerEquipment _equip;
        private PlayerState _state;
        private StarterAssetsInputs _inputs;

        public PlayerState PlayerState => _state;
        public PlayerEquipment Equipment => _equip;

        private void Awake()
        {
            _cam = Camera.main;
            _equip = GetComponent<PlayerEquipment>();
            _state = GetComponent<PlayerState>();
            _inputs = GetComponent<StarterAssetsInputs>();
        }

        private void Update()
        {
            if (_inputs == null)
                return;

            if (_inputs.interact)
            {
                _inputs.interact = false;

                if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen)
                {
                    DialogueUI.Instance.Advance();
                    return;
                }

                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (_state != null && _state.IsDead)
                return;

            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null)
            {
                LogWarning("No MainCamera found. Check camera Tag = MainCamera.");
                return;
            }

            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

            if (drawDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 0.35f);

            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, QueryTriggerInteraction.Ignore))
            {
                Log("No interactable hit.");
                return;
            }

            IInteractable interactable = GetInteractableFromHit(hit.collider);

            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }

            Log("Hit object has no IInteractable.");
        }

        private IInteractable GetInteractableFromHit(Collider hitCollider)
        {
            if (hitCollider == null)
                return null;

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractable interactable)
                    return interactable;
            }

            return null;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[PlayerInteractor] {message}", this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PlayerInteractor] {message}", this);
        }
    }
}