// ============================================================
// HitEffect.cs — เอฟเฟกต์ตอนโดนยิง (Hitstop, Particles)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// ระบบเอฟเฟกต์เมื่อกระสุนโดนเป้า
/// - Hitstop (หยุดเกมชั่วขณะ → ความสะใจ!)
/// - Spawn Particles ณ จุดที่โดน
/// - เรียก Screen Shake จาก CameraFollow
/// 
/// ใช้เป็น Singleton — เรียก HitEffect.Instance.DoHitstop() ได้จากทุกที่
/// </summary>
public class HitEffect : MonoBehaviour
{
    public static HitEffect Instance { get; private set; }

    [Header("Hitstop Settings")]
    [Tooltip("ระยะเวลา Hitstop สำหรับ Critical Hit (วินาที)")]
    [SerializeField] private float criticalHitstopDuration = 0.08f;

    [Tooltip("ระยะเวลา Hitstop สำหรับ Normal Hit (วินาที)")]
    [SerializeField] private float normalHitstopDuration = 0.03f;

    [Header("Screen Shake Settings")]
    [SerializeField] private float criticalShakeIntensity = 0.4f;
    [SerializeField] private float criticalShakeDuration = 0.2f;
    [SerializeField] private float normalShakeIntensity = 0.1f;
    [SerializeField] private float normalShakeDuration = 0.1f;

    [Header("Parry Effect Settings")]
    [Tooltip("เปิดระบบซูมกล้องตอน Parry (ถ้ามึนหัวให้เอาติ๊กออก)")]
    [SerializeField] private bool enableParryZoom = true;
    [SerializeField] private float parryHitstopDuration = 0.15f;
    [SerializeField] private float parryShakeIntensity = 0.5f;
    [SerializeField] private float parryShakeDuration = 0.3f;
    [SerializeField] private float parryZoomAmount = 1.0f; // ปรับลงมาหน่อยเผื่อตาลาย
    [SerializeField] private float parryZoomDuration = 0.4f;

    [Header("Particle Prefabs (Optional)")]
    [Tooltip("Particle ตอนยิงโดนไข่แดง (แนะนำสีแดง)")]
    [SerializeField] private GameObject criticalHitParticlePrefab;

    [Tooltip("Particle ตอนยิงโดนไข่ขาว (แนะนำสีขาว/เหลือง)")]
    [SerializeField] private GameObject normalHitParticlePrefab;

    [Tooltip("Particle ตอนปัดกระสุน (แนะนำประกายไฟ)")]
    [SerializeField] private GameObject parryParticlePrefab;

    [Tooltip("Particle ตอนกระสุนชนกำแพง/พื้น (แนะนำฝุ่น/ควัน)")]
    [SerializeField] private GameObject impactParticlePrefab;

    [Header("Blood & Gore Settings (Procedural)")]
    [Tooltip("จำนวนเลือดเวลายิงโดนไข่แดง (Critical)")]
    [SerializeField] private int criticalBloodCount = 12;
    [Tooltip("จำนวนเลือดเวลายิงโดนไข่ขาว (Normal)")]
    [SerializeField] private int normalBloodCount = 5;
    [Tooltip("จำนวนเลือดเวลาเอาดาบ/กระทะฟาด (Melee)")]
    [SerializeField] private int meleeBloodCount = 20;
    
    [Tooltip("แรงสาดเลือด (Min/Max) เวลายิงปืน")]
    [SerializeField] private Vector2 shootBloodForce = new Vector2(5f, 15f);
    [Tooltip("แรงสาดเลือด (Min/Max) เวลาฟาดประชิด")]
    [SerializeField] private Vector2 meleeBloodForce = new Vector2(8f, 20f);
    
    [Tooltip("ความกว้างของกรวยเลือดเวลายิง (องศา) - ค่ายิ่งเยอะ ยิ่งกระจายกว้าง")]
    [SerializeField] private float shootBloodSpreadAngle = 45f;
    [Tooltip("แรงดึงดูดของเลือด (Min/Max)")]
    [SerializeField] private Vector2 bloodGravityScale = new Vector2(1f, 3f);
    [Tooltip("ขนาดของหยดเลือด (Min/Max)")]
    [SerializeField] private Vector2 bloodSizeRange = new Vector2(0.05f, 0.2f);
    
    [Tooltip("ระยะเวลาที่คราบเลือดติดกำแพง (วินาที) ก่อนจะสลายตัว")]
    [SerializeField] public float bloodDecalDuration = 20f;

