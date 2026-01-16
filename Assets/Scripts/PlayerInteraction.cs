using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 2f;   // 最大交互半径

    private PlayerInventory _inventory;
    private StarterAssetsInputs _inputs;

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _inputs = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        if (_inputs == null) return;

        if (_inputs.interact)
        {
            _inputs.interact = false; // 消费输入
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // 在玩家周围做一个球形检测
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius);

        StickyTorch closestTorch = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (col == null) continue;

            StickyTorch torch = col.GetComponentInParent<StickyTorch>();
            if (torch == null) continue;

            float dist = Vector3.Distance(transform.position, torch.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTorch = torch;
            }
        }

        if (closestTorch != null)
        {
            closestTorch.Pickup(_inventory);
        }
    }
}
