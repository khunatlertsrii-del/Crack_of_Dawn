// ============================================================
// Bullet.cs — กระสุนของผู้เล่น (ปืนซอสพริกศรีราชา)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// กระสุนของผู้เล่น:
/// - บินตามทิศทางที่ยิง
/// - ตรวจจับ Collision กับศัตรู (FriedEggEnemy)
/// - บอก FriedEggEnemy ว่ายิงโดนตรงไหน → ไข่แดงหรือไข่ขาว
/// - ทำลายตัวเองเมื่อชนหรือหมดอายุ
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ความเร็วกระสุน")]
    [SerializeField] private float speed = 20f;

    [Tooltip("Damage กระสุน (ใช้เมื่อโดน EggWhite)")]
    [SerializeField] private float damage = 10f;

    [Tooltip("อายุกระสุน (วินาที) — ทำลายตัวเองหลังเวลานี้")]
    [SerializeField] private float lifetime = 3f;

    [Header("Trail (Optional)")]
    [Tooltip("สร้าง Trail Effect")]
    [SerializeField] private bool enableTrail = true;

    [Tooltip("สี Trail")]
    [SerializeField] private Color trailColor = new Color(1f, 0.4f, 0.1f, 0.5f);

    // ===================== Internal =====================
    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasHit = false;
    private float timer;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        timer = lifetime;

        // Ensure trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (enableTrail)
        {
            CreateTrailEffect();
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// เรียกจาก PlayerShooting เมื่อสร้างกระสุน
    /// </summary>
    /// <param name="dir">ทิศทางการยิง (normalized)</param>
    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;

        // Rotate กระสุนตามทิศ
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // =========================================================
    // COLLISION — ตรวจจับการชน
    // =========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // ป้องกันไม่ให้กระสุนผู้เล่นยิงโดนตัวเอง (ไม่ว่าจะใช้ Box Collider หรือ Circle Collider)
        if (other.CompareTag("Player") || 
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.GetComponent<PlayerController>() != null || 
            other.GetComponentInParent<PlayerController>() != null ||
            other.GetComponent<PlayerHealth>() != null ||
            other.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        // === ชนศัตรูไข่ดาว? ===
        FriedEggEnemy enemy = other.GetComponent<FriedEggEnemy>();
        if (enemy == null) enemy = other.GetComponentInParent<FriedEggEnemy>();

        if (enemy != null && enemy.IsAlive)
        {
            hasHit = true;
            // ส่งตำแหน่งที่โดน → FriedEggEnemy จะตัดสินว่าโดนไข่แดงหรือไข่ขาว
            enemy.HandleBulletHit(transform.position, damage, direction);
            DestroyBullet();
            return;
        }

        // === ชน IDamageable อื่นๆ? ===
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null && damageable.IsAlive)
        {
            hasHit = true;
            damageable.TakeDamage(damage, transform.position, false, direction);
            DestroyBullet();
            return;
        }

        // === ชนพื้น/ผนัง/สิ่งกีดขวางทุกชนิด ===
        if (!other.isTrigger)
        {
            // ชน Collider ใดๆ ก็ตามที่ไม่ใช่ Trigger (เช่น พื้น, ผนัง, สิ่งกีดขวางทุก Layer/Tag)
            hasHit = true;
            HitEffect.Instance?.PlayImpactEffect(transform.position);
            DestroyBullet();
            return;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // ป้องกันไม่ให้กระสุนผู้เล่นยิงโดนตัวเอง
        if (collision.gameObject.CompareTag("Player") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
            collision.gameObject.GetComponent<PlayerController>() != null || 
            collision.gameObject.GetComponentInParent<PlayerController>() != null ||
            collision.gameObject.GetComponent<PlayerHealth>() != null ||
            collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable == null) damageable = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageable != null && damageable.IsAlive)
        {
            damageable.TakeDamage(damage, transform.position, false, direction);
        }

        // ชนอะไรก็ตามที่เป็น non-trigger → ทำลาย
        hasHit = true;
        SpawnImpactEffect();
        DestroyBullet();
    }

    // =========================================================
    // EFFECTS
    // =========================================================

    private void DestroyBullet()
    {
        // ปิด visual ทันที
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // ปิด collider
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // หยุดเคลื่อนที่
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // ทำลายหลัง delay เล็กน้อย (เพื่อให้ trail effect เล่นจบ)
        Destroy(gameObject, 0.1f);
    }

    private void SpawnImpactEffect()
    {
        // สร้าง particle เล็กๆ ตอนชนผนัง
        for (int i = 0; i < 3; i++)
        {
            GameObject p = new GameObject("Impact");
            p.transform.position = transform.position;

            var sr = p.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int j = 0; j < 16; j++) px[j] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
            sr.color = trailColor;
            sr.sortingOrder = 10;
            p.transform.localScale = Vector3.one * Random.Range(0.05f, 0.1f);

            var rb2d = p.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 2f;
            rb2d.AddForce(Random.insideUnitCircle * 3f, ForceMode2D.Impulse);

            Destroy(p, 0.5f);
        }
    }

    private void CreateTrailEffect()
    {
        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.2f; // ลากหางยาวขึ้นนิดนึง
        trail.startWidth = 0.15f; // ให้กระสุนดูอ้วน/สว่างขึ้น
        trail.endWidth = 0.01f;

        // คลูณสีให้สว่างทะลุหลอด (HDR Glow)
        float glowIntensity = 3f; 
        Color glowColor = new Color(trailColor.r * glowIntensity, trailColor.g * glowIntensity, trailColor.b * glowIntensity, trailColor.a);
        
        trail.startColor = glowColor;
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        
        trail.sortingOrder = 9;
        
        // ถ้าใช้ Material ปกติ จะรองรับ HDR และทำให้ Bloom ทำงานถ้ามีการตั้งค่า PostProcessing ไว้
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.numCornerVertices = 2;

        // ปรับ SpriteRenderer ของกระสุนให้สว่างขึ้นด้วย
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(sr.color.r * glowIntensity, sr.color.g * glowIntensity, sr.color.b * glowIntensity, sr.color.a);
        }
    }
}