    [Header("Smoke Settings (Procedural)")]
    [Tooltip("ควันเลือด (สีแดง)")]
    public Color bloodSmokeColor = new Color(0.8f, 0.1f, 0.1f, 0.6f);
    [Tooltip("ควันฝุ่นกำแพง (สีเทา)")]
    public Color impactSmokeColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    [Tooltip("ระยะเวลาที่ควันจะจางหายไป")]
    public float smokeFadeDuration = 1.5f;
    [Tooltip("ความเร็วในการขยายตัวของควัน")]
    public float smokeExpandSpeed = 2f;
    [Tooltip("ขนาดควัน (พื้นฐาน)")]
    public float smokeBaseSize = 0.5f;
    [Tooltip("ใส่รูปทรงควันเอง (เว้นว่างไว้จะใช้วงกลมเบลอๆ อัตโนมัติ)")]
    public Sprite customSmokeSprite;

    [Header("Smoke Amounts (ปรับปริมาณควัน)")]
    [Tooltip("จำนวนควันเวลายิงโดนตัว (ธรรมดา)")]
    public int gunSmokeCount = 2;
    [Tooltip("จำนวนควันเวลายิงโดนจุดอ่อน (Critical)")]
    public int gunCriticalSmokeCount = 5;
    [Tooltip("จำนวนควันเวลาฟันระยะประชิด (Melee)")]
    public int meleeSmokeCount = 3;
    [Tooltip("ขนาดควันเวลาฟันประชิด (คูณจากขนาดพื้นฐาน)")]
    public float meleeSmokeSizeMultiplier = 1.2f;

    private bool isHitstopActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // =========================================================
    // PUBLIC METHODS — เรียกจาก Bullet, EggYolk, EggWhite
    // =========================================================

    /// <summary>
    /// เล่นเอฟเฟกต์ Critical Hit (ยิงโดนไข่แดง!)
    /// Hitstop ยาวกว่า + Screen Shake แรงกว่า + Particle สีเหลืองส้ม
    /// </summary>
    public void PlayCriticalHitEffect(Vector2 hitPoint, Vector2 hitDirection)
    {
        // Hitstop
        StartCoroutine(HitstopCoroutine(criticalHitstopDuration));

        // Screen Shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(criticalShakeIntensity, criticalShakeDuration);
        }

