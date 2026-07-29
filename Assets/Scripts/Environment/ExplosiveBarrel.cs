using UnityEngine;
using System.Collections.Generic;

public class ExplosiveBarrel : MonoBehaviour, IDamageable
{
    [Header("Explosion Settings")]
    [Tooltip("รัศมีการระเบิด")]
    public float explosionRadius = 3f;
    [Tooltip("ดาเมจที่ทำกับผู้เล่น")]
    public float playerDamage = 20f;
    [Tooltip("ดาเมจที่ทำกับศัตรู (ตั้งสูงๆ จะได้ตายทันที)")]
    public float enemyDamage = 9999f;
    [Tooltip("แรงสั่นกล้องตอนระเบิด")]
    public float screenShakeIntensity = 1.5f;
    public float screenShakeDuration = 0.3f;

    [Header("Effects")]
    [Tooltip("สีของเอฟเฟกต์ระเบิด (แฟลชสว่าง)")]
    public Color explosionColor = new Color(1f, 0.5f, 0f, 0.8f);

    [Header("Explosion Gore & Blood Settings")]
    [Tooltip("จำนวนซากไข่ดาว/เลือดที่จะกระจายทั่วฉากตอนระเบิดโดนศัตรู (ยิ่งเยอะยิ่งสาดสะใจ!)")]
    public int explosionBloodCount = 50;
    [Tooltip("ความแรงในการสาดซากไข่ไปทั่วแมพ")]
    public float explosionBloodForce = 20f;
    [Tooltip("ขนาดของคราบเลือด/ซากไข่ (1.0 = ปกติ, 1.8 = ชิ้นใหญ่ 1.8 เท่า)")]
    public float explosionBloodSizeMultiplier = 1.8f;

    public bool IsAlive => true; // สมมติว่ายังไม่ระเบิด

    private bool hasExploded = false;

    public void TakeDamage(float damage, Vector2 hitPoint, bool isCritical, Vector2 hitDirection)
    {
        if (hasExploded) return;
        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // 1. สั่นกล้อง
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(screenShakeIntensity, screenShakeDuration);
        }

        // 2. เอฟเฟกต์ภาพชั่วคราว (วงกลมขยายตัว)
        SpawnExplosionEffect();

        // 3. ทำดาเมจเป็นวงกว้าง
        HashSet<FriedEggEnemy> hitEnemies = new HashSet<FriedEggEnemy>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null && damageable != (IDamageable)this)
            {
                // เช็คว่าเป็นศัตรูหรือผู้เล่น
                FriedEggEnemy enemy = hit.GetComponent<FriedEggEnemy>();
                if (enemy == null) enemy = hit.GetComponentInParent<FriedEggEnemy>();

                if (enemy != null)
                {
                    // โดนศัตรู
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    damageable.TakeDamage(enemyDamage, hit.ClosestPoint(transform.position), true, dir);

                    // สาดซากไข่ดาวและเลือดกระจายทั่วแมพสำหรับถังระเบิด!
                    if (!hitEnemies.Contains(enemy))
                    {
                        hitEnemies.Add(enemy);
                        if (HitEffect.Instance != null)
                        {
                            HitEffect.Instance.PlayExplosionGoreEffect(enemy.transform.position, explosionBloodCount, explosionBloodForce, explosionBloodSizeMultiplier);
                        }
                    }
                }
                else if (hit.GetComponent<PlayerHealth>() != null || hit.GetComponentInParent<PlayerHealth>() != null)
                {
                    // โดนผู้เล่น
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    damageable.TakeDamage(playerDamage, hit.ClosestPoint(transform.position), false, dir);
                }
            }
        }

        // 4. ลบตัวเองทิ้ง
        Destroy(gameObject);
    }

    private void SpawnExplosionEffect()
    {
        // สร้าง GameObject ใหม่ออกมาเพื่อโชว์แอนิเมชั่นขยายวงกลม
        GameObject effectObj = new GameObject("BarrelExplosionEffect");
        effectObj.transform.position = transform.position;
        
        SpriteRenderer sr = effectObj.AddComponent<SpriteRenderer>();
        // สร้าง Sprite วงกลมแบบรวดเร็ว
        Texture2D tex = CreateCircleTexture(64);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sr.color = explosionColor;
        sr.sortingOrder = 100; // ให้อยู่บนๆ

        // ใส่ Script เล็กๆ สำหรับแอนิเมท (ขยายและจาง)
        ExplosionVisual visual = effectObj.AddComponent<ExplosionVisual>();
        visual.targetScale = explosionRadius * 2f; // ให้ขยายเท่าเส้นผ่านศูนย์กลางระเบิด
    }

    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // วาดรัศมีให้เห็นใน Unity Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

// คลาสตัวช่วยในการทำแอนิเมชั่นระเบิด
public class ExplosionVisual : MonoBehaviour
{
    public float targetScale = 5f;
    public float duration = 0.2f;
    
    private SpriteRenderer sr;
    private float timer = 0f;
    private Color startColor;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        // ขยายตัว
        float currentScale = Mathf.Lerp(0f, targetScale, progress);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // ค่อยๆ โปร่งใส
        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, progress);
        sr.color = c;

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
