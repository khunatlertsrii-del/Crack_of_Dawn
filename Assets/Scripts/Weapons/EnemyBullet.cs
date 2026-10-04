// ============================================================
// EnemyBullet.cs — กระสุนศัตรู
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// กระสุนที่ศัตรูไข่ดาวยิงใส่ผู้เล่น:
/// - เคลื่อนที่ตรงไปตามทิศที่ยิง
/// - สร้าง Damage ให้ Player เมื่อโดน
/// - ทำลายตัวเองเมื่อชนหรือหมดอายุ
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Damage ที่ให้กับผู้เล่น")]
    [SerializeField] private float damage = 15f;

    [Tooltip("อายุกระสุน (วินาที)")]
    [SerializeField] private float lifetime = 5f;

    // ===================== Internal =====================
    private Rigidbody2D rb;
    private bool hasHit = false;
    private float timer;
    private bool isDeflected = false;
    public bool IsDeflected => isDeflected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        timer = lifetime;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        CreateTrailEffect();
    }

    private void CreateTrailEffect()
    {
        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.15f; 
        trail.startWidth = 0.12f; // อ้วนหน่อย
        trail.endWidth = 0.01f;

        // คลูณสีให้สว่างทะลุหลอด (HDR Glow สีแดงๆ เหลืองๆ สำหรับศัตรู)
        float glowIntensity = 3f; 
        Color baseColor = new Color(1f, 0.2f, 0.2f, 0.5f); // กระสุนศัตรูสีแดง
        Color glowColor = new Color(baseColor.r * glowIntensity, baseColor.g * glowIntensity, baseColor.b * glowIntensity, baseColor.a);
        
        trail.startColor = glowColor;
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        
        trail.sortingOrder = 9;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.numCornerVertices = 2;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(sr.color.r * glowIntensity, sr.color.g * glowIntensity, sr.color.b * glowIntensity, sr.color.a);
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

    /// <summary>
    /// เรียกจาก FriedEggEnemy เมื่อสร้างกระสุน
    /// </summary>
    public void Initialize(Vector2 direction, float speed)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;

        // Rotate ตามทิศ
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Deflect(Vector2 newDirection)
    {
        isDeflected = true;
        
        // สะท้อนกลับไปเร็วกว่าเดิม 2 เท่า! (WOW Factor)
        float currentSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = newDirection.normalized * currentSpeed * 2f;
        
        // หันหน้าใหม่
        float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // --- เปลี่ยนสีกระสุนและหางเป็นสีฟ้า (Cyan) ---
        float glowIntensity = 3f;
        Color cyanGlow = new Color(0f, 1f, 1f, 0.5f) * glowIntensity; // สีฟ้าเรืองแสง

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = cyanGlow;

        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.startColor = cyanGlow;
            trail.endColor = new Color(cyanGlow.r, cyanGlow.g, cyanGlow.b, 0f); // ค่อยๆ เฟด
        }

        timer = lifetime; // รีเซ็ตเวลา
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // ถ้ายิงสะท้อนกลับไปโดนศัตรู
        if (isDeflected && other.CompareTag("Enemy"))
        {
            hasHit = true;
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
            
            if (damageable != null && damageable.IsAlive)
            {
                // โดนกระสุนปัด ดาเมจแรง 3 เท่า!
                damageable.TakeDamage(damage * 3f, transform.position, true, rb.linearVelocity.normalized);
            }
            Destroy(gameObject);
            return;
        }

        // --- ป้องกันกระสุนติดกล่อง Interactable Object ---
        if (other.GetComponent<InteractableObject>() != null || other.GetComponentInParent<InteractableObject>() != null)
        {
            return; // ทะลุผ่านไปเลย ไม่ต้องสนใจ
        }

        // 1. เช็คก่อนเลย! ชน Player หรือชิ้นส่วนลูกๆ ของ Player หรือไม่? (ไม่ว่าจะตั้ง Tag ว่าอะไร)
        PlayerParry parry = other.GetComponent<PlayerParry>();
        if (parry == null) parry = other.GetComponentInParent<PlayerParry>();
        if (parry == null) parry = other.GetComponentInChildren<PlayerParry>();

        if (parry != null)
        {
            // ถ้ากำลังเปิดโหมด Parry อยู่ -> ปัดกระสุนทิ้งทันที! ห้ามทำดาเมจเด็ดขาด!
            if (parry.IsParrying)
            {
                parry.DeflectBullet(this);
                return;
            }

            // ถ้าไม่ได้เปิด Parry และกระสุนยังไม่โดนปัด -> ทำดาเมจใส่ผู้เล่น
            if (!isDeflected)
            {
                hasHit = true;
                IDamageable playerDamageable = other.GetComponent<IDamageable>();
                if (playerDamageable == null) playerDamageable = other.GetComponentInParent<IDamageable>();

                if (playerDamageable != null && playerDamageable.IsAlive)
                {
                    playerDamageable.TakeDamage(damage, transform.position, false, rb.linearVelocity.normalized);
                }
                Destroy(gameObject);
            }
            return;
        }

        // 2. ชน IDamageable อื่นๆ (ที่ไม่ใช่ศัตรู และไม่ใช่ผู้เล่น) เช่น ถังระเบิด
        if (!other.CompareTag("Enemy") && other.GetComponent<FriedEggEnemy>() == null)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null && damageable.IsAlive)
            {
                hasHit = true;
                damageable.TakeDamage(damage, transform.position, false, rb.linearVelocity.normalized);
                Destroy(gameObject);
                return;
            }
        }

        // ชนพื้น/ผนัง? (ชนอะไรก็ตามที่แข็ง เป็น solid ไม่ใช่ trigger และไม่ใช่ศัตรู/ผู้เล่น)
        if (!other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("Player"))
        {
            hasHit = true;
            HitEffect.Instance?.PlayImpactEffect(transform.position);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // เช็คก่อนเลยว่าชน Player ที่กำลัง Parry อยู่หรือไม่?
        PlayerParry parry = collision.gameObject.GetComponent<PlayerParry>();
        if (parry == null) parry = collision.gameObject.GetComponentInParent<PlayerParry>();
        if (parry == null) parry = collision.gameObject.GetComponentInChildren<PlayerParry>();

        if (parry != null && parry.IsParrying)
        {
            parry.DeflectBullet(this);
            return;
        }

        // --- ป้องกันกระสุนติดกล่อง Interactable Object (กรณีกล่องแข็ง) ---
        if (collision.gameObject.GetComponent<InteractableObject>() != null || collision.gameObject.GetComponentInParent<InteractableObject>() != null)
        {
            // ทำลายไม่ได้ เพราะต้องให้กระสุนทะลุ แต่เนื่องจากเป็น Collision มันอาจจะเด้งออกทางฟิสิกส์ไปแล้ว
            // วิธีแก้คือต้องตั้ง Layer ไว้ แต่เบื้องต้นคือป้องกันไม่ให้มัน Destroy ตัวเอง
            return;
        }

        if (!collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable == null) damageable = collision.gameObject.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage, transform.position, false, rb.linearVelocity.normalized);
            }

            hasHit = true;
            Destroy(gameObject);
        }
    }
}
