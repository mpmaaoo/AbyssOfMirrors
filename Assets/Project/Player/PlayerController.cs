using UnityEngine;


[RequireComponent(typeof(Rigidbody2D), typeof(NoiseEmitter))]
public class PlayerController : MonoBehaviour
{
    [Header("速度設定")]
    public float walkSpeed = 3f;
    public float sneakSpeed = 1.5f;
    public float runSpeed = 5f;

    [Header("鍵位設定")]
    public KeyCode sneakKey = KeyCode.LeftControl;
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("聲音範圍")]
    public float soundRadiusSneak = 0f;
    public float soundRadiusWalk = 1.5f;
    public float soundRadiusRun = 3f;
    public float noiseCooldown = 0.5f;

    private Rigidbody2D rb;
    private NoiseEmitter noiseEmitter;

    private Vector2 moveInput;
    private bool isSneaking;
    private bool isRunning;

    // 前一幀狀態，用來偵測轉變
    private bool prevMoving;
    private bool prevSneaking;
    private bool prevRunning;

    private float noiseTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        noiseEmitter = GetComponent<NoiseEmitter>();

        prevMoving = false;
        prevSneaking = false;
        prevRunning = false;
        noiseTimer = 0f;
    }

    void Update()
    {
        // 讀取輸入
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // 判斷潛行/跑步
        isSneaking = Input.GetKey(sneakKey);
        isRunning = Input.GetKey(runKey);
        if (isSneaking) isRunning = false; // 潛行優先

        bool isMoving = moveInput.magnitude > 0.1f;

        // 1. 從「移動 → 停止」時，立刻歸零噪音冷卻
        if (!isMoving && prevMoving)
        {
            noiseTimer = 0f;
        }

        // 2. 只有在有移動且非潛行時才發聲
        if (isMoving && !isSneaking)
        {
            // 剛開始移動、或在走→跑、跑→走間切換，都立即觸發
            if (!prevMoving ||
                (prevRunning && !isRunning) ||
                (!prevRunning && isRunning))
            {
                TriggerNoise();
            }
            else
            {
                // 持續走路依冷卻觸發
                noiseTimer -= Time.deltaTime;
                if (noiseTimer <= 0f)
                    TriggerNoise();
            }
        }

        // 更新上一幀狀態
        prevMoving = isMoving;
        prevSneaking = isSneaking;
        prevRunning = isRunning;
    }

    void FixedUpdate()
    {
        // 移動
        float speed = walkSpeed;
        if (isSneaking) speed = sneakSpeed;
        else if (isRunning) speed = runSpeed;
        rb.velocity = moveInput * speed;
    }

    /// <summary>
    /// 設定半徑並發出噪音，重置冷卻
    /// </summary>
    private void TriggerNoise()
    {
        float radius = isSneaking ? soundRadiusSneak
                     : isRunning ? soundRadiusRun
                                  : soundRadiusWalk;

        noiseEmitter.noiseRadius = radius;
        noiseEmitter.EmitNoise();
        noiseTimer = noiseCooldown;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        float radius = isSneaking ? soundRadiusSneak
                     : isRunning ? soundRadiusRun
                                  : soundRadiusWalk;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
