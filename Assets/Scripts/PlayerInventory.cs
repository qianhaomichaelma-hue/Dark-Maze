using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Torch Settings")]
    public int maxTorches = 4;      // 最大火把数量
    public int currentTorches = 4;  // 当前火把数量

    public bool CanThrowTorch()
    {
        return currentTorches > 0;
    }

    public void UseTorch()
    {
        if (currentTorches > 0)
        {
            currentTorches--;
            Debug.Log("Use torch, left: " + currentTorches);
        }
    }

    public void AddTorch()
    {
        if (currentTorches < maxTorches)
        {
            currentTorches++;
            Debug.Log("Pick torch, now: " + currentTorches);
        }
    }
}