        // Particles
        SpawnHitParticles(hitPoint, isCritical: true, hitDirection);
    }

    /// <summary>
    /// เล่นเอฟเฟกต์ Normal Hit (ยิงโดนไข่ขาว)
    /// Hitstop สั้น + Screen Shake เบา
    /// </summary>
    public void PlayNormalHitEffect(Vector2 hitPoint, Vector2 hitDirection)
    {
        StartCoroutine(HitstopCoroutine(normalHitstopDuration));

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(normalShakeIntensity, normalShakeDuration);
        }

        SpawnHitParticles(hitPoint, isCritical: false, hitDirection);
    }

    /// <summary>
    /// เล่นเอฟเฟกต์ Melee Hit (ฟันระยะประชิด)
    /// เลือดสาด 360 องศา ปริมาณเยอะสะใจ
    /// </summary>
    public void PlayMeleeHitEffect(Vector2 hitPoint)
    {
        StartCoroutine(HitstopCoroutine(criticalHitstopDuration * 2f)); // Hitstop นานกว่าปกติเพื่อความหนักแน่น

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(criticalShakeIntensity * 1.5f, criticalShakeDuration);
        }

        SpawnMeleeParticles(hitPoint);
    }

    /// <summary>
    /// เล่นเอฟเฟกต์ปัดกระสุน (Parry) สุดอลังการ!
    /// </summary>
    public void PlayParryEffect(Vector2 position)
    {
        // Hitstop รุนแรง
        StartCoroutine(HitstopCoroutine(parryHitstopDuration));

        if (CameraFollow.Instance != null)
        {
            // สั่นจอ
            CameraFollow.Instance.ShakeCamera(parryShakeIntensity, parryShakeDuration);
            
            // ซูมจอเข้า (ถ้าเปิดไว้ และต้องอยู่ในโหมด Slow-Mo เท่านั้นถึงจะซูมเพื่อความเท่!)
            bool isSlowMo = SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive;
            if (enableParryZoom && isSlowMo)
            {
                CameraFollow.Instance.ZoomCamera(parryZoomAmount, parryZoomDuration);
            }
        }

        // เสก Particle ประกายไฟ
        if (parryParticlePrefab != null)
        {
            Instantiate(parryParticlePrefab, position, Quaternion.identity);
        }
    }

    /// <summary>
    /// เล่นเอฟเฟกต์กระสุนกระทบพื้น/กำแพง
    /// </summary>
    public void PlayImpactEffect(Vector2 position)
    {
        if (impactParticlePrefab != null)
        {
            Instantiate(impactParticlePrefab, position, Quaternion.identity);
        }

        SpawnSmokeParticles(position, impactSmokeColor, 4, 1.0f);
    }

    /// <summary>
    /// เล่นเอฟเฟกต์แสงแฟลชปลายปืน
    /// </summary>
    public void PlayMuzzleFlash(Vector2 position, Vector2 direction)
    {
        if (cachedCircleSprite == null) cachedCircleSprite = CreateCircleSprite();
        
        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.position = position + direction * 0.2f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        flash.transform.rotation = Quaternion.Euler(0, 0, angle);

        var sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = cachedCircleSprite;
        sr.color = new Color(1f, 1f, 0.5f, 0.8f); // แสงเหลืองสว่าง
        sr.sortingOrder = 95;

        // รูปทรงวงรีพุ่งออกไป
        flash.transform.localScale = new Vector3(1.5f, 0.5f, 1f);

        // ทำลายตัวเองใน 0.05 วิ
        Destroy(flash, 0.05f);
    }

    // =========================================================
    // HITSTOP — หยุดเกมชั่วขณะเพื่อเพิ่มความสะใจ
    // =========================================================

    /// <summary>
    /// Hitstop: ตั้ง timeScale = 0 ชั่วขณะ แล้วค่อยคืนค่าเดิม
    /// ใช้ WaitForSecondsRealtime เพราะ Time.timeScale = 0
    /// </summary>
    private IEnumerator HitstopCoroutine(float duration)
    {
        if (isHitstopActive) yield break; // ป้องกันซ้อนกัน
        isHitstopActive = true;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        // คืนค่า timeScale (ถ้าเกมไม่ได้ Pause)
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            Time.timeScale = originalTimeScale;
        }

        isHitstopActive = false;
    }

    // =========================================================
    // PARTICLES
    // =========================================================

    private void SpawnHitParticles(Vector2 hitPoint, bool isCritical, Vector2 hitDirection)
    {
        GameObject prefab = isCritical ? criticalHitParticlePrefab : normalHitParticlePrefab;

        if (prefab != null)
        {
            GameObject particle = Instantiate(prefab, hitPoint, Quaternion.identity);
            Destroy(particle, 2f);
        }
        else
        {
            // Default: สร้าง Particle แบบง่ายๆ ถ้าไม่มี Prefab
            SpawnDefaultParticles(hitPoint, isCritical, hitDirection);
        }
    }

    /// <summary>
    /// สร้าง Particle Effect แบบ Procedural (ไม่ต้องมี Prefab)
    /// ใช้ตอนยังไม่มี Art Asset
    /// </summary>
    private void SpawnDefaultParticles(Vector2 hitPoint, bool isCritical, Vector2 hitDirection)
    {
        int particleCount = isCritical ? criticalBloodCount : normalBloodCount;
        Color particleColor = isCritical
            ? new Color(1f, 0.7f, 0.1f) // สีไข่แดง (เหลืองส้ม)
            : new Color(1f, 1f, 0.9f);  // สีไข่ขาว (ขาวนวล)

        // ทิศทางหลักคือกระเด็นตามกระสุนที่วิ่งทะลุ หรือสวนกลับ
        // (ปกติถ้ายิงโดนของเหลว มันจะมีเศษที่กระเด็นตามทางกระสุน และกระเด็นสวนทางเล็กน้อย)
        // เพื่อความชัวร์ เราจะให้มันพุ่งตาม direction ของกระสุน 
        // ถ้าต้องการให้พุ่งสวนทาง ให้ใส่ -hitDirection ตอนส่งค่ามา
        
        if (cachedCircleSprite == null) cachedCircleSprite = CreateCircleSprite();

        for (int i = 0; i < particleCount; i++)
        {
            GameObject p = new GameObject($"HitParticle_{i}");
            p.transform.position = hitPoint + Random.insideUnitCircle * 0.2f;

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = cachedCircleSprite;
            sr.color = particleColor;
            sr.sortingOrder = 100;
            p.transform.localScale = Vector3.one * Random.Range(bloodSizeRange.x, bloodSizeRange.y);

            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(bloodGravityScale.x, bloodGravityScale.y);
            
            // กระจายเป็นรูปกรวย (Cone) พุ่งตามกระสุน (hitDirection)
            // เพิ่มความกว้างของกรวยด้วยการสุ่มมุมเล็กน้อย
            float angleOffset = Random.Range(-shootBloodSpreadAngle, shootBloodSpreadAngle);
            Vector2 sprayDir = Quaternion.Euler(0, 0, angleOffset) * hitDirection;

            // เพิ่มความแรงในการสาด
            float force = Random.Range(shootBloodForce.x, shootBloodForce.y);
            if (!isCritical) force *= 0.6f; // ถ้าโดนไข่ขาวให้กระเด็นเบากว่าหน่อย

            rb.AddForce(sprayDir.normalized * force, ForceMode2D.Impulse);
            rb.angularVelocity = Random.Range(-360f, 360f);

            // ใส่ Collider เป็น Trigger เพื่อตรวจจับกำแพง (โดยไม่ผลักผู้เล่น)
            var col = p.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            // ใส่ BloodParticle สคริปต์
            p.AddComponent<BloodParticle>();

            // ปิดการชนกันเองของ Layer เดียวกัน (ถ้าเป็นไปได้ควรไปตั้งใน Project Settings > Physics 2D)
            // ชั่วคราว: ใส่ IgnoreCollision 
        }

        // เสกควันเลือด
        int smokeCount = isCritical ? gunCriticalSmokeCount : gunSmokeCount;
        SpawnSmokeParticles(hitPoint, bloodSmokeColor, smokeCount, 1.0f);
    }

    private void SpawnMeleeParticles(Vector2 hitPoint)
    {
        int particleCount = meleeBloodCount;
        Color particleColor = new Color(1f, 0.7f, 0.1f); // สีไข่แดง

        if (cachedCircleSprite == null) cachedCircleSprite = CreateCircleSprite();

        for (int i = 0; i < particleCount; i++)
        {
            GameObject p = new GameObject($"MeleeParticle_{i}");
            p.transform.position = hitPoint + Random.insideUnitCircle * 0.3f; // สุ่มจุดเกิดนิดๆ จะได้ไม่ทับกัน

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = cachedCircleSprite;
            sr.color = particleColor;
            sr.sortingOrder = 100;
            p.transform.localScale = Vector3.one * Random.Range(bloodSizeRange.x, bloodSizeRange.y * 1.5f); // ใหญ่กว่าปกตินิดนึง

            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(bloodGravityScale.x, bloodGravityScale.y);
            
            // กระจาย 360 องศา!
            Vector2 sprayDir = Random.insideUnitCircle.normalized;

            // เพิ่มความแรงสุดๆ
            float force = Random.Range(meleeBloodForce.x, meleeBloodForce.y);
            rb.AddForce(sprayDir * force, ForceMode2D.Impulse);
            rb.angularVelocity = Random.Range(-360f, 360f);

            var col = p.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            p.AddComponent<BloodParticle>();
        }

        // เสกควันเลือดก้อนใหญ่ (ปรับจำนวนได้ใน Inspector)
        SpawnSmokeParticles(hitPoint, bloodSmokeColor, meleeSmokeCount, meleeSmokeSizeMultiplier);
    }

    private void SpawnSmokeParticles(Vector2 position, Color color, int count, float sizeMultiplier)
    {
        if (cachedCircleSprite == null) cachedCircleSprite = CreateCircleSprite();
        Sprite smokeSprite = customSmokeSprite != null ? customSmokeSprite : cachedCircleSprite;

        for (int i = 0; i < count; i++)
        {
            GameObject smoke = new GameObject("SmokeParticle");
            smoke.transform.position = position + Random.insideUnitCircle * 0.3f;
            
            var sr = smoke.AddComponent<SpriteRenderer>();
            sr.sprite = smokeSprite;
            sr.color = color;
            sr.sortingOrder = 90; // อยู่ใต้เลือดที่เป็นเม็ดๆ
            
            // ทำให้ควันดูบี้ๆ ไม่กลมดิ๊ก
            float sizeX = Random.Range(smokeBaseSize * 0.7f, smokeBaseSize * 1.3f) * sizeMultiplier;
            float sizeY = Random.Range(smokeBaseSize * 0.7f, smokeBaseSize * 1.3f) * sizeMultiplier;
            smoke.transform.localScale = new Vector3(sizeX, sizeY, 1f);
            
            // สุ่มหมุน
            smoke.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            var sp = smoke.AddComponent<SmokeParticle>();
            
            // สุ่มระยะเวลาและความขยายตัวนิดหน่อยให้ดูเป็นธรรมชาติ
            float duration = smokeFadeDuration * Random.Range(0.8f, 1.2f);
            float expand = smokeExpandSpeed * Random.Range(0.8f, 1.2f);
            
            // ส่ง Velocity ให้สคริปต์ทำงานเอง (ลดแลคจาก Physics)
            Vector2 initialVelocity = Random.insideUnitCircle * Random.Range(1f, 3f);
            initialVelocity.y += 1f; // พุ่งขึ้นบนนิดนึง
            
            sp.Initialize(duration, expand, initialVelocity);
        }
    }

    private Sprite cachedCircleSprite;
    /// <summary>สร้าง Sprite วงกลมขาว (Placeholder)</summary>
    private Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(16, 16);
        Color[] pixels = new Color[16 * 16];
        Vector2 center = new Vector2(8, 8);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * 16 + x] = dist < 7 ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }
}
