using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("ҕҰ�O��")]
    public Transform eyePoint;
    public float viewRadius = 5f;
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    // Ŀǰ��������
    private Vector2 lookDirection = Vector2.right;

    /// <summary>
    /// �z�y�Ƿ񿴵���ҵ� Collider2D
    /// </summary>
    public bool CanSeePlayer()
    {
        // �ҳ������ڰ돽�ȡ�Layer �� playerMask �� Collider2D
        Collider2D[] hits = Physics2D.OverlapCircleAll(eyePoint.position, viewRadius, playerMask);
        foreach (var col in hits)
        {
            // ȡ�@�� Collider2D ����ӽ� eyePoint ���c
            Vector2 targetPoint = col.ClosestPoint(eyePoint.position);
            Vector2 dirToPoint = targetPoint - (Vector2)eyePoint.position;
            float dist = dirToPoint.magnitude;

            // ���x�z��
            if (dist > viewRadius)
                continue;

            // ҕ�Ǚz��
            float angle = Vector2.Angle(lookDirection, dirToPoint);
            if (angle > viewAngle * 0.5f)
                continue;

            // �ڱΙz��
            RaycastHit2D hit = Physics2D.Raycast(
                eyePoint.position,
                dirToPoint.normalized,
                dist,
                obstacleMask
            );
            if (hit.collider == null)
            {
                // �]���ڱΣ�ҕҰ��
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ����������
    /// </summary>
    public void SetLookDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            lookDirection = direction.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(eyePoint.position, viewRadius);

        Vector3 dirA = DirFromAngle(-viewAngle / 2f, false);
        Vector3 dirB = DirFromAngle(viewAngle / 2f, false);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + dirA * viewRadius);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + dirB * viewRadius);
    }

    // ������������߅��
    private Vector3 DirFromAngle(float angleDeg, bool global)
    {
        if (!global)
        {
            float baseAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            angleDeg += baseAngle;
        }
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
