using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NoiseEmitter : MonoBehaviour, INoiseSource
{
    [Header("�����돽")]
    public float noiseRadius = 3f;
    public LayerMask enemyMask; // Ҳ���Բ��ã�ȫ���� NoiseManager �V��

    /// <summary>
    /// ���� INoiseSource.EmitNoise���V���o NoiseManager
    /// </summary>
    [ContextMenu("�l������")]
    public void EmitNoise()
    {
        NoiseManager.Instance.BroadcastNoise(transform.position, noiseRadius);

        // �����x���ԣ����������̌���������ֱ�Ӻ��У�Ҳ�ɣ�
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
