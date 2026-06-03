using UnityEngine;
using DarkMazeMinimal;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerInteractPromptDetector : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float detectDistance = 3.0f;
        [SerializeField] private LayerMask interactLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        private Camera _cam;
        private PlayerState _state;

        private InteractPromptTarget _currentPrompt;

        private void Awake()
        {
            _cam = Camera.main;
            _state = GetComponent<PlayerState>();
        }

        private void Update()
        {
            if (_state != null && _state.IsDead)
            {
                ClearCurrentPrompt();
                return;
            }

            if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen)
            {
                ClearCurrentPrompt();
                return;
            }

            DetectPrompt();
        }

        private void DetectPrompt()
        {
            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null)
            {
                ClearCurrentPrompt();
                return;
            }

            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

            if (drawDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.cyan, 0.05f);

            if (!Physics.Raycast(ray, out RaycastHit hit, detectDistance, interactLayers, QueryTriggerInteraction.Ignore))
            {
                ClearCurrentPrompt();
                return;
            }

            IInteractable interactable = GetInteractableFromHit(hit.collider);

            if (interactable == null)
            {
                ClearCurrentPrompt();
                return;
            }

            InteractPromptTarget prompt = GetPromptTargetFromHit(hit.collider, interactable);

            if (prompt == null || !prompt.enabled)
            {
                ClearCurrentPrompt();
                return;
            }

            SetCurrentPrompt(prompt);
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

        private InteractPromptTarget GetPromptTargetFromHit(Collider hitCollider, IInteractable interactable)
        {
            if (hitCollider == null)
                return null;

            InteractPromptTarget prompt = hitCollider.GetComponentInParent<InteractPromptTarget>();

            if (prompt != null)
                return prompt;

            prompt = hitCollider.GetComponentInChildren<InteractPromptTarget>(true);

            if (prompt != null)
                return prompt;

            if (interactable is MonoBehaviour mb)
            {
                prompt = mb.GetComponentInChildren<InteractPromptTarget>(true);

                if (prompt != null)
                    return prompt;

                prompt = mb.GetComponentInParent<InteractPromptTarget>();

                if (prompt != null)
                    return prompt;
            }

            return null;
        }

        private void SetCurrentPrompt(InteractPromptTarget newPrompt)
        {
            if (_currentPrompt == newPrompt)
                return;

            ClearCurrentPrompt();

            _currentPrompt = newPrompt;
            _currentPrompt.Show();
        }

        private void ClearCurrentPrompt()
        {
            if (_currentPrompt != null)
            {
                _currentPrompt.Hide();
                _currentPrompt = null;
            }
        }
    }
}