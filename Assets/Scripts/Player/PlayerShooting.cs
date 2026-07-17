// ============================================================
// PlayerShooting.cs — ระบบยิงปืน + เล็งด้วยเมาส์
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบยิงปืนของ "จอห์น ไข่เจียว":
/// - เล็งด้วยเมาส์ (หมุนแขนปืนตาม Mouse)
/// - ยิงรัวได้ตาม Fire Rate
/// - Muzzle Flash (เอฟเฟกต์ไฟปากกระบอกปืน)
/// - Recoil (แรงสะท้อนเล็กน้อย)
/// 
/// ต้องสร้าง Child Object "GunPivot" ที่มี "MuzzlePoint" เป็น child อีกที
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    [Tooltip("จุดหมุนปืน (Empty GameObject เป็น Child ของ Player)")]
    [SerializeField] private Transform gunPivot;

    [Tooltip("จุดปากกระบอกปืน (Child ของ GunPivot)")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("Prefab กระสุน")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Shooting Settings")]
    [Tooltip("จำนวนนัดต่อวินาที")]
    [SerializeField] private float fireRate = 8f;

    [Tooltip("จำนวนกระสุนสูงสุดในแม็กกาซีน")]
    [SerializeField] private int ammoCapacity = 12;

    [Tooltip("เวลาที่ใช้ในการรีโหลด (วินาที)")]
    [SerializeField] private float reloadTime = 1.2f;

    [Tooltip("แรง Recoil (ดันตัวถอยหลัง)")]
    [SerializeField] private float recoilForce = 1.5f;

    [Header("Muzzle Flash")]
    [Tooltip("Sprite ไฟจากปากกระบอก (ถ้ามี)")]
    [SerializeField] private SpriteRenderer muzzleFlashSprite;

    [Tooltip("ระยะเวลาแสดง Muzzle Flash (วินาที)")]
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    [Header("Gun Visual")]
    [Tooltip("SpriteRenderer ของปืน (สำหรับ Flip)")]
    [SerializeField] private SpriteRenderer gunSprite;

    // ===================== Internal =====================
    private InputAction shootAction;
    private InputAction reloadAction;
    private Rigidbody2D rb;
    private Camera mainCam;
    private float fireTimer;
    private float muzzleFlashTimer;
    private bool isShooting;
    private bool isReloading;
    public int CurrentAmmo { get; private set; }
    private PlayerController playerController;
    private PlayerParry playerParry;

    [Header("UI")]
    [Tooltip("ลาก Text (TextMeshPro) จาก Canvas มาใส่ตรงนี้เพื่อแสดงกระสุน (ถ้าไม่ใส่จะไม่มี UI กระสุน)")]
    [SerializeField] private TextMeshProUGUI ammoText;

    // Dynamic HUD
    private RectTransform ammoRect;
    private Vector2 ammoOriginalPos;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        playerController = GetComponent<PlayerController>();
        playerParry = GetComponent<PlayerParry>();

        // Setup input — Left Mouse Button / Right Trigger
        shootAction = new InputAction("Shoot", InputActionType.Button);
        shootAction.AddBinding("<Mouse>/leftButton");
        shootAction.AddBinding("<Gamepad>/rightTrigger");

        reloadAction = new InputAction("Reload", InputActionType.Button);
        reloadAction.AddBinding("<Keyboard>/r");
        reloadAction.AddBinding("<Gamepad>/buttonWest");

        CurrentAmmo = ammoCapacity;

        if (ammoText != null)
        {
            ammoRect = ammoText.GetComponent<RectTransform>();
            if (ammoRect != null) ammoOriginalPos = ammoRect.anchoredPosition;
        }

        // Auto-create gun pivot if not assigned
        if (gunPivot == null)
        {
            GameObject pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            gunPivot = pivot.transform;

            // Create muzzle point
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(gunPivot);
            muzzle.transform.localPosition = new Vector3(0.8f, 0f, 0f);
            muzzlePoint = muzzle.transform;

            // Create gun visual (placeholder rectangle)
            GameObject gunVisual = new GameObject("GunVisual");
            gunVisual.transform.SetParent(gunPivot);
            gunVisual.transform.localPosition = new Vector3(0.4f, 0f, 0f);
            gunVisual.transform.localScale = new Vector3(0.6f, 0.15f, 1f);
            gunSprite = gunVisual.AddComponent<SpriteRenderer>();
            gunSprite.sprite = CreatePlaceholderSprite();
            gunSprite.color = new Color(0.3f, 0.3f, 0.3f); // สีเทาเข้ม (ปืน)
            gunSprite.sortingOrder = 5;
        }
    }

    private void OnEnable()
    {
        shootAction.Enable();
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
        reloadAction.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameManager.GameState.GameOver || GameManager.Instance.CurrentState == GameManager.GameState.Victory))
        {
            return;
        }

        // ถ้ากำลังรำดาบอยู่ ห้ามยิงปืน! (ป้องกันความขัดแย้งของอนิเมชั่นและเกมเพลย์)
        if (playerParry != null && playerParry.IsParrying) return;

        AimAtMouse();
        HandleShooting();
        UpdateMuzzleFlash();

        // Dynamic HUD หลบขอบจอ
        if (ammoRect != null && SlowMotionManager.Instance != null)
        {
            float direction = (ammoRect.anchorMin.y >= 0.5f) ? -1f : 1f;
            float targetY = SlowMotionManager.Instance.IsSlowMotionActive ? ammoOriginalPos.y + (150f * direction) : ammoOriginalPos.y;
            Vector2 targetPos = new Vector2(ammoOriginalPos.x, targetY);
            ammoRect.anchoredPosition = Vector2.Lerp(ammoRect.anchoredPosition, targetPos, 10f * Time.unscaledDeltaTime);
        }
        UpdateAmmoUI();
    }

    // =========================================================
    // AIM — หมุนปืนตามเมาส์
    // =========================================================

    private void AimAtMouse()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || gunPivot == null) return;

        // แปลง Mouse Position → World Position
        Vector2 mouseScreenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCam.transform.position.z))
        );

        // คำนวณมุมจาก gunPivot ไป Mouse
        Vector2 direction = (mouseWorldPos - gunPivot.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // ถ้า Player หันซ้าย (localScale.x < 0) ต้อง flip มุม
        if (transform.localScale.x < 0)
        {
            angle += 180f;
        }

        gunPivot.rotation = Quaternion.Euler(0, 0, angle);

        // Flip Gun Sprite (ไม่ให้ปืนกลับหัวตอนเล็งขึ้น)
        if (gunSprite != null)
        {
            gunSprite.flipY = (angle > 90 || angle < -90) && transform.localScale.x > 0
                || (angle <= 90 && angle >= -90) && transform.localScale.x < 0;
        }
    }

    // =========================================================
    // SHOOTING — ยิง!
    // =========================================================

    private void HandleShooting()
    {
        if (isReloading) return;

        bool isCrispy = CrispyModeManager.Instance != null && CrispyModeManager.Instance.IsCrispyActive;

        // เช็คกดปุ่มรีโหลดแมนนวล (ถ้าร่างกรอบ ไม่ต้องรีโหลดเพราะกระสุนไม่จำกัด)
        if (reloadAction.WasPressedThisFrame() && CurrentAmmo < ammoCapacity && !isCrispy)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        fireTimer -= Time.deltaTime;
        
        // กดยิงทีละนัด (Semi-auto) 
        isShooting = shootAction.WasPressedThisFrame();

        if (isShooting && fireTimer <= 0)
        {
            if (CurrentAmmo > 0 || isCrispy) // ถ้าร่างกรอบ ยิงได้เสมอ
            {
                Fire();
                
                if (!isCrispy)
                {
                    CurrentAmmo--;
                }
                
                // ดึง Multiplier จาก CrispyMode ถ้ามี
                float finalFireRate = fireRate;
                if (isCrispy)
                {
                    finalFireRate *= CrispyModeManager.Instance.CurrentFireRateMultiplier;
                }
                
                fireTimer = 1f / finalFireRate;
            }
            else
            {
                // กระสุนหมด → ออโต้รีโหลด
                StartCoroutine(ReloadRoutine());
            }
        }
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("[PlayerShooting] Reloading...");
        
        // ถ้าต้องการเพิ่มเสียง Reload ให้ใส่ตรงนี้
        yield return new WaitForSeconds(reloadTime);
        
        CurrentAmmo = ammoCapacity;
        isReloading = false;
        Debug.Log("[PlayerShooting] Reload Complete!");
    }

    private void Fire()
    {
        if (muzzlePoint == null) return;

        // === Spawn Bullet ===
        if (bulletPrefab != null)
        {
            Vector2 direction = (muzzlePoint.position - gunPivot.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.identity);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(direction);
            }
        }
        else
        {
            // ถ้ายังไม่มี Bullet Prefab → สร้าง Default Bullet
            SpawnDefaultBullet();
        }

        // === Muzzle Flash ===
        ShowMuzzleFlash();

        // === Recoil ===
        if (rb != null)
        {
            Vector2 recoilDir = -(muzzlePoint.position - gunPivot.position).normalized;
            rb.AddForce(recoilDir * recoilForce, ForceMode2D.Impulse);
        }
    }

    /// <summary>สร้างกระสุน Default ถ้ายังไม่ได้ assign Bullet Prefab</summary>
    private void SpawnDefaultBullet()
    {
        Vector2 direction = (muzzlePoint.position - gunPivot.position).normalized;

        GameObject bullet = new GameObject("Bullet");
        bullet.transform.position = muzzlePoint.position;
        bullet.layer = LayerMask.NameToLayer("Default");

        // Visual
        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.color = new Color(1f, 0.4f, 0.1f); // สีส้ม (ซอสพริกศรีราชา!)
        sr.sortingOrder = 10;
        bullet.transform.localScale = new Vector3(0.2f, 0.1f, 1f);

        // Physics
        var rb2d = bullet.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0;
        rb2d.linearVelocity = direction * 20f;

        // Collider
        var col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Bullet script
        var bulletScript = bullet.AddComponent<Bullet>();
        bulletScript.Initialize(direction);

        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // =========================================================
    // MUZZLE FLASH
    // =========================================================

    private void ShowMuzzleFlash()
    {
        if (muzzleFlashSprite != null)
        {
            muzzleFlashSprite.enabled = true;
            muzzleFlashTimer = muzzleFlashDuration;

            // สุ่มขนาดเล็กน้อยเพื่อ Variety
            float randomScale = Random.Range(0.8f, 1.3f);
            muzzleFlashSprite.transform.localScale = Vector3.one * randomScale;
            muzzleFlashSprite.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }
    }

    private void UpdateMuzzleFlash()
    {
        if (muzzleFlashTimer > 0)
        {
            muzzleFlashTimer -= Time.deltaTime;
            if (muzzleFlashTimer <= 0 && muzzleFlashSprite != null)
            {
                muzzleFlashSprite.enabled = false;
            }
        }
    }

    // =========================================================
    // UTILITIES
    // =========================================================

    /// <summary>สร้าง Sprite สี่เหลี่ยมขาว (Placeholder)</summary>
    private Sprite CreatePlaceholderSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
    
    // =========================================================
    // UI
    // =========================================================

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            bool isCrispy = CrispyModeManager.Instance != null && CrispyModeManager.Instance.IsCrispyActive;

            if (isCrispy)
            {
                ammoText.text = "AMMO: INFINITE!";
                ammoText.color = Color.yellow;
            }
            else if (isReloading)
            {
                ammoText.text = "RELOADING...";
                ammoText.color = Color.white;
            }
            else
            {
                ammoText.text = $"AMMO: {CurrentAmmo} / {ammoCapacity}";
                ammoText.color = Color.white;
            }
        }
    }
}
