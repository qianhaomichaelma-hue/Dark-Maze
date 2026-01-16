using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StickyTorch : MonoBehaviour
{
    private Rigidbody _rb;
    private bool _stuck;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_stuck) return;

        StickToSurface(collision);
    }

    private void StickToSurface(Collision collision)
    {
        _stuck = true;

        // 第一个接触点
        ContactPoint contact = collision.contacts[0];

        // 把火把放到接触点附近，稍微往外偏一点避免穿模
        transform.position = contact.point + contact.normal * 0.02f;

        // 让火把大致朝向表面法线
        transform.rotation = Quaternion.LookRotation(-contact.normal, Vector3.up);

        // 停止物理模拟
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// 被玩家回收时调用：库存+1，然后销毁自己
    /// </summary>
    public void Pickup(PlayerInventory inventory)
    {
        if (inventory == null) return;

        inventory.AddTorch();
        Destroy(gameObject);
    }
}
