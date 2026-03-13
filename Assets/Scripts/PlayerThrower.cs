using UnityEngine;
using StarterAssets;
using DarkMazeItems;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerThrower : MonoBehaviour
    {
        [Header("Throw Setup")]
        public Transform throwOrigin;
        public GameObject stonePrefab;
        public float throwForce = 10f;
        public float upwardForce = 1.5f;

        [Header("Inventory Cost")]
        public ItemData stoneItem;

        private Camera _cam;
        private StarterAssetsInputs _inputs;
        private PlayerInventory _inventory;

        private void Awake()
        {
            _cam = Camera.main;
            _inputs = GetComponent<StarterAssetsInputs>();
            _inventory = GetComponent<PlayerInventory>();

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

            if (_inputs.throwItem)
            {
                _inputs.throwItem = false; // 消费输入
                TryThrow();
            }
        }

        private void TryThrow() // stone
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            if (_inventory == null || stoneItem == null || stonePrefab == null)
                return;


            // 消耗 1 个当前物品
            if (!_inventory.TryGetCurrentItem(1, stoneItem))
            {
                Debug.Log("[PlayerThrower] No stone available.");
                return;
            }

            // stone
            Vector3 origin = throwOrigin.position;
            Vector3 dir = _cam.transform.forward;

            GameObject stone = Instantiate(stonePrefab, origin, Quaternion.identity);

            if (stone.TryGetComponent<Rigidbody>(out var rb))
            {
                Vector3 impulse = dir.normalized * throwForce + Vector3.up * upwardForce;
                rb.AddForce(impulse, ForceMode.Impulse);
            }
            Debug.Log("[PlayerThrower] Stone thrown.");
            // update inventory if we need
            if(_inventory.currentSlot.count == 0)
            {
                _inventory.TryRemoveCurrent();
                _inventory.UpdateCurrentSlot();
            }
        }
    }
}

