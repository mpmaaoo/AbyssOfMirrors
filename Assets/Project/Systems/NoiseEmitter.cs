using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NoiseEmitter : MonoBehaviour, INoiseSource
{
    [Header("噪音半徑")]
    public float noiseRadius = 3f;
    public LayerMask enemyMask; // 也可以不用，全部由 NoiseManager 廣播

    /// <summary>
    /// 實作 INoiseSource.EmitNoise，廣播給 NoiseManager
    /// </summary>
    [ContextMenu("發出噪音")]
    public void EmitNoise()
    {
        NoiseManager.Instance.BroadcastNoise(transform.position, noiseRadius);

        // 以下選擇性：若你想立刻對附近敵人直接呼叫，也可：
        // Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, noiseRadius, enemyMask);
        // foreach (var col in hits)
        // {
        //     var ai = col.GetComponentInParent<EnemyAIPathController>();
        //     if (ai != null) ai.HearNoise(transform.position);
        // }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
