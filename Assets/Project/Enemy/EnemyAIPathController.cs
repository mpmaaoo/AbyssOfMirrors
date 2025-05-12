using UnityEngine;
using Pathfinding; // A* �׼��������g

[System.Serializable]
public struct PatrolNode
{
    public Transform point;
    public bool stopAndLook; // ���_���c�rͣ�K���ҏ���
}

public class EnemyAIPathController : MonoBehaviour
{
    [Header("Ѳ߉·��")]
    public PatrolNode[] patrolNodes;

    [Header("��� & ҕҰ")]
    public Transform player;
    public EnemyVision vision;

    [Header("�ٶ��O��")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("�О�r�g")]
    public float waitTime = 2f;   // ���_Ѳ߉�c��ͣ���r�g
    public float searchTime = 3f;   // �ɲ�����λ����ͣ���r�g
    public float loseSightDistance = 8f;  // ׷�G������|�l�ɲ���x

    [Header("�hҕ�O��")]
    [Tooltip("�hҕ�r���ƫ�D�Ƕȣ��ȣ�")]
    public float lookAngle = 45f;
    [Tooltip("���һ�����Ҕ[�^����r�g���룯�L�ڣ�")]
    public float lookPeriod = 2f;

    // A* ���P�M��
    private AIPath aiPath;
    private AIDestinationSetter destSetter;
    private Rigidbody2D rb;

    // ��B�C
    private enum State { Patrol, Waiting, Chase, Investigate, Search, Return }
    private State currentState;

    // �Ȳ�����
    private int patrolIndex;
    private Vector2 lastKnownPos;
    private float stateTimer;

    // �����Д�����c����
    private bool noiseReceived;
    private Vector2 noisePosition;
    private float noiseRadius;

    // �ɲ�����λ���õ��R�rĿ��
    private GameObject tempTarget;

    // �hҕ����
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
        // ������
        if (noiseReceived && currentState != State.Chase)
        {
            noiseReceived = false;
            lastKnownPos = noisePosition;
            EnterState(State.Investigate);
        }

        // ҕҰ����׷�� (���ò��������� CanSeePlayer)
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
                // ׷���г��m�z��ҕҰ
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

        // �hҕ���ƄӸ���ҕҰ
        if (currentState == State.Search || currentState == State.Waiting)
            DoLookAround();
        else
        {
            Vector2 moveDir = aiPath.desiredVelocity;
            if (moveDir.sqrMagnitude > 0.01f)
                UpdateDirection(moveDir.normalized);
        }
    }

    // ̎������
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
