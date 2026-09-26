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

    [Tooltip("จุดหมุนหัว (Empty GameObject สำหรับหมุนหัวตามเมาส์)")]
    [SerializeField] private Transform headPivot;

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

    [Tooltip("ระยะมือกระตุกถอยหลังตอนยิง (Visual Recoil)")]
    [SerializeField] private float visualRecoilDistance = 0.3f;
    private float currentVisualRecoil = 0f;
    [Tooltip("ใส่รูป Sprite แสงแฟลชปืนของคุณตรงนี้ได้เลย! (ถ้ามี)")]
    public Sprite customMuzzleFlash;

    [Tooltip("Sprite ไฟจากปากกระบอก (ให้โค้ดจัดการอัตโนมัติ)")]
    [SerializeField] private SpriteRenderer muzzleFlashSprite;

    [Tooltip("ระยะเวลาแสดง Muzzle Flash (วินาที)")]
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    [Header("Gun Visual")]
    [Tooltip("SpriteRenderer ของปืน (สำหรับ Flip)")]
    [SerializeField] private SpriteRenderer gunSprite;

    [Tooltip("SpriteRenderer ของหัว (สำหรับ Flip ไม่ให้หัวกลับด้าน)")]
    [SerializeField] private SpriteRenderer headSprite;

    [Tooltip("ระยะลดระดับความสูงของปืนลงมาตอนสไลด์ (ค่าติดลบคือเลื่อนลง)")]
    [SerializeField] private float slideGunOffsetY = -0.35f;

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
    [Tooltip("ใส่ Image แถบรีโหลดที่ปรับ Image Type เป็น Filled (Fill Method: Horizontal)")]
    [SerializeField] private Image reloadProgressBar;

    // Dynamic HUD
    [Header("Ammo UI Dodge Settings")]
    [Tooltip("ระยะที่ UI กระสุน/รีโหลดเลื่อนขึ้นหลบขอบจอ (แนะนำ 85 คือเลื่อนขึ้นบน)")]
    public float ammoUIDodgeOffsetY = 85f;
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
        reloadAction.AddBinding("<Mouse>/rightButton");
        reloadAction.AddBinding("<Gamepad>/buttonWest");

        CurrentAmmo = ammoCapacity;

        // Auto-create gun pivot if not assigned
        if (gunPivot == null)
        {
            GameObject pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = new Vector3(0.1f, 0.5f, 0f); // อยู่ระดับหัวไหล่กลางลำตัวพอดี
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

        // Auto-create muzzle flash sprite renderer if missing
        if (muzzleFlashSprite == null && muzzlePoint != null)
        {
            GameObject flashObj = new GameObject("MuzzleFlashVisual");
            flashObj.transform.SetParent(muzzlePoint);
            flashObj.transform.localPosition = Vector3.zero;
            muzzleFlashSprite = flashObj.AddComponent<SpriteRenderer>();
            muzzleFlashSprite.sortingOrder = 6;
            muzzleFlashSprite.enabled = false;
        }
    }

    private RectTransform GetAmmoDodgeRect()
    {
        if (ammoText == null) return null;
        RectTransform rt = ammoText.GetComponent<RectTransform>();
        // ถ้า Text อยู่ใต้ Panel เฉพาะของ Ammo/Reload ให้ขยับกล่องนั้น (แต่ห้ามไปขยับ Container หลักของหน้าจอ!)
        if (rt != null && rt.parent != null && rt.parent.GetComponent<Canvas>() == null)
        {
            string pName = rt.parent.name.ToLower();
            if (pName.Contains("ammo") || pName.Contains("reload") || pName.Contains("bullet") || pName.Contains("gun"))
            {
                RectTransform parentRect = rt.parent.GetComponent<RectTransform>();
                if (parentRect != null) return parentRect;
            }
        }
        return rt;
    }

    private Vector3 originalGunPivotLocalPos;

    private void Start()
    {
        ammoRect = GetAmmoDodgeRect();
        if (ammoRect != null)
        {
            ammoOriginalPos = ammoRect.anchoredPosition;
        }

        if (gunPivot != null)
        {
            originalGunPivotLocalPos = gunPivot.localPosition;
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
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive)
        {
            // ซ่อน UI กระสุนเมื่อจบด่าน
            if (ammoRect != null && ammoRect.gameObject.activeSelf) 
            {
                ammoRect.gameObject.SetActive(false);
            }
            else if (ammoText != null && ammoText.gameObject.activeSelf)
            {
                ammoText.gameObject.SetActive(false);
            }
            if (reloadProgressBar != null && reloadProgressBar.gameObject.activeSelf)
            {
                reloadProgressBar.gameObject.SetActive(false);
            }

            return;
        }

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        // ถ้ากำลังรำดาบอยู่ ห้ามยิงปืน! (ป้องกันความขัดแย้งของอนิเมชั่นและเกมเพลย์)
        if (playerParry != null && playerParry.IsParrying)
        {
            if (gunPivot != null) gunPivot.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (gunPivot != null && !gunPivot.gameObject.activeSelf) 
                gunPivot.gameObject.SetActive(true);
        }

        AimAtMouse();
        HandleShooting();
        UpdateMuzzleFlash();

        // Dynamic HUD หลบขอบจอตอนเปิด Slow-Mo หรือ Cinematic Borders
        if (ammoRect == null && ammoText != null)
        {
            ammoRect = GetAmmoDodgeRect();
            if (ammoRect != null) ammoOriginalPos = ammoRect.anchoredPosition;
        }

        if (ammoRect != null && SlowMotionManager.Instance != null)
        {
            bool isSlowMo = SlowMotionManager.Instance.IsSlowMotionActive;
            // เช็คว่าอยู่ด้านล่างหรือบนของจอ เพื่อให้ขยับเข้าหาตรงกลางจอเสมอ ไม่ตกขอบจอ
            float dodgeDir = (ammoRect.anchorMin.y < 0.5f || ammoOriginalPos.y > 0) ? Mathf.Abs(ammoUIDodgeOffsetY) : -Mathf.Abs(ammoUIDodgeOffsetY);
            float targetY = isSlowMo ? (ammoOriginalPos.y + dodgeDir) : ammoOriginalPos.y;
            Vector2 pos = ammoRect.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, 15f * Time.unscaledDeltaTime);
            ammoRect.anchoredPosition = pos;
        }

        // --- Visual Recoil (ดึงแขน/ปืนกระตุกถอยหลัง) ---
        if (gunPivot != null)
        {
            currentVisualRecoil = Mathf.Lerp(currentVisualRecoil, 0f, 15f * Time.deltaTime);
            
            // คำนวณทิศทางถอยหลังในแกน Local ของ Player
            Vector3 recoilDirLocal = -gunPivot.parent.InverseTransformDirection(gunPivot.right);
            
            // ถ้ากำลังสไลด์ ให้เลื่อนปืนลงต่ำลงมาหน่อยให้ตรงกับตัวที่แบนลง และซ่อนหัวที่แยกชิ้นไว้ (เพราะวาดรวมในอนิเมชั่นแล้ว)
            Vector3 targetPivotPos = originalGunPivotLocalPos;
            bool isSliding = playerController != null && playerController.IsShadowDashing;
            
            if (isSliding)
            {
                targetPivotPos.y += slideGunOffsetY; // เลื่อนแกนแขนลงมาข้างล่างตามค่าที่ตั้งใน Inspector
                if (headSprite != null) headSprite.enabled = false;
                else if (headPivot != null) headPivot.gameObject.SetActive(false);
            }
            else
            {
                if (headSprite != null) headSprite.enabled = true;
                else if (headPivot != null && !headPivot.gameObject.activeSelf) headPivot.gameObject.SetActive(true);
            }

            gunPivot.localPosition = targetPivotPos + (recoilDirLocal * currentVisualRecoil);
        }

        UpdateAmmoUI();
    }

    // =========================================================
    // AIM — หมุนปืนตามเมาส์
    // =========================================================

    private Vector2 GetMouseWorldPos()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return Vector2.zero;

        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCam.transform.position.z)));
        return mouseWorldPos;
    }

    private void AimAtMouse()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || gunPivot == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPos();

        // คำนวณมุมจาก gunPivot ไป Mouse
        Vector2 direction = (mouseWorldPos - gunPivot.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // ถ้า Player หันซ้าย (localScale.x < 0) ต้อง flip มุม
        if (transform.localScale.x < 0)
        {
            angle += 180f;
        }

        gunPivot.rotation = Quaternion.Euler(0, 0, angle);

        // หมุนหัวเบาๆ (ก้ม-เงย ไม่เกิน 25 องศา จะได้ไม่คอหักและไม่กลับหัว!)
        if (headPivot != null)
        {
            float headAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
            headAngle = Mathf.Clamp(headAngle, -25f, 25f);
            headPivot.localRotation = Quaternion.Euler(0, 0, headAngle);
        }

        // ปืนไม่กลับหัวแน่นอนเพราะ parent.localScale.x = -1 ควบคู่กับ angle+180 ทำให้แกน Y ของปืนชี้ขึ้นบนเสมอ!
        if (gunSprite != null)
        {
            gunSprite.flipY = false;
        }
    }

    // =========================================================
    // SHOOTING — ยิง!
    // =========================================================

    private void HandleShooting()
    {
        if (isReloading) return;

        // เช็คกดปุ่มรีโหลดแมนนวล
        if (reloadAction.WasPressedThisFrame() && CurrentAmmo < ammoCapacity)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        fireTimer -= Time.deltaTime;
        
        // กดยิงทีละนัด (Semi-auto) 
        isShooting = shootAction.WasPressedThisFrame();

        if (isShooting && fireTimer <= 0)
        {
            if (CurrentAmmo > 0)
            {
                Fire();
                
                CurrentAmmo--;
                
                fireTimer = 1f / fireRate;
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
        
        // === เล่นเสียงรีโหลด ===
        if (AudioManager.Instance != null && AudioManager.Instance.reloadSound != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.reloadSound, 0.7f);
        }

        float timer = 0f;
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            if (reloadProgressBar != null)
            {
                reloadProgressBar.fillAmount = timer / reloadTime;
            }
            yield return null; // รอจนกว่าจะถึงเฟรมถัดไป
        }
        
        if (reloadProgressBar != null) reloadProgressBar.fillAmount = 0f; // ซ่อนหลอดเมื่อรีโหลดเสร็จ
        
        CurrentAmmo = ammoCapacity;
        isReloading = false;
    }

    private void Fire()
    {
        if (muzzlePoint == null) return;

        // === Spawn Bullet ===
        if (bulletPrefab != null)
        {
            Vector2 mouseWorldPos = GetMouseWorldPos();
            Vector2 direction = (mouseWorldPos - (Vector2)muzzlePoint.position).normalized;
            if (Vector2.Distance(mouseWorldPos, muzzlePoint.position) < 0.2f)
            {
                direction = (muzzlePoint.position - gunPivot.position).normalized;
            }

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

        // === Sound Effect ===
        if (AudioManager.Instance != null && AudioManager.Instance.shootSounds != null && AudioManager.Instance.shootSounds.Length > 0)
        {
            AudioClip randomShootSound = AudioManager.Instance.shootSounds[UnityEngine.Random.Range(0, AudioManager.Instance.shootSounds.Length)];
            AudioManager.Instance.PlaySFX(randomShootSound, 0.6f);
        }

        // === Visual Recoil (ปืนกระตุกถอยหลัง) ===
        currentVisualRecoil = visualRecoilDistance;

        // === Physics Recoil (ดันตัวผู้เล่น) ===
        if (rb != null)
        {
            Vector2 recoilDir = -(muzzlePoint.position - gunPivot.position).normalized;
            rb.AddForce(recoilDir * recoilForce, ForceMode2D.Impulse);
        }
    }

    /// <summary>สร้างกระสุน Default ถ้ายังไม่ได้ assign Bullet Prefab</summary>
    private void SpawnDefaultBullet()
    {
        Vector2 mouseWorldPos = GetMouseWorldPos();
        Vector2 direction = (mouseWorldPos - (Vector2)muzzlePoint.position).normalized;
        if (Vector2.Distance(mouseWorldPos, muzzlePoint.position) < 0.2f)
        {
            direction = (muzzlePoint.position - gunPivot.position).normalized;
        }

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
        if (muzzlePoint != null)
        {
            Vector2 direction = (muzzlePoint.position - gunPivot.position).normalized;
            HitEffect.Instance?.PlayMuzzleFlash(muzzlePoint.position, direction);
        }
        
        // เก็บของเก่าไว้เผื่อมีการลากใส่ใน Inspector
        if (muzzleFlashSprite != null)
        {
            // ถ้าผู้เล่นลากรูปมาใส่ ให้ใช้รูปนั้นแทน
            if (customMuzzleFlash != null)
            {
                muzzleFlashSprite.sprite = customMuzzleFlash;
                muzzleFlashSprite.color = Color.white;
            }

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
            // เติมเลข 0 ข้างหน้าถ้าเป็นเลขหลักเดียว (เช่น 05 / 12) เพื่อป้องกันตัวหนังสือดิ้น
            ammoText.text = $"{CurrentAmmo.ToString("00")} / {ammoCapacity.ToString("00")}"; 
        }
    }
}
