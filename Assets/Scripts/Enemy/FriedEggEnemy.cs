// ============================================================
// FriedEggEnemy.cs — ศัตรูไข่ดาว (AI + Patrol + Shoot)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// ศัตรูไข่ดาว — หัวใจของเกม Omelet Rush!
/// 
/// กลไกหลัก:
/// - ไข่ขาว (รอบนอก) = โดนยิงแล้ว Damage น้อย
/// - ไข่แดง (ตรงกลาง) = โดนยิงแล้ว ตายทันที! (Critical Hit)
/// 
/// AI Behavior:
/// 1. Patrol — เดินไปมาซ้ายขวา
/// 2. Detect — เห็นผู้เล่นในระยะ → หยุดเดิน + หันหน้าหาผู้เล่น
/// 3. Attack — ยิงกระสุนใส่ผู้เล่น
/// 
/// ระบบตรวจจับการยิง ใช้ Distance-based:
/// กระสุนโดนตรงไหน → วัดระยะจากจุดกลาง (ไข่แดง)
/// ถ้าใกล้ไข่แดง = Critical / ถ้าอยู่บริเวณไข่ขาว = Body Shot
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FriedEggEnemy : MonoBehaviour, IDamageable
{
    // ===================== WEAK POINT =====================
    [Header("=== Weak Point (ไข่แดง) ===")]
    [Tooltip("รัศมีไข่แดง — ยิงโดนในวงนี้ = ตายทันที")]
    [SerializeField] private float yolkRadius = 0.4f;

    [Tooltip("ตำแหน่งไข่แดง (ถ้าไม่ใส่จะใช้ตำแหน่งกลางตัว)")]
    [SerializeField] private Transform yolkCenter;

    [Tooltip("ตัวคูณความแรงเมื่อยิงโดนไข่แดง (เช่น 3.33 = 3 นัดตาย)")]
    [SerializeField] private float yolkDamageMultiplier = 3.33f;

    [Header("=== Body (ไข่ขาว) ===")]
    [Tooltip("HP ของไข่ขาว (ยิงตรง Body จะลด HP นี้)")]
    [SerializeField] private float whiteHealth = 100f;

    [Tooltip("Damage ที่ได้รับเมื่อโดนยิงบริเวณไข่ขาว")]
    [SerializeField] private float whiteDamagePerHit = 10f;

    // ===================== PATROL =====================
    [Header("=== Patrol Settings ===")]
    [Tooltip("ระยะเดิน Patrol จากจุดเริ่มต้น")]
    [SerializeField] private float patrolDistance = 3f;

    [Tooltip("ความเร็วเดิน")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("เวลารอก่อนกลับตัว (วินาที)")]
    [SerializeField] private float waitTimeAtEnd = 0.5f;

    // ===================== DETECTION =====================
    [Header("=== Detection ===")]
    [Tooltip("ระยะมองเห็นผู้เล่น")]
    [SerializeField] private float detectionRange = 8f;

    [Tooltip("มุมมองเห็น (องศา, จาก forward direction)")]
    [SerializeField] private float detectionAngle = 90f;

    [Tooltip("Layer ของกำแพง/พื้น ที่ใช้บังสายตา (Obstacles)")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("Layer ของผู้เล่น")]
    [SerializeField] private LayerMask playerLayer;

    // ===================== ATTACK =====================
    [Header("=== Attack ===")]
    [Tooltip("Prefab กระสุนศัตรู")]
    [SerializeField] private GameObject enemyBulletPrefab;

    [Tooltip("ความแม่นยำการยิง (องศากระจายของกระสุน ยิ่งเยอะยิ่งยิงมั่ว)")]
    [SerializeField] private float accuracySpread = 8f;

    [Tooltip("จุดยิงกระสุน")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Fire Rate (นัดต่อวินาที)")]
    [SerializeField] private float fireRate = 1.5f;

    [Tooltip("ความเร็วกระสุน")]
    [SerializeField] private float bulletSpeed = 32f; // เร่งความเร็วเพื่อกันการสไลด์หนีง่ายๆ

    [Header("=== Advanced Attack ===")]
    [Tooltip("ยิงเป็นชุด (Burst) ใส่ 3 คือยิง 3 นัดติดกัน")]
    [SerializeField] private int burstCount = 3;
    
    [Tooltip("ระยะห่างระหว่างกระสุนในชุด (วินาที)")]
    [SerializeField] private float timeBetweenBurstShots = 0.12f;
    
    [Tooltip("ยิงกี่ชุดถึงจะรีโหลด")]
    [SerializeField] private int burstsBeforeReload = 2;
    
    [Tooltip("เวลาที่ใช้รีโหลด (วินาที)")]
    [SerializeField] private float reloadTime = 2.5f;

    // ===================== VISUALS =====================
    [Header("=== Visuals (Placeholder) ===")]
    [SerializeField] private SpriteRenderer whiteRenderer;  // ไข่ขาว
    [SerializeField] private SpriteRenderer yolkRenderer;    // ไข่แดง
    
    [Tooltip("จุดหมุนหัว (แยกจากตัว เพื่อหันตาม Player)")]
    [SerializeField] private Transform headPivot;
    
    [Tooltip("จุดหมุนปืนและแขน (แยกจากตัว เพื่อหันตาม Player)")]
    [SerializeField] private Transform gunPivot;

    // ===================== STATE =====================
    public bool IsAlive => currentHealth > 0;

    private enum EnemyState { Idle, Alert, Attack, Dead, EnteringDoor }
    private EnemyState state = EnemyState.Idle;

    private float currentHealth;
    private float patrolStartX;
    private int patrolDirection = 1;
    private float waitTimer;
    private float fireTimer;
    private bool isFacingRight = true;
    private Rigidbody2D rb;
    private Transform playerTransform;

    private int currentBurstsFired = 0;
    private bool isShootingBurst = false;
    private bool isReloadingEnemy = false;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        currentHealth = whiteHealth;
        patrolStartX = transform.position.x;

        // Auto-create yolk center — เลื่อนไปด้านหน้า!
        // ไข่แดงอยู่ด้าน "หน้า" ของไข่ดาว
        // เมื่อไข่ดาว Flip (เปลี่ยน scale.x) → ไข่แดงจะย้ายฝั่งอัตโนมัติ
        if (yolkCenter == null)
        {
            GameObject yc = new GameObject("YolkCenter");
            yc.transform.SetParent(transform);
            yc.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            yolkCenter = yc.transform;
        }

        // Auto-create fire point
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            firePoint = fp.transform;
        }

        // Auto-create visuals if not assigned
        if (whiteRenderer == null || yolkRenderer == null)
        {
            CreatePlaceholderVisuals();
        }

        // Add trigger collider for bullet detection
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;
        }

    }

    private void Start()
    {
        // หา Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) return;

        // หา Player (พยายามหาเรื่อยๆ แต่ไม่บล็อค idle)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // หันหน้าและเล็งปืนตาม Player ตลอดเวลาถ้าเห็น
        if (playerTransform != null && state != EnemyState.EnteringDoor && state != EnemyState.Dead)
        {
            AimAtPlayer();
        }

        switch (state)
        {
            case EnemyState.EnteringDoor:
                // เพิ่งเกิดจากประตู: เดินออกมามึนๆ
                transform.Translate(Vector2.right * patrolDirection * moveSpeed * Time.deltaTime);
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    state = EnemyState.Idle;
                }
                break;
            case EnemyState.Idle:
                // อยู่เฉยๆ รอผู้เล่นเข้าระยะ
                if (playerTransform != null) CheckForPlayer();
                break;
            case EnemyState.Alert:
            case EnemyState.Attack:
                if (playerTransform != null)
                    UpdateAttack();
                else
                    state = EnemyState.Idle; // กลับ idle ถ้าหา Player ไม่เจอ
                CheckPlayerOutOfRange();
                break;
        }
    }

    private void AimAtPlayer()
    {
        // 1. หันตัวพลิกซ้ายขวา
        float dirToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
        FaceDirection(dirToPlayer > 0);

        // 2. หมุนปืนและหัวให้ชี้ไปหา Player
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (transform.localScale.x < 0)
        {
            angle += 180f; // ถ้าหันซ้ายต้องบวก 180 องศาให้ปืนพลิกถูก
        }

        if (gunPivot != null)
        {
            gunPivot.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (headPivot != null)
        {
            // หมุนหัวเบาๆ (ก้ม-เงย ไม่เกิน 25 องศา)
            float headAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
            headAngle = Mathf.Clamp(headAngle, -25f, 25f);
            headPivot.localRotation = Quaternion.Euler(0, 0, headAngle);
        }
    }

    // =========================================================
    // AI — DETECTION
    // =========================================================

    private void CheckForPlayer()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= detectionRange)
        {
            // เช็คมุมมอง
            Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
            Vector2 forward = isFacingRight ? Vector2.right : Vector2.left;
            float angle = Vector2.Angle(forward, dirToPlayer);

            if (angle <= detectionAngle)
            {
                // เช็คว่ามีกำแพงบังหรือไม่ (Line of Sight)
                RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleLayer);
                if (hit.collider == null)
                {
                    state = EnemyState.Attack;
                    // หยุดเดิน (ไม่ต้องตั้ง velocity เพราะเป็น Kinematic + ใช้ MovePosition)
                }
            }
        }
    }

    private void CheckPlayerOutOfRange()
    {
        if (playerTransform == null)
        {
            state = EnemyState.Idle;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        
        // ถ้าอยู่ไกลไป หรือ มีกำแพงบัง (หลบหลังกำแพง)
        bool isBehindWall = Physics2D.Linecast(transform.position, playerTransform.position, obstacleLayer).collider != null;

        if (distance > detectionRange * 1.5f || isBehindWall) // ให้ระยะหลุดไกลกว่าระยะตรวจจับ
        {
            state = EnemyState.Idle;
        }
    }

    // =========================================================
    // AI — ATTACK
    // =========================================================

    private void UpdateAttack()
    {
        if (playerTransform == null) return;

        // หันหน้าหาผู้เล่น
        FaceDirection(playerTransform.position.x > transform.position.x);

        // หันปืนหาผู้เล่น
        if (firePoint != null)
        {
            Vector2 dir = playerTransform.position - firePoint.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            if (!isFacingRight) angle += 180f; // ชดเชยการกลับด้านของตัวแม่
            
            firePoint.localRotation = Quaternion.Euler(0, 0, angle);
        }

        // ยิง
        if (isShootingBurst || isReloadingEnemy) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            StartCoroutine(BurstFireRoutine());
        }
    }

    private IEnumerator BurstFireRoutine()
    {
        isShootingBurst = true;

        // ยิง 1 ชุด (หลายนัด)
        for (int i = 0; i < burstCount; i++)
        {
            if (playerTransform == null || !IsAlive) break;
            
            FireAtPlayer();
            // สุ่มหน่วงเวลานิดๆ ให้การยิงดูไม่เป็นหุ่นยนต์เกินไป
            float randomBurstDelay = timeBetweenBurstShots + Random.Range(-0.02f, 0.05f);
            yield return new WaitForSeconds(Mathf.Max(0.01f, randomBurstDelay));
        }

        currentBurstsFired++;

        if (currentBurstsFired >= burstsBeforeReload)
        {
            // หมดแม็กซ์ ต้อง Reload
            isReloadingEnemy = true;
            isShootingBurst = false;
            
            // เปลี่ยนสีปืนเพื่อบอกว่ากำลัง Reload
            SpriteRenderer gunRenderer = firePoint != null ? firePoint.GetComponent<SpriteRenderer>() : null;
            Color oldGunColor = Color.gray;
            if (gunRenderer != null) 
            {
                oldGunColor = gunRenderer.color;
                gunRenderer.color = new Color(1f, 0.3f, 0.3f); // ปืนสีแดง = Reload
            }

            yield return new WaitForSeconds(reloadTime);

            if (gunRenderer != null) gunRenderer.color = oldGunColor;

            currentBurstsFired = 0;
            isReloadingEnemy = false;
            // พักแป๊บนึงก่อนยิงชุดถัดไปหลังรีโหลดเสร็จ + สุ่มหน่วงเวลาให้คาดเดายาก
            fireTimer = (1f / fireRate) + Random.Range(0f, 0.3f); 
        }
        else
        {
            // ยังไม่หมดแม็กซ์ รอ fireRate ปกติ + สุ่มจังหวะให้ยิงเหลื่อมๆ กัน
            isShootingBurst = false;
            fireTimer = (1f / fireRate) + Random.Range(0f, 0.2f);
        }
    }

    private void FireAtPlayer()
    {
        if (playerTransform == null || firePoint == null) return;

        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)firePoint.position).normalized;

        // สุ่มให้กระสุนกระจาย (Accuracy Spread)
        float spreadAngle = Random.Range(-accuracySpread, accuracySpread);
        direction = Quaternion.Euler(0, 0, spreadAngle) * direction;

        if (enemyBulletPrefab != null)
        {
            GameObject bullet = Instantiate(enemyBulletPrefab, firePoint.position, Quaternion.identity);
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null) eb.Initialize(direction, bulletSpeed);
        }
        else
        {
            // สร้าง Default Bullet
            SpawnDefaultEnemyBullet(direction);
        }

        // เอฟเฟกต์แสงไฟปลายปืน
        HitEffect.Instance?.PlayMuzzleFlash(firePoint.position, direction);
    }

    private void SpawnDefaultEnemyBullet(Vector2 direction)
    {
        GameObject bullet = new GameObject("EnemyBullet");
        bullet.transform.position = firePoint.position;

        var sr = bullet.AddComponent<SpriteRenderer>();
        // สร้างสี่เหลี่ยมผืนผ้าให้เหมือนกระสุนปืน Player
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        
        sr.color = new Color(1f, 0.7f, 0f); // กระสุนสีส้มทอง
        sr.sortingOrder = 10;
        bullet.transform.localScale = new Vector3(0.3f, 0.1f, 1f);

        var rb2d = bullet.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0;
        rb2d.linearVelocity = direction * bulletSpeed;

        var col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var eb = bullet.AddComponent<EnemyBullet>();
        eb.Initialize(direction, bulletSpeed);

        // Rotate
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // =========================================================
    // HIT DETECTION — ไข่แดง vs ไข่ขาว
    // =========================================================

    /// <summary>
    /// เรียกโดย Bullet เมื่อกระสุนโดนตัว
    /// ตรวจว่าโดน "ไข่แดง" (Critical) หรือ "ไข่ขาว" (Body Shot)
    /// </summary>
    public void HandleBulletHit(Vector2 hitPoint, float damage, Vector2 hitDirection)
    {
        if (!IsAlive) return;

        float distToYolk = Vector2.Distance(hitPoint, yolkCenter.position);

        if (distToYolk <= yolkRadius)
        {
            // ========== CRITICAL HIT! ยิงโดนไข่แดง! ==========
            Debug.Log("🎯 CRITICAL HIT! ยิงโดนไข่แดง!");
            
            // ใช้ค่าจาก Inspector มาคูณดาเมจ
            TakeDamage(damage * yolkDamageMultiplier, hitPoint, true, hitDirection);
        }
        else
        {
            // ========== BODY SHOT — ยิงโดนไข่ขาว ==========
            Debug.Log($"💫 Body Shot — ไข่ขาวเหลือ HP: {currentHealth - whiteDamagePerHit}");
            TakeDamage(whiteDamagePerHit, hitPoint, false, hitDirection);
        }
    }

    // =========================================================
    // IDamageable IMPLEMENTATION
    // =========================================================

    public void TakeDamage(float damage, Vector2 hitPoint, bool isCritical, Vector2 hitDirection)
    {
        if (!IsAlive) return;

        // เติมหลอด Slow-Motion หากกำลังเปิดอยู่
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.AddMeterFromHit();
        }
        ScreenEffectManager.Instance?.TriggerAdrenaline();

        currentHealth -= damage;

        // เล่น Hit Effect
        if (HitEffect.Instance != null)
        {
            if (isCritical)
            {
                HitEffect.Instance.PlayCriticalHitEffect(hitPoint, hitDirection);
            }
            else
            {
                HitEffect.Instance.PlayNormalHitEffect(hitPoint, hitDirection);
            }
        }

        // Visual feedback — ไข่ขาวกระพริบ
        StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
        {
            Die(isCritical, hitPoint);
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die(bool isCritical, Vector2 hitPoint)
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Dead;

        Debug.Log(isCritical
            ? "💥 ไข่ดาวตาย! (Critical — ไข่แดงแตก!)"
            : "☠️ ไข่ดาวตาย! (HP หมด)");

        // Hit Effect
        if (HitEffect.Instance != null)
        {
            // ถ้าตาย ให้เลือดสาดกระจายออกทุกทิศทาง หรือใช้ hitPoint ชั่วคราวไปก่อน (เดี๋ยวปรับเป็น hitDirection ตอนตายได้)
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (isCritical)
                HitEffect.Instance.PlayCriticalHitEffect(hitPoint, randomDir);
            else
                HitEffect.Instance.PlayNormalHitEffect(hitPoint, randomDir);
        }

        // แจ้ง LevelRatingManager (นับ Kills)
        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.RegisterKill(isCritical);
        }



        // เล่น Death Animation/Effect
        StartCoroutine(DeathSequence(isCritical));
    }

    private IEnumerator DeathSequence(bool isCritical)
    {
        rb.linearVelocity = Vector2.zero;

        if (isCritical)
        {
            // ไข่แดงระเบิด! → สเกลพองแล้วหาย
            if (yolkRenderer != null)
            {
                yolkRenderer.color = new Color(1f, 0.3f, 0f); // สีส้มแดง
            }

            float t = 0;
            Vector3 originalScale = transform.localScale;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float scale = 1f + t * 3f;
                transform.localScale = originalScale * scale;

                // Fade out
                if (whiteRenderer != null)
                {
                    Color c = whiteRenderer.color;
                    c.a = 1f - (t / 0.3f);
                    whiteRenderer.color = c;
                }
                if (yolkRenderer != null)
                {
                    Color c = yolkRenderer.color;
                    c.a = 1f - (t / 0.3f);
                    yolkRenderer.color = c;
                }

                yield return null;
            }
        }
        else
        {
            // ตายช้า → เปลี่ยนสีเป็นเทาแล้ว Fade
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float alpha = 1f - (t / 0.5f);
                if (whiteRenderer != null)
                {
                    whiteRenderer.color = new Color(0.5f, 0.5f, 0.5f, alpha);
                }
                if (yolkRenderer != null)
                {
                    yolkRenderer.color = new Color(0.5f, 0.4f, 0.2f, alpha);
                }
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    // =========================================================
    // VISUAL FEEDBACK
    // =========================================================

    private IEnumerator FlashWhite()
    {
        if (whiteRenderer == null) yield break;

        Color originalColor = whiteRenderer.color;
        whiteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (whiteRenderer != null) whiteRenderer.color = Color.white;
        if (yolkRenderer != null) yolkRenderer.color = new Color(1f, 0.8f, 0f);
    }
    
    // =========================================================
    // EXTERNAL TRIGGERS
    // =========================================================

    public void SpawnFromDoor(int direction)
    {
        state = EnemyState.EnteringDoor;
        patrolDirection = direction;
        FaceDirection(direction > 0);
        waitTimer = 1.0f; // เดินมึนๆ ออกมา 1 วินาที ค่อยเริ่ม Patrol
    }

    private void FaceDirection(bool faceRight)
    {
        if (isFacingRight == faceRight) return;
        isFacingRight = faceRight;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1);
        transform.localScale = scale;
    }

    // =========================================================
    // PLACEHOLDER VISUALS
    // =========================================================

    private void CreatePlaceholderVisuals()
    {
        // ไข่ขาว (วงรีใหญ่สีขาว — เลื่อนนิดหน่อยไปด้านหน้า)
        GameObject whiteObj = new GameObject("EggWhite_Visual");
        whiteObj.transform.SetParent(transform);
        whiteObj.transform.localPosition = new Vector3(0.05f, 0f, 0f);
        whiteObj.transform.localScale = new Vector3(1.1f, 0.8f, 1f);
        whiteRenderer = whiteObj.AddComponent<SpriteRenderer>();
        whiteRenderer.sprite = CreateCircleSprite(32);
        whiteRenderer.color = new Color(0.98f, 0.98f, 0.92f); // ขาวนวล
        whiteRenderer.sortingOrder = 1;

        // ไข่แดง — อยู่ด้าน "หน้า" ของไข่ดาว!
        // เมื่อไข่ดาว Flip ทั้งตัว (scale.x *= -1) → ไข่แดงจะย้ายฝั่งอัตโนมัติ
        // ผู้เล่นต้องอ้อมไปยิงจากด้านหน้า หรือล่อให้ไข่ดาวหันมา!
        GameObject yolkObj = new GameObject("EggYolk_Visual");
        yolkObj.transform.SetParent(transform);
        yolkObj.transform.localPosition = new Vector3(0.25f, 0f, 0f); // เลื่อนไปด้านหน้า!
        yolkObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        yolkRenderer = yolkObj.AddComponent<SpriteRenderer>();
        yolkRenderer.sprite = CreateCircleSprite(16);
        yolkRenderer.color = new Color(1f, 0.8f, 0f); // สีเหลืองไข่แดง
        yolkRenderer.sortingOrder = 2;

        if (firePoint != null && firePoint.GetComponent<SpriteRenderer>() == null)
        {
            var srGun = firePoint.gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            srGun.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            srGun.color = new Color(0.3f, 0.3f, 0.3f); // สีเทา (ปืน)
            srGun.sortingOrder = 3;
            firePoint.localScale = new Vector3(0.5f, 0.15f, 1f);
        }

        // ตา — อยู่ด้านหน้าของไข่ดาว (ถัดจากไข่แดง)
        CreateEye(new Vector3(0.15f, 0.1f, 0f));
        CreateEye(new Vector3(0.3f, 0.08f, 0f));
    }

    private void CreateEye(Vector3 localPos)
    {
        GameObject eye = new GameObject("Eye");
        eye.transform.SetParent(transform);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = Vector3.one * 0.08f;
        var sr = eye.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(8);
        sr.color = Color.black;
        sr.sortingOrder = 3;
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = dist < radius ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // ไข่แดง (Critical Zone)
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Vector3 yolkPos = yolkCenter != null ? yolkCenter.position : transform.position;
        Gizmos.DrawSphere(yolkPos, yolkRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(yolkPos, yolkRadius);

        // Detection Range
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Patrol Range
        Gizmos.color = Color.cyan;
        float startX = Application.isPlaying ? patrolStartX : transform.position.x;
        Gizmos.DrawLine(
            new Vector3(startX - patrolDistance, transform.position.y, 0),
            new Vector3(startX + patrolDistance, transform.position.y, 0)
        );
    }
}
