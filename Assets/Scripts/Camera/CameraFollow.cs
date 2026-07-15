// ============================================================
// CameraFollow.cs — กล้องติดตามผู้เล่น + Screen Shake
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// กล้อง 2D ที่:
/// 1. ติดตามผู้เล่นแบบ Smooth
/// 2. Look Ahead — ขยับนำหน้าทิศทางที่ผู้เล่นเดิน
/// 3. Screen Shake — สั่นเมื่อเรียก (ยิงโดน Critical, ระเบิด, etc.)
/// 
/// ใช้เป็น Singleton → CameraFollow.Instance.ShakeCamera(...)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [Tooltip("ลาก Player มาวางที่นี่ (ถ้าไม่ใส่จะหาเองจาก Tag 'Player')")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("ความเร็วในการติดตาม (ยิ่งเยอะยิ่งตามเร็ว)")]
    [SerializeField] private float followSpeed = 8f;

    [Tooltip("ระยะ Offset จากผู้เล่น (เช่น ยกกล้องขึ้นนิดหน่อย)")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

    [Header("Look Ahead")]
    [Tooltip("ระยะที่กล้องจะขยับนำหน้าผู้เล่น")]
    [SerializeField] private float lookAheadDistance = 2f;

    [Tooltip("ความเร็วในการขยับ Look Ahead")]
    [SerializeField] private float lookAheadSpeed = 5f;

    [Header("Bounds (Optional)")]
    [Tooltip("จำกัดขอบเขตกล้อง — ปิดได้โดยตั้ง useBounds = false")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -10);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 10);

    [Header("Zoom Settings")]
    [Tooltip("ขนาดกล้องเริ่มต้น (Default Orthographic Size)")]
    [SerializeField] private float defaultOrthoSize = 5f;
    [Tooltip("ความเร็วในการซูมเข้า (ตอนเกิดเอฟเฟกต์)")]
    [SerializeField] private float zoomInSpeed = 20f;
    [Tooltip("ความเร็วในการซูมออก (คืนค่ากลับเป็นปกติ)")]
    [SerializeField] private float zoomOutSpeed = 5f;

    // ===================== Internal =====================
    private float currentLookAhead = 0f;
    private float targetLookAhead = 0f;

    // Camera
    private Camera cam;

    // Screen Shake
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;
    private float shakeTimer = 0f;

    // Zoom
    private float zoomTimer = 0f;
    private float currentZoomAmount = 0f;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographicSize = defaultOrthoSize;
        }
    }

    private void Start()
    {
        // ถ้าไม่ได้ลาก target มาใส่ → หาจาก Tag
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Snap กล้องไปที่ Player ทันทีตอนเริ่ม (ไม่ต้อง Smooth)
        if (target != null)
        {
            Vector3 startPos = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );
            transform.position = startPos;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
        HandleScreenShake();
        HandleZoom();
    }

    // =========================================================
    // FOLLOW + LOOK AHEAD
    // =========================================================

    private void FollowTarget()
    {
        // Look Ahead: ขยับกล้องนำหน้าทิศที่ Player หันหน้าไป
        targetLookAhead = target.localScale.x > 0
            ? lookAheadDistance
            : -lookAheadDistance;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);

        // Target position
        float targetX = target.position.x + offset.x + currentLookAhead;
        float targetY = target.position.y + offset.y;

        // Clamp ถ้าใช้ Bounds
        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minBounds.x, maxBounds.x);
            targetY = Mathf.Clamp(targetY, minBounds.y, maxBounds.y);
        }

        // Smooth follow
        Vector3 desiredPos = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
    }

    // =========================================================
    // SCREEN SHAKE — เรียกจาก HitEffect, Explosion, etc.
    // =========================================================

    /// <summary>
    /// สั่นกล้อง!
    /// </summary>
    /// <param name="intensity">ความแรงการสั่น (0.1 = เบา, 0.5 = แรง)</param>
    /// <param name="duration">ระยะเวลาสั่น (วินาที)</param>
    public void ShakeCamera(float intensity, float duration)
    {
        // ถ้ามีการสั่นอยู่แล้ว ใช้ค่าที่แรงกว่า
        if (intensity > shakeIntensity || shakeTimer <= 0)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }

    private void HandleScreenShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime; // ใช้ unscaled เพื่อสั่นได้แม้ Slow-Mo

            // ค่อยๆ ลด intensity ตามเวลา
            float progress = shakeTimer / shakeDuration;
            float currentIntensity = shakeIntensity * progress;

            // สุ่มตำแหน่งสั่น
            Vector2 shakeOffset = Random.insideUnitCircle * currentIntensity;
            transform.position += new Vector3(shakeOffset.x, shakeOffset.y, 0f);

            if (shakeTimer <= 0)
            {
                shakeTimer = 0f;
                shakeIntensity = 0f;
            }
        }
    }

    // =========================================================
    // CAMERA ZOOM — เรียกจาก HitEffect (เช่นตอน Parry)
    // =========================================================

    public void ZoomCamera(float zoomAmount, float duration)
    {
        currentZoomAmount = zoomAmount;
        zoomTimer = duration;
    }

    private void HandleZoom()
    {
        if (cam == null) return;

        float targetSize = defaultOrthoSize;

        if (zoomTimer > 0)
        {
            zoomTimer -= Time.unscaledDeltaTime;
            targetSize = defaultOrthoSize - currentZoomAmount;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomInSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomOutSpeed * Time.unscaledDeltaTime);
        }
    }

    // =========================================================
    // GIZMOS — แสดงขอบเขตกล้องใน Editor
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            (minBounds.y + maxBounds.y) / 2f,
            0f
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0.1f
        );
        Gizmos.DrawWireCube(center, size);
    }
}
