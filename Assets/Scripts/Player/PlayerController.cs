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

    // ===================== SIZZLING SLIDE (Oil Dash) =====================
    [Header("Sizzling Slide (Oil Dash)")]
    [Tooltip("ความเร็วตอนสไลด์ค้าง")]
    [SerializeField] private float dashSpeed = 25f;

    [Tooltip("ความเร็วที่จะถูกนำไปใช้ตอนกระโดดสไลด์ (Slide Jump Momentum)")]
    [SerializeField] private float slideJumpSpeed = 15f;

    [Tooltip("ระยะเวลาความคงกระพันตอนเริ่มสไลด์")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("ดีเลย์ก่อนสไลด์ครั้งต่อไป (วินาที)")]
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
    private bool isSlideJumping;

    private float initialGravityScale;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        initialGravityScale = rb.gravityScale;

        // เปลี่ยนจาก TP Dash กลับมาเป็น Dash ปกติ
        if (dashSpeed >= 50f) dashSpeed = 25f;
        if (dashDuration <= 0.05f) dashDuration = 0.2f;

        // --- Setup TrailRenderer สำหรับ Sizzling Slide ---
        GameObject oilTrailObj = new GameObject("OilSmearTrail");
        oilTrailObj.transform.SetParent(transform);
        oilTrailObj.transform.localPosition = new Vector3(0, -0.6f, 0); // ตำแหน่งเท้า
        
        oilTrailRenderer = oilTrailObj.AddComponent<TrailRenderer>();
        oilTrailRenderer.time = 1.0f; // รอยน้ำมันค่อยๆ จางหายใน 1 วิ
        oilTrailRenderer.startWidth = 0.4f;
        oilTrailRenderer.endWidth = 0.1f;
        
        // สร้าง Material ด่วนๆ เพื่อให้มันแสดงผลเป็นสีได้แบบไม่ต้องวาดรูป
        oilTrailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        oilTrailRenderer.startColor = new Color(1f, 0.8f, 0.1f, 0.6f); // สีเหลืองทอง
        oilTrailRenderer.endColor = new Color(1f, 0.8f, 0.1f, 0f); // จางหายไป
        oilTrailRenderer.emitting = false;
        oilTrailRenderer.sortingOrder = -1; // ให้อยู่ล่างสุด

        // ป้องกันทะลุกำแพงตอนพุ่งไวๆ
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
        dashAction.AddBinding("<Keyboard>/s");
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
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
    }

    private void Update()
    {
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        // Read input
        MoveInput = moveAction.ReadValue<float>();

        // Checks
        CheckGround();
        CheckWall();

        // Timers
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        UpdateWallJumpLock();
        
        if (jumpAction.IsPressed() && IsGrounded && rb.linearVelocity.y <= 0f)
        {
            // บัพเฟอร์กระโดดไว้เลยถ้ากดค้างแล้วเท้าแตะพื้น (Bunny Hop)
            jumpBufferCounter = jumpBufferTime;
        }

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        // เช็คสถานะ Parry เพื่อไม่ให้สไลด์พร้อมฟันดาบ
        PlayerParry parrySystem = GetComponent<PlayerParry>();
        bool isParrying = parrySystem != null && parrySystem.IsParrying;

        // เช็คว่าผู้เล่นกดปุ่มสไลด์ค้างอยู่หรือไม่
        bool wantsToSlide = dashAction.IsPressed();

        // เริ่มสไลด์ได้ก็ต่อเมื่อไม่ได้ Parry อยู่
        if (wantsToSlide && IsGrounded && !IsShadowDashing && dashCooldownTimer <= 0 && !isParrying)
        {
            StartSlide();
        }
        // เลิกสไลด์ถ้าปล่อยปุ่ม, เท้าหลุดจากพื้น, หรือกด Parry แทรก
        else if (IsShadowDashing && (!wantsToSlide || !IsGrounded || isParrying))
        {
            EndSlide();
        }

        if (IsShadowDashing)
        {
            dashTimer -= Time.deltaTime;
            
            // หมดเวลาอมตะ 0.2 วิแรก
            if (dashTimer <= 0)
            {
                var health = GetComponent<PlayerHealth>();
                if (health != null) health.SetManualInvincible(false);
            }

            // เปิด Trail ให้ลากยาวเฉพาะตอนเท้าแตะพื้น (กันเหนียว)
            if (oilTrailRenderer != null)
            {
                oilTrailRenderer.emitting = IsGrounded;
            }
        }
        else
        {
            // ปิด Trail เมื่อไม่ได้ Dash
            if (oilTrailRenderer != null)
            {
                oilTrailRenderer.emitting = false;
            }
        }

        // Try buffered jump
        TryBufferedJump();

        // Flip sprite
        HandleFlip();
    }

    private void FixedUpdate()
    {
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) return;

        // ในแบบสไลด์ค้าง เรายังคงใช้ HandleMovement จัดการแรงเดินปกติ 
        // แค่เปลี่ยนความเร็วสูงสุด (Speed) ชั่วคราว
        HandleMovement();
        
        if (!IsShadowDashing)
        {
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

        // Reset extra jumps และโมเมนตัม เมื่อแตะพื้น
        if (IsGrounded && !wasGrounded)
        {
            extraJumpsRemaining = extraJumps;
            isSlideJumping = false; // ยกเลิกโมเมนตัมเมื่อเท้าแตะพื้น
        }

        // Coyote Time
        if (wasGrounded && !IsGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (IsGrounded)
        {
            coyoteTimeCounter = 0;
            IsWallSliding = false;
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

        // ถ้าสไลด์อยู่ หรือ โดดขึ้นมาจากสไลด์ (Slide Jump) ให้คงความเร็วสูงไว้!
        float currentMoveSpeed = moveSpeed;
        if (IsShadowDashing) currentMoveSpeed = dashSpeed;
        else if (isSlideJumping) currentMoveSpeed = slideJumpSpeed;

        float targetSpeed = MoveInput * currentMoveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;

        // ใช้ acceleration ตอนเร่ง, deceleration ตอนชะลอ
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        
        if (IsShadowDashing) 
            accelRate *= 0.5f; // ควบคุมยากขึ้นนิดหน่อยเวลาสไลด์ (อารมณ์ไถลบนน้ำมัน)
        else if (isSlideJumping) 
            accelRate *= 0.3f; // กลางอากาศบังคับตัวยากมากถ้าพุ่งมาแรงๆ (คงโมเมนตัม)

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
        if (wallCoyoteCounter > 0 && !IsGrounded && wallJumpLockTimer <= 0)
        {
            PerformWallJump();
            jumpBufferCounter = 0;
            wallCoyoteCounter = 0;
            coyoteTimeCounter = 0; // ล้าง Normal Coyote Time ด้วย ป้องกันบั๊กโดดซ้อนหลังลอยออกจากกำแพง
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
        // โดดจากสไลด์ (Slide Jump) จะเก็บโมเมนตัมความเร็วเอาไว้กลางอากาศ
        if (IsShadowDashing || (dashCooldownTimer > dashCooldown - 0.1f && dashCooldownTimer > 0)) 
        {
            isSlideJumping = true;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void PerformWallJump()
    {
        // กระโดดออกจากผนังไปฝั่งตรงข้าม
        float jumpDirX = -wallDirection * wallJumpForceX;
        rb.linearVelocity = new Vector2(jumpDirX, wallJumpForceY);

        // ล็อคทิศชั่วขณะ ไม่ให้ผู้เล่นดึงกลับทันที
        wallJumpLockTimer = wallJumpLockTime;

        // ล้างสิทธิ์การ Double Jump ทิ้งเพื่อไม่ให้กระโดดซ้อนกลางอากาศได้อีก
        extraJumpsRemaining = 0;

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
    // SIZZLING SLIDE (HOLD TO SLIDE)
    // =========================================================

    private void StartSlide()
    {
        IsShadowDashing = true;
        dashTimer = dashDuration; 

        // ระเบิดขอบจอ (ความเร็ว)
        ScreenEffectManager.Instance?.TriggerAdrenaline();
    }

    private void EndSlide()
    {
        IsShadowDashing = false;
        dashCooldownTimer = dashCooldown; // เริ่มนับ Cooldown เล็กน้อยหลังจากปล่อยปุ่ม
    }

    private TrailRenderer oilTrailRenderer;
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

        // ถ้ากำลังสไลด์ ให้หันหน้าไปตามทิศทางที่สไลด์ (ซ้าย/ขวา) แทนการหันตามเมาส์
        if (IsShadowDashing)
        {
            float slideDir = MoveInput != 0 ? MoveInput : rb.linearVelocity.x;
            if (slideDir > 0.1f && !IsFacingRight) Flip();
            else if (slideDir < -0.1f && IsFacingRight) Flip();
            return;
        }

        // หันหน้าตามเมาส์ (สภาวะปกติ)
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
