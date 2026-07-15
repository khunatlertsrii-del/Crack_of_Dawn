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
    public void PlayCriticalHitEffect(Vector2 hitPoint)
    {
        // Hitstop
        StartCoroutine(HitstopCoroutine(criticalHitstopDuration));

        // Screen Shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(criticalShakeIntensity, criticalShakeDuration);
        }

        // Particles
        SpawnHitParticles(hitPoint, isCritical: true);
    }

    /// <summary>
    /// เล่นเอฟเฟกต์ Normal Hit (ยิงโดนไข่ขาว)
    /// Hitstop สั้น + Screen Shake เบา
    /// </summary>
    public void PlayNormalHitEffect(Vector2 hitPoint)
    {
        StartCoroutine(HitstopCoroutine(normalHitstopDuration));

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(normalShakeIntensity, normalShakeDuration);
        }

        SpawnHitParticles(hitPoint, isCritical: false);
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

    private void SpawnHitParticles(Vector2 hitPoint, bool isCritical)
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
            SpawnDefaultParticles(hitPoint, isCritical);
        }
    }

    /// <summary>
    /// สร้าง Particle Effect แบบ Procedural (ไม่ต้องมี Prefab)
    /// ใช้ตอนยังไม่มี Art Asset
    /// </summary>
    private void SpawnDefaultParticles(Vector2 hitPoint, bool isCritical)
    {
        int particleCount = isCritical ? 12 : 5;
        Color particleColor = isCritical
            ? new Color(1f, 0.7f, 0.1f) // สีไข่แดง (เหลืองส้ม)
            : new Color(1f, 1f, 0.9f);  // สีไข่ขาว (ขาวนวล)

        for (int i = 0; i < particleCount; i++)
        {
            GameObject p = new GameObject($"HitParticle_{i}");
            p.transform.position = hitPoint;

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = particleColor;
            sr.sortingOrder = 100;
            p.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);

            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(1f, 3f);
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float force = isCritical ? Random.Range(5f, 12f) : Random.Range(2f, 6f);
            rb.AddForce(randomDir * force, ForceMode2D.Impulse);
            rb.angularVelocity = Random.Range(-360f, 360f);

            Destroy(p, 1.5f);
        }
    }

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
