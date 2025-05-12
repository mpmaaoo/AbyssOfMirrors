using System;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance { get; private set; }

    // 當有噪音時，參數為「發聲位置」和「聲音半徑」
    public event Action<Vector2, float> OnNoise;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    /// <summary>
    /// 全域呼叫：廣播噪音事件
    /// </summary>
    public void BroadcastNoise(Vector2 position, float radius)
    {
        OnNoise?.Invoke(position, radius);
    }
}
