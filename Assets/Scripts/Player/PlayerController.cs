// ============================================================
// PlayerController.cs — ระบบเคลื่อนที่ผู้เล่น (Acrobatic Platformer)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ระบบเคลื่อนที่แบบ Acrobatic Platformer (แรงบันดาลใจ: My Friend Pedro)
/// 
/// ฟีเจอร์:
/// - เดินซ้าย-ขวา (Acceleration / Deceleration)
/// - Variable Jump Height (กดสั้น=ต่ำ, กดค้าง=สูง)
/// - Coyote Time (กระโดดได้อีกนิดหลังเดินตกขอบ)
/// - Jump Buffer (กดก่อนแตะพื้นนิดเดียวจะกระโดดทันที)
/// - Wall Slide + Wall Jump
/// - Dash (พุ่งตัว)
/// - Flip (หันหน้าตามทิศเดิน)
/// 
/// ต้องมี Components: Rigidbody2D, Collider2D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ===================== MOVEMENT =====================
    [Header("Movement")]
    [Tooltip("ความเร็วเดิน")]
    [SerializeField] private float moveSpeed = 10f;

    [Tooltip("ความเร็วในการเร่ง (ยิ่งเยอะยิ่งตอบสนองไว)")]
    [SerializeField] private float acceleration = 15f;

    [Tooltip("ความเร็วในการชะลอ (เมื่อปล่อยปุ่ม)")]
    [SerializeField] private float deceleration = 20f;

    // ===================== JUMP =====================
    [Header("Jump")]
    [Tooltip("แรงกระโดด")]
    [SerializeField] private float jumpForce = 16f;

    [Tooltip("ตัวคูณ Gravity ตอนตก (ตกเร็วขึ้น = Game Feel ดี)")]
    [SerializeField] private float fallMultiplier = 3f;

    [Tooltip("ตัวคูณ Gravity ตอนปล่อยปุ่ม Jump (กระโดดเตี้ยลง)")]
    [SerializeField] private float lowJumpMultiplier = 5f;

    [Tooltip("ระยะเวลา Coyote Time (วินาที)")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("ระยะเวลา Jump Buffer (วินาที)")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Tooltip("จำนวนกระโดดกลางอากาศเพิ่มเติม (0 = ไม่มี Double Jump)")]
    [SerializeField] private int extraJumps = 0;

    // ===================== WALL SLIDE / JUMP =====================
    [Header("Wall Slide / Jump")]
    [Tooltip("ความเร็วไถลลงผนัง")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Tooltip("แรงกระโดดออกจากผนัง (แนวนอน)")]
    [SerializeField] private float wallJumpForceX = 12f;

    [Tooltip("แรงกระโดดออกจากผนัง (แนวตั้ง)")]
    [SerializeField] private float wallJumpForceY = 16f;

    [Tooltip("เวลาที่ล็อคทิศเคลื่อนที่หลัง Wall Jump (วินาที)")]
    [SerializeField] private float wallJumpLockTime = 0.15f;
    
    [Tooltip("เวลาหน่วงหลังหลุดจากกำแพงที่ยังกดโดดต่อได้ (วินาที)")]
    [SerializeField] private float wallCoyoteTime = 0.2f;

    // ===================== SHADOW DASH =====================
    [Header("Shadow Dash")]
    [Tooltip("ความเร็วตอนพุ่ง")]
    [SerializeField] private float dashSpeed = 20f;
    [Tooltip("ระยะเวลาที่พุ่ง (วินาที)")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("ดีเลย์ก่อนพุ่งครั้งต่อไป (วินาที)")]
    [SerializeField] private float dashCooldown = 1f;
    
    // ===================== GROUND / WALL CHECK =====================
    [Header("Ground & Wall Detection")]
    [Tooltip("จุดตรวจพื้น (ลาก Empty GameObject มาวางที่เท้า)")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("รัศมีตรวจพื้น")]
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Tooltip("Layer ของพื้น")]
    [SerializeField] private LayerMask groundLayer = -1;

    [Tooltip("จุดตรวจผนัง (ลาก Empty GameObject มาวางที่ด้านหน้าตัว)")]
    [SerializeField] private Transform wallCheck;

    [Tooltip("รัศมีตรวจผนัง")]
    [SerializeField] private float wallCheckRadius = 0.15f;

    [Tooltip("Layer ของผนัง (ปกติใช้ Layer เดียวกับพื้น)")]
    [SerializeField] private LayerMask wallLayer = -1;

    // ===================== STATE (Read-Only) =====================
    [Header("Debug Info (Read Only)")]
    [SerializeField] private bool debugShowState = true;

    public bool IsGrounded { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsShadowDashing { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public float MoveInput { get; private set; }
    public Vector2 Velocity => rb.linearVelocity;

    // ===================== INTERNAL =====================
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

    // Jump state
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int extraJumpsRemaining;
    private bool isJumpHeld;

    // Wall Jump state
    private bool isTouchingWall;
    private float wallJumpLockTimer;
    private int wallDirection; // 1 = ผนังอยู่ขวา, -1 = ผนังอยู่ซ้าย
    private float wallCoyoteCounter;

    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    private float initialGravityScale;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        initialGravityScale = rb.gravityScale;

        // ============ SETUP INPUT ============
        // เดินซ้าย-ขวา (WASD + Arrow Keys)
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/leftStick/left")
            .With("Positive", "<Gamepad>/leftStick/right");

        // กระโดด
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        // Shadow Dash
        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/rightShoulder");

        // Auto-create ground/wall check points if not assigned
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.6f, 0); // ต่ำกว่า Collider นิดนึง
            groundCheck = gc.transform;
        }
        if (wallCheck == null)
        {
            GameObject wc = new GameObject("WallCheck");
            wc.transform.SetParent(transform);
            wc.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheck = wc.transform;
        }

        // ใช้ทุก Layer เป็นค่าเริ่มต้น (-1) แล้วให้ filter logic ข้างล่างกรองเอง
        groundLayer = -1;
        wallLayer = -1;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();

        jumpAction.started += _ => OnJumpStarted();
        jumpAction.canceled += _ => OnJumpCanceled();
        dashAction.performed += _ => OnDashPerformed();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
    }

    private void Update()
    {
        // Skip ถ้าเกมไม่ได้ Playing
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        // Read input
        MoveInput = moveAction.ReadValue<float>();

        // Checks
        CheckGround();
        CheckWall();

        // Timers
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        UpdateWallJumpLock();
        
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        if (IsShadowDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        // Try buffered jump
        TryBufferedJump();

        // Flip sprite
        HandleFlip();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        if (IsShadowDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            HandleMovement();
            HandleWallSlide();
            ApplyBetterJumpGravity();
        }
    }

    // =========================================================
    // GROUND & WALL DETECTION (ใช้ Raycast — ง่ายและชัวร์!)
    // =========================================================

    private void CheckGround()
    {
        bool wasGrounded = IsGrounded;

        // === วิธีใหม่: ยิง Ray ลงล่างจากหลายจุด ===
        // ดีกว่า OverlapCircle เพราะไม่มีปัญหาตรวจจับ Collider ตัวเอง
        float rayLength = 0.4f;
        float halfWidth = 0.25f; // กว้างครึ่งหนึ่งของตัว Player

        // ยิง 3 เส้น: ซ้าย, กลาง, ขวา (เพื่อจับขอบแพลตฟอร์ม)
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.4f;
        bool hitCenter = CastGroundRay(origin, rayLength);
        bool hitLeft   = CastGroundRay(origin + Vector2.left * halfWidth, rayLength);
        bool hitRight  = CastGroundRay(origin + Vector2.right * halfWidth, rayLength);

        IsGrounded = hitCenter || hitLeft || hitRight;

        // Reset extra jumps เมื่อแตะพื้น
        if (IsGrounded && !wasGrounded)
        {
            extraJumpsRemaining = extraJumps;
        }
    }

    /// <summary>ยิง Ray ลงล่าง 1 เส้น — return true ถ้าชนพื้นที่ไม่ใช่ Trigger</summary>
    private bool CastGroundRay(Vector2 origin, float length)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, length);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider == playerCollider) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.isTrigger) continue;
            return true;
        }
        return false;
    }

    private void CheckWall()
    {
        // ยิงเลเซอร์ออกจากกลางตัวละคร (แม่นยำกว่า transform.position ที่อาจจะอยู่ตรงเท้า)
        Vector2 origin = playerCollider.bounds.center;
        
        // ระยะเลเซอร์ = ครึ่งนึงของความกว้างตัวละคร + 0.3f ให้เลยออกไปแตะกำแพง
        float rayLength = playerCollider.bounds.extents.x + 0.3f;
        
        bool hitRight = CastRay(origin, Vector2.right, rayLength);
        bool hitLeft = CastRay(origin, Vector2.left, rayLength);
        
        isTouchingWall = hitRight || hitLeft;
        
        if (isTouchingWall)
        {
            if (hitRight && hitLeft)
                wallDirection = MoveInput > 0 ? 1 : -1;
            else if (hitRight)
                wallDirection = 1;
            else if (hitLeft)
                wallDirection = -1;
        }
    }

    private bool CastRay(Vector2 origin, Vector2 dir, float length)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, length);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider == playerCollider) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.isTrigger) continue;
            return true;
        }
        return false;
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        if (wallJumpLockTimer > 0) return; // ล็อคการเคลื่อนที่ชั่วขณะหลัง Wall Jump

        float currentMoveSpeed = moveSpeed;

        float targetSpeed = MoveInput * currentMoveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;

        // ใช้ acceleration ตอนเร่ง, deceleration ตอนชะลอ
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDifference * accelRate * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x + movement,
            rb.linearVelocity.y
        );
    }

    // =========================================================
    // JUMP SYSTEM
    // =========================================================

    private void OnJumpStarted()
    {
        isJumpHeld = true;
        jumpBufferCounter = jumpBufferTime;

        TryBufferedJump();
    }

    private void OnJumpCanceled()
    {
        isJumpHeld = false;
    }

    private void TryBufferedJump()
    {
        if (jumpBufferCounter <= 0) return;

        // Wall Jump (ใช้ wallCoyoteCounter แทน IsWallSliding เพื่อให้กระโดดง่ายขึ้น)
        if (wallCoyoteCounter > 0 && !IsGrounded)
        {
            PerformWallJump();
            jumpBufferCounter = 0;
            wallCoyoteCounter = 0;
            return;
        }

        // Normal Jump (Coyote Time)
        if (coyoteTimeCounter > 0)
        {
            PerformJump();
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            return;
        }

        // Extra Jump (Double Jump)
        if (extraJumpsRemaining > 0)
        {
            PerformJump();
            jumpBufferCounter = 0;
            extraJumpsRemaining--;
            return;
        }
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void PerformWallJump()
    {
        // กระโดดออกจากผนังไปฝั่งตรงข้าม
        float jumpDirX = -wallDirection * wallJumpForceX;
        rb.linearVelocity = new Vector2(jumpDirX, wallJumpForceY);

        // ล็อคทิศชั่วขณะ ไม่ให้ผู้เล่นดึงกลับทันที
        wallJumpLockTimer = wallJumpLockTime;

        // Flip ไปฝั่งตรงข้าม
        if ((wallDirection > 0 && IsFacingRight) || (wallDirection < 0 && !IsFacingRight))
        {
            Flip();
        }

        IsWallSliding = false;
    }

    /// <summary>
    /// Better Jump Gravity:
    /// - ตกเร็วขึ้นเมื่อเริ่มตก (fallMultiplier)
    /// - ตกเร็วขึ้นถ้าปล่อยปุ่ม Jump (lowJumpMultiplier) → Variable Jump Height
    /// </summary>
    private void ApplyBetterJumpGravity()
    {
        if (IsWallSliding) return;

        if (rb.linearVelocity.y < 0)
        {
            // กำลังตก → เพิ่มแรงโน้มถ่วง
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !isJumpHeld)
        {
            // กำลังขึ้น + ปล่อยปุ่ม Jump → ตัดแรงกระโดด (กระโดดเตี้ย)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    // =========================================================
    // WALL SLIDE
    // =========================================================

    private void HandleWallSlide()
    {
        bool shouldWallSlide = isTouchingWall && !IsGrounded && rb.linearVelocity.y < 0
            && MoveInput != 0;

        IsWallSliding = shouldWallSlide;

        if (IsWallSliding)
        {
            // จำกัดความเร็วตกตอนเกาะผนัง
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed)
            );
        }
    }

    // =========================================================
    // SHADOW DASH
    // =========================================================

    private void OnDashPerformed()
    {
        if (dashCooldownTimer > 0) return;
        if (IsShadowDashing) return;

        // หาทิศทางพุ่ง ถ้ากดปุ่มเดินอยู่ก็พุ่งตามปุ่ม ถ้าไม่ได้กดให้พุ่งตามหน้า
        float dashDir = MoveInput != 0 ? Mathf.Sign(MoveInput) : (IsFacingRight ? 1 : -1);
        dashDirection = new Vector2(dashDir, 0);

        IsShadowDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0; // ปิดแรงโน้มถ่วงเพื่อไม่ให้ตก
        
        // I-Frame (อมตะชั่วขณะ)
        var health = GetComponent<PlayerHealth>();
        if (health != null) health.SetManualInvincible(true);

        // ระเบิดขอบจอ (ความเร็ว)
        ScreenEffectManager.Instance?.TriggerAdrenaline();
    }

    private void EndDash()
    {
        IsShadowDashing = false;
        rb.gravityScale = initialGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, 0); // เบรกเล็กน้อยตอนจบ

        // ปิด I-Frame
        var health = GetComponent<PlayerHealth>();
        if (health != null) health.SetManualInvincible(false);
    }

    // =========================================================
    // TIMERS
    // =========================================================

    private void UpdateCoyoteTime()
    {
        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Wall Coyote Time (ทำให้แตะกำแพงหรือหลุดกำแพงนิดๆ ก็โดดได้)
        if (isTouchingWall)
        {
            wallCoyoteCounter = wallCoyoteTime;
            // ไม่ต้องเซ็ต wallDirection ตรงนี้แล้ว เพราะ CheckWall จัดการให้แล้ว
        }
        else
        {
            wallCoyoteCounter -= Time.deltaTime;
        }
    } private void UpdateJumpBuffer()
    {
        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void UpdateWallJumpLock()
    {
        if (wallJumpLockTimer > 0)
            wallJumpLockTimer -= Time.deltaTime;
    }

    // =========================================================
    // FLIP
    // =========================================================

    private void HandleFlip()
    {
        if (wallJumpLockTimer > 0) return;

        // หันหน้าตามเมาส์
        if (Camera.main == null || UnityEngine.InputSystem.Mouse.current == null) return;

        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(Camera.main.transform.position.z)));

        if (mouseWorldPos.x > transform.position.x && !IsFacingRight)
            Flip();
        else if (mouseWorldPos.x < transform.position.x && IsFacingRight)
            Flip();
    }

    private void Flip()
    {
        IsFacingRight = !IsFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // =========================================================
    // GIZMOS — แสดง Ground/Wall Check ใน Editor
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = isTouchingWall ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}
