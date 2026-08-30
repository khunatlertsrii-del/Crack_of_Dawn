// ============================================================
// PlayerAnimator.cs — ควบคุม Animation States ของผู้เล่น
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ตัวกลางส่งค่าจาก PlayerController → Animator
/// 
/// Parameters ที่ส่ง:
/// - isRunning (bool)   → ผู้เล่นกำลังเดิน
/// - isJumping (bool)   → ผู้เล่นกำลังกระโดดขึ้น
/// - isFalling (bool)   → ผู้เล่นกำลังตก
/// - isWallSliding (bool) → ผู้เล่นเกาะผนัง
/// - isDashing (bool)   → ผู้เล่นกำลัง Dash
/// - yVelocity (float)  → ความเร็วแนวตั้ง
/// - xSpeed (float)     → ความเร็วแนวนอน (absolute)
/// 
/// ถ้ายังไม่มี Animator → จะใช้ Placeholder Color Feedback แทน
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Placeholder Colors (ใช้ตอนยังไม่มี Animation)")]
    [Tooltip("สีตอนอยู่นิ่ง")]
    [SerializeField] private Color idleColor = new Color(1f, 0.85f, 0.2f); // เหลืองไข่เจียว
    [Tooltip("สีตอนวิ่ง")]
    [SerializeField] private Color runColor = new Color(1f, 0.75f, 0.1f);
    [Tooltip("สีตอนกระโดดขึ้น")]
    [SerializeField] private Color jumpColor = new Color(1f, 0.9f, 0.4f);
    [Tooltip("สีตอนตก")]
    [SerializeField] private Color fallColor = new Color(0.9f, 0.7f, 0.1f);
    [Tooltip("สีตอน Wall Slide")]
    [SerializeField] private Color wallSlideColor = new Color(0.8f, 0.6f, 0.1f);
    [Tooltip("สีตอน Dash")]
    [SerializeField] private Color dashColor = new Color(1f, 1f, 0.6f);

    // ===================== Internal =====================
    private PlayerController playerController;
    private bool useAnimator;

    // Animator parameter hashes (ประหยัด Performance)
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsWalkingBackwards = Animator.StringToHash("isWalkingBackwards");
    private static readonly int IsJumping = Animator.StringToHash("isJumping");
    private static readonly int IsFalling = Animator.StringToHash("isFalling");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsWallSliding = Animator.StringToHash("isWallSliding");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");
    private static readonly int XSpeed = Animator.StringToHash("xSpeed");

    private HashSet<int> paramHashes = new HashSet<int>();
    private RuntimeAnimatorController cachedController;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // ถ้ามี Animator → ใช้ Animation จริง
        // ถ้าไม่มี → ใช้ Placeholder Color Feedback
        useAnimator = (animator != null && animator.runtimeAnimatorController != null);
    }

    private void LateUpdate()
    {
        if (playerController == null) return;

        if (useAnimator)
        {
            UpdateAnimator();
        }
        else
        {
            UpdatePlaceholderVisual();
        }
    }

    // =========================================================
    // ANIMATOR
    // =========================================================

    private void CheckAndCacheParameters()
    {
        if (animator != null && animator.runtimeAnimatorController != cachedController)
        {
            cachedController = animator.runtimeAnimatorController;
            paramHashes.Clear();
            if (cachedController != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    paramHashes.Add(param.nameHash);
                }
            }
        }
    }

    private void SetBoolSafe(int hash, bool val)
    {
        if (paramHashes.Contains(hash)) animator.SetBool(hash, val);
    }

    private void SetFloatSafe(int hash, float val)
    {
        if (paramHashes.Contains(hash)) animator.SetFloat(hash, val);
    }

    private void UpdateAnimator()
    {
        CheckAndCacheParameters();

        Vector2 vel = playerController.Velocity;
        bool isMoving = Mathf.Abs(playerController.MoveInput) > 0.1f && playerController.IsGrounded;
        bool isJumping = !playerController.IsGrounded && vel.y > 0.1f;
        bool isFalling = !playerController.IsGrounded && vel.y < -0.1f;

        // เช็คว่ากำลังเดินถอยหลังหรือไม่ (กดปุ่มตรงข้ามกับทิศที่หันหน้าอยู่)
        bool isWalkingBackwards = isMoving && (
            (playerController.IsFacingRight && playerController.MoveInput < -0.1f) ||
            (!playerController.IsFacingRight && playerController.MoveInput > 0.1f)
        );
        // ให้ isRunning เป็นจริงเฉพาะเวลาเดินไปข้างหน้าเท่านั้น ไม่ชนกับ isWalkingBackwards
        bool isRunning = isMoving && !isWalkingBackwards;

        SetBoolSafe(IsRunning, isRunning);
        SetBoolSafe(IsWalkingBackwards, isWalkingBackwards);
        SetBoolSafe(IsJumping, isJumping);
        SetBoolSafe(IsFalling, isFalling);
        SetBoolSafe(IsGrounded, playerController.IsGrounded);
        SetBoolSafe(IsWallSliding, playerController.IsWallSliding);
        SetBoolSafe(IsDashing, playerController.IsShadowDashing);
        SetFloatSafe(YVelocity, vel.y);
        SetFloatSafe(XSpeed, Mathf.Abs(vel.x));
    }

    // =========================================================
    // PLACEHOLDER VISUAL — ใช้สีแทน Animation ชั่วคราว
    // =========================================================

    private void UpdatePlaceholderVisual()
    {
        if (spriteRenderer == null) return;

        Color targetColor = idleColor;
        Vector2 vel = playerController.Velocity;

        if (playerController.IsShadowDashing)
        {
            targetColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // เงาดำโปร่งใส
        }
        else if (playerController.IsWallSliding)
        {
            targetColor = wallSlideColor;
        }
        else if (!playerController.IsGrounded && vel.y > 0.1f)
        {
            targetColor = jumpColor;
        }
        else if (!playerController.IsGrounded && vel.y < -0.1f)
        {
            targetColor = fallColor;
        }
        else if (Mathf.Abs(playerController.MoveInput) > 0.1f)
        {
            targetColor = runColor;
        }

        // Smooth color transition
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, 10f * Time.deltaTime);

        // Squash & Stretch แบบง่ายๆ
        ApplySquashStretch(vel);
    }

    /// <summary>
    /// Squash & Stretch — ยืด/หดตัวตาม Velocity
    /// เพิ่ม Juiciness ให้กับตัวละคร!
    /// </summary>
    private void ApplySquashStretch(Vector2 velocity)
    {
        float scaleX = Mathf.Abs(transform.localScale.x);
        float targetScaleY = 1f;
        float targetScaleXMag = 1f;

        if (!playerController.IsGrounded)
        {
            if (velocity.y > 2f)
            {
                // กำลังขึ้น → ยืดตัว
                targetScaleY = 1.15f;
                targetScaleXMag = 0.9f;
            }
            else if (velocity.y < -2f)
            {
                // กำลังตก → ยืดตัว
                targetScaleY = 1.1f;
                targetScaleXMag = 0.92f;
            }
        }
        else if (playerController.IsShadowDashing)
        {
            // แดชทะลุมิติ → แบนลงนิด ยืดขวา
            targetScaleY = 0.6f;
            targetScaleXMag = 1.4f;
        }

        // Smooth lerp
        float currentScaleY = Mathf.Lerp(
            Mathf.Abs(transform.localScale.y),
            targetScaleY,
            15f * Time.deltaTime
        );

        float currentScaleXMag = Mathf.Lerp(
            Mathf.Abs(transform.localScale.x),
            targetScaleXMag,
            15f * Time.deltaTime
        );

        float signX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(
            signX * currentScaleXMag,
            currentScaleY,
            1f
        );
    }
}
