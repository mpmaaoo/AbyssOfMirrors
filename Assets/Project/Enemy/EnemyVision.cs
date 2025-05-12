using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("視野設定")]
    public Transform eyePoint;
    public float viewRadius = 5f;
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    // 目前朝向向量
    private Vector2 lookDirection = Vector2.right;

    /// <summary>
    /// 檢測是否看到玩家的 Collider2D
    /// </summary>
    public bool CanSeePlayer()
    {
        // 找出所有在半徑內、Layer 在 playerMask 的 Collider2D
        Collider2D[] hits = Physics2D.OverlapCircleAll(eyePoint.position, viewRadius, playerMask);
        foreach (var col in hits)
        {
            // 取這個 Collider2D 上最接近 eyePoint 的點
            Vector2 targetPoint = col.ClosestPoint(eyePoint.position);
            Vector2 dirToPoint = targetPoint - (Vector2)eyePoint.position;
            float dist = dirToPoint.magnitude;

            // 距離檢查
            if (dist > viewRadius)
                continue;

            // 視角檢查
            float angle = Vector2.Angle(lookDirection, dirToPoint);
            if (angle > viewAngle * 0.5f)
                continue;

            // 遮蔽檢查
            RaycastHit2D hit = Physics2D.Raycast(
                eyePoint.position,
                dirToPoint.normalized,
                dist,
                obstacleMask
            );
            if (hit.collider == null)
            {
                // 沒被遮蔽，視野內
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 更新敵人面向
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

    // 幫助畫出扇形邊線
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
