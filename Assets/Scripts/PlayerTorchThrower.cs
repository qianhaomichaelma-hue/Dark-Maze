using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerTorchThrower : MonoBehaviour
{
    public GameObject torchPrefab;      // 拖你的 Torch Prefab 进来
    public Transform throwOrigin;       // 丢出位置（一般是相机或者角色胸口）
    public float throwForce = 10f;      // 丢出的力

    private PlayerInventory _inventory;
    private StarterAssets.StarterAssetsInputs _inputs;
    private Camera _mainCamera;

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_inputs == null) return;

        if (_inputs.throwTorch)
        {
            _inputs.throwTorch = false; // 消费输入

            TryThrowTorch();
        }
    }

    private void TryThrowTorch()
    {
        if (!_inventory.CanThrowTorch())
        {
            Debug.Log("No torch left.");
            return;
        }

        if (torchPrefab == null)
        {
            Debug.LogError("Torch prefab not assigned.");
            return;
        }

        // 丢出方向：用相机正前方
        Vector3 dir = _mainCamera != null ? _mainCamera.transform.forward : transform.forward;

        // 生成火把
        GameObject torchObj = Instantiate(torchPrefab, throwOrigin.position, Quaternion.identity);

        // 朝向和相机一致（可选）
        torchObj.transform.forward = dir;

        // 加力
        Rigidbody rb = torchObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * throwForce, ForceMode.VelocityChange);
        }

        _inventory.UseTorch();
    }
}
