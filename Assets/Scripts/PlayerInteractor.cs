using UnityEngine;
using StarterAssets;
using DarkMazeItems;
using DarkMazeMinimal;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        public float interactDistance = 3.0f;
        public LayerMask interactLayers = ~0;

        [Header("Requirement")]
        public ItemData torchItem;

        private Camera _cam;
        private PlayerEquipment _equip;
        private PlayerState _state;
        private StarterAssetsInputs _inputs;

        private void Awake()
        {
            _cam = Camera.main;
            _equip = GetComponent<PlayerEquipment>();
            _state = GetComponent<PlayerState>();
            _inputs = GetComponent<StarterAssetsInputs>();

            Debug.Log($"[PlayerInteractor] Awake | cam={(_cam ? _cam.name : "NULL")} | hasEquip={_equip != null} | hasInputs={_inputs != null}", this);
        }

        private void Update()
        {
            if (_inputs == null) return;

            if (_inputs.interact)
            {
                Debug.Log($"[PlayerInteractor] Interact pressed | frame={Time.frameCount}", this);

                _inputs.interact = false; // consume input
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (_state != null && _state.IsDead)
            {
                Debug.Log("[PlayerInteractor] Player is dead -> ignore interact.", this);
                return;
            }

            if (_cam == null) _cam = Camera.main;
            if (_cam == null)
            {
                Debug.LogWarning("[PlayerInteractor] No MainCamera found. Check camera Tag=MainCamera.", this);
                return;
            }

            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 0.5f);

            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, QueryTriggerInteraction.Ignore);

            if (!hitSomething)
            {
                Debug.Log($"[PlayerInteractor] Raycast NO HIT | dist={interactDistance}", this);
                return;
            }

            Debug.Log($"[PlayerInteractor] Raycast HIT | name={hit.collider.name} | layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} | dist={hit.distance:F2}", hit.collider);

            Bonfire bonfire = hit.collider.GetComponentInParent<Bonfire>();
            if (bonfire == null)
            {
                Debug.Log("[PlayerInteractor] Hit object is NOT a Bonfire (no Bonfire in parents).", hit.collider);
                return;
            }

            Debug.Log($"[PlayerInteractor] Found Bonfire -> {bonfire.name} | isLit={bonfire.IsLit}", bonfire);

            if (bonfire.IsLit)
            {
                Debug.Log("[PlayerInteractor] Bonfire already lit -> no action.", bonfire);
                return;
            }

            if (_equip == null)
            {
                Debug.LogWarning("[PlayerInteractor] Missing PlayerEquipment on player.", this);
                return;
            }

            if (torchItem == null)
            {
                Debug.LogWarning("[PlayerInteractor] torchItem not assigned in Inspector.", this);
                return;
            }

            Debug.Log($"[PlayerInteractor] Holding check | holdingTorch={_equip.IsHolding(torchItem)} | heldItem={(_equip.HeldItem ? _equip.HeldItem.displayName : "None")}", this);

            if (!_equip.IsHolding(torchItem))
            {
                Debug.Log("[PlayerInteractor] Need to HOLD torch to ignite.", this);
                return;
            }

            bool ignited = bonfire.TryIgnite();
            Debug.Log($"[PlayerInteractor] TryIgnite result = {ignited}", bonfire);
        }
    }
}

