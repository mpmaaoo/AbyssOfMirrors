using System;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance { get; private set; }

    // ���������r�������顸�lλ�á��͡����돽��
    public event Action<Vector2, float> OnNoise;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    /// <summary>
    /// ȫ����У��V�������¼�
    /// </summary>
    public void BroadcastNoise(Vector2 position, float radius)
    {
        OnNoise?.Invoke(position, radius);
    }
}
