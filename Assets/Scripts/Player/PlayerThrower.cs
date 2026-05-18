using DarkMazeItems;
using DarkMazeMinimal;
using StarterAssets;
using UnityEngine;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerThrower : MonoBehaviour
    {
        [Header("Throw Setup")]
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private float throwForce = 10f;
        [SerializeField] private float upwardForce = 1.5f;

        [Header("Inventory Cost")]
        [SerializeField] private ItemData stoneItem;

        [Header("Audio - Throw")]
        [SerializeField] private AudioSource throwAudioSource;
        [SerializeField] private AudioClip throwSFX;
        [SerializeField] private float throwVolume = 0.8f;

        private Camera _cam;
        private StarterAssetsInputs _inputs;
        private PlayerInventory _inventory;
        private PlayerState _state;

        private void Awake()
        {
            _cam = Camera.main;
            _inputs = GetComponent<StarterAssetsInputs>();
            _inventory = GetComponent<PlayerInventory>();
            _state = GetComponent<PlayerState>();

            if (throwAudioSource == null)
                throwAudioSource = GetComponent<AudioSource>();

            if (throwAudioSource != null)
            {
                throwAudioSource.playOnAwake = false;
                throwAudioSource.loop = false;
            }

            if (throwOrigin == null)
            {
                GameObject auto = new GameObject("ThrowOrigin_Auto");
                auto.transform.SetParent(transform);
                auto.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
                throwOrigin = auto.transform;
            }
        }

        private void Update()
        {
            if (_inputs == null) return;
            if (_state != null && _state.IsDead) return;

            if (_inputs.throwItem)
            {
                _inputs.throwItem = false;
                TryThrow();
            }
        }

        private void TryThrow()
        {
            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null)
            {
                Debug.LogWarning("[PlayerThrower] No main camera found.", this);
                return;
            }

            if (_inventory == null)
            {
                Debug.LogWarning("[PlayerThrower] No PlayerInventory found.", this);
                return;
            }

            if (stoneItem == null)
            {
                Debug.LogWarning("[PlayerThrower] stoneItem is NULL.", this);
                return;
            }

            if (stonePrefab == null)
            {
                Debug.LogWarning("[PlayerThrower] stonePrefab is NULL.", this);
                return;
            }

            // 只允许消耗当前手持 / 当前选中 slot 的 stone。
            // TryGetCurrentItem 内部已经负责扣数量、移除空 slot、同步 PlayerEquipment。
            if (!_inventory.TryGetCurrentItem(1, stoneItem))
            {
                Debug.Log("[PlayerThrower] No stone available in current slot.");
                return;
            }

            Vector3 origin = throwOrigin != null
                ? throwOrigin.position
                : transform.position + Vector3.up * 1.2f + transform.forward * 0.4f;

            Vector3 dir = _cam.transform.forward.normalized;

            GameObject stone = Instantiate(stonePrefab, origin, Quaternion.identity);

            if (stone.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Vector3 impulse = dir * throwForce + Vector3.up * upwardForce;
                rb.AddForce(impulse, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("[PlayerThrower] Spawned stone has no Rigidbody.", stone);
            }

            PlayThrowSFX();

            Debug.Log("[PlayerThrower] Stone thrown.");
        }

        private void PlayThrowSFX()
        {
            if (throwAudioSource == null || throwSFX == null)
                return;

            throwAudioSource.PlayOneShot(throwSFX, throwVolume);
        }
    }
}