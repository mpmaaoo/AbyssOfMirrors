using UnityEngine;
using Pathfinding; // A* 套件命名空間

[System.Serializable]
public struct PatrolNode
{
    public Transform point;
    public bool stopAndLook; // 到達此點時停下並左右張望
}

public class EnemyAIPathController : MonoBehaviour
{
    [Header("巡邏路徑")]
    public PatrolNode[] patrolNodes;

    [Header("玩家 & 視野")]
    public Transform player;
    public EnemyVision vision;

    [Header("速度設定")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("行為時間")]
    public float waitTime = 2f;   // 到達巡邏點後停留時間
    public float searchTime = 3f;   // 偵查最後位置後停留時間
    public float loseSightDistance = 8f;  // 追丟玩家後觸發偵查距離

    [Header("環視設定")]
    [Tooltip("環視時最大偏轉角度（度）")]
    public float lookAngle = 45f;
    [Tooltip("完成一次左右擺頭所需時間（秒／週期）")]
    public float lookPeriod = 2f;

    // A* 相關組件
    private AIPath aiPath;
    private AIDestinationSetter destSetter;
    private Rigidbody2D rb;

    // 狀態機
    private enum State { Patrol, Waiting, Chase, Investigate, Search, Return }
    private State currentState;

    // 內部參數
    private int patrolIndex;
    private Vector2 lastKnownPos;
    private float stateTimer;

    // 噪音中斷旗標與參數
    private bool noiseReceived;
    private Vector2 noisePosition;
    private float noiseRadius;

    // 偵查最後位置用的臨時目標
    private GameObject tempTarget;

    // 環視朝向
    private Vector2 baseLookDir = Vector2.right;
    private Vector2 currentLookDir = Vector2.right;

    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        destSetter = GetComponent<AIDestinationSetter>();
        rb = GetComponent<Rigidbody2D>();
        // Kinematic
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        tempTarget = new GameObject("TempTarget");
        NoiseManager.Instance.OnNoise += HandleNoise;
    }

    void OnDestroy()
    {
        if (NoiseManager.Instance != null)
            NoiseManager.Instance.OnNoise -= HandleNoise;
    }

    void Start()
    {
        if (patrolNodes.Length == 0) return;
        EnterState(State.Patrol);
    }

    void Update()
    {
        // 噪音優先
        if (noiseReceived && currentState != State.Chase)
        {
            noiseReceived = false;
            lastKnownPos = noisePosition;
            EnterState(State.Investigate);
        }

        // 視野優先追擊 (改用不帶參數的 CanSeePlayer)
        if (currentState != State.Chase && vision.CanSeePlayer())
        {
            lastKnownPos = player.position;
            EnterState(State.Chase);
        }

        switch (currentState)
        {
            case State.Patrol:
                if (aiPath.reachedEndOfPath)
                {
                    if (patrolNodes[patrolIndex].stopAndLook)
                        EnterState(State.Waiting);
                    else
                    {
                        patrolIndex = (patrolIndex + 1) % patrolNodes.Length;
                        EnterState(State.Patrol);
                    }
                }
                break;

            case State.Waiting:
                if (AdvanceTimer(waitTime))
                {
                    patrolIndex = (patrolIndex + 1) % patrolNodes.Length;
                    EnterState(State.Patrol);
                }
                break;

            case State.Chase:
                // 追擊中持續檢查視野
                if (vision.CanSeePlayer())
                {
                    destSetter.target = player;
                    aiPath.maxSpeed = chaseSpeed;
                }
                else
                {
                    lastKnownPos = player != null ? (Vector2)player.position : lastKnownPos;
                    EnterState(State.Investigate);
                }
                break;

            case State.Investigate:
                if (aiPath.reachedEndOfPath)
                    EnterState(State.Search);
                break;

            case State.Search:
                if (AdvanceTimer(searchTime))
                    EnterState(State.Return);
                break;

            case State.Return:
                if (aiPath.reachedEndOfPath)
                    EnterState(State.Patrol);
                break;
        }

        // 環視或移動更新視野
        if (currentState == State.Search || currentState == State.Waiting)
            DoLookAround();
        else
        {
            Vector2 moveDir = aiPath.desiredVelocity;
            if (moveDir.sqrMagnitude > 0.01f)
                UpdateDirection(moveDir.normalized);
        }
    }

    // 處理噪音
    void HandleNoise(Vector2 sourcePos, float radius)
    {
        if (currentState == State.Chase) return;
        float dist = Vector2.Distance(transform.position, sourcePos);
        if (dist <= radius)
        {
            noiseReceived = true;
            noisePosition = sourcePos;
            noiseRadius = radius;
        }
    }

    void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;
        switch (newState)
        {
            case State.Patrol:
                destSetter.target = patrolNodes[patrolIndex].point;
                aiPath.maxSpeed = patrolSpeed;
                aiPath.canMove = true;
                break;

            case State.Waiting:
                aiPath.canMove = false;
                baseLookDir = currentLookDir;
                break;

            case State.Chase:
                destSetter.target = player;
                aiPath.maxSpeed = chaseSpeed;
                aiPath.canMove = true;
                break;

            case State.Investigate:
                tempTarget.transform.position = lastKnownPos;
                destSetter.target = tempTarget.transform;
                aiPath.maxSpeed = patrolSpeed;
                aiPath.canMove = true;
                aiPath.SearchPath();
                break;

            case State.Search:
                aiPath.canMove = false;
                baseLookDir = currentLookDir;
                break;

            case State.Return:
                destSetter.target = patrolNodes[patrolIndex].point;
                aiPath.maxSpeed = patrolSpeed;
                aiPath.canMove = true;
                break;
        }
    }

    bool AdvanceTimer(float dur)
    {
        stateTimer += Time.deltaTime;
        return stateTimer >= dur;
    }

    void DoLookAround()
    {
        float t = (stateTimer % lookPeriod) / lookPeriod * Mathf.PI * 2f;
        float sin = Mathf.Sin(t);
        float delta = sin * lookAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(
            baseLookDir.x * Mathf.Cos(delta) - baseLookDir.y * Mathf.Sin(delta),
            baseLookDir.x * Mathf.Sin(delta) + baseLookDir.y * Mathf.Cos(delta)
        );
        UpdateDirection(dir.normalized);
    }

    void UpdateDirection(Vector2 dir)
    {
        currentLookDir = dir;
        vision.SetLookDirection(dir);
        Vector3 ls = transform.localScale;
        ls.x = dir.x > 0 ? Mathf.Abs(ls.x) : -Mathf.Abs(ls.x);
        transform.localScale = ls;
    }
}
