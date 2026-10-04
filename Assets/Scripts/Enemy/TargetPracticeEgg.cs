using UnityEngine;
using System.Collections;

/// <summary>
/// ไข่ดาวซ้อมยิงเป้า — ยืนยิงเป้าซ้อมยิงเฉยๆ ไม่สนใจผู้เล่น
/// ใช้ในฉาก Tutorial ให้ผู้เล่นเห็นจากทางเดินด้านล่าง
/// 
/// ★ ถูกผู้เล่นยิงตายได้เหมือน FriedEggEnemy ปกติ
/// ★ ใช้ FriedEggEnemy ตัวเดิม แค่ห่อหุ้ม behavior ใหม่ให้ยิงเป้าแทน
/// </summary>
public class TargetPracticeEgg : MonoBehaviour
{
    [Header("=== เป้ายิง ===")]
    [Tooltip("Transform ของเป้าที่จะยิงใส่ (ลาก ShootingTarget มาใส่)")]
    public Transform targetPoint;

    [Header("=== การยิง ===")]
    [Tooltip("Prefab กระสุนศัตรู")]
    public GameObject enemyBulletPrefab;
    
    [Tooltip("จุดยิงกระสุน")]
    public Transform firePoint;
    
    [Tooltip("จุดหมุนปืน")]
    public Transform gunPivot;
    
    [Tooltip("จุดหมุนหัว")]
    public Transform headPivot;

    [Tooltip("ความเร็วกระสุน")]
    public float bulletSpeed = 20f;

    [Tooltip("ระยะห่างระหว่างนัด (วินาที)")]
    public float fireInterval = 1.5f;
    
    [Tooltip("ความแม่นยำ (องศากระจาย)")]
    public float accuracySpread = 5f;

    private float fireTimer;
    private bool isFacingRight = true;

    private void Start()
    {
        fireTimer = fireInterval;

        // สร้าง firePoint อัตโนมัติถ้าไม่ได้ลากมา
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            firePoint = fp.transform;
        }

        // หันหน้าไปหาเป้า
        if (targetPoint != null)
        {
            FaceTarget();
            AimAtTarget();
        }
    }

    private void Update()
    {
        if (targetPoint == null) return;

        // เช็คว่า FriedEggEnemy (ถ้ามี) ยังมีชีวิตอยู่ไหม
        FriedEggEnemy egg = GetComponent<FriedEggEnemy>();
        if (egg != null && !egg.IsAlive) return;

        // หันหน้าและเล็งไปที่เป้าตลอด
        FaceTarget();
        AimAtTarget();

        // ยิงตามรอบ
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            Fire();
            fireTimer = fireInterval + Random.Range(-0.3f, 0.3f); // สุ่มนิดหน่อยให้ดูเป็นธรรมชาติ
        }
    }

    private void FaceTarget()
    {
        bool shouldFaceRight = targetPoint.position.x > transform.position.x;
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
            transform.localScale = scale;
        }
    }

    private void AimAtTarget()
    {
        Vector2 direction = ((Vector2)targetPoint.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // ถ้าหันซ้ายต้องบวก 180 องศา
        if (transform.localScale.x < 0)
            angle += 180f;

        if (gunPivot != null)
            gunPivot.rotation = Quaternion.Euler(0, 0, angle);

        if (headPivot != null)
        {
            float headAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
            headAngle = Mathf.Clamp(headAngle, -25f, 25f);
            headPivot.localRotation = Quaternion.Euler(0, 0, headAngle);
        }
    }

    private void Fire()
    {
        if (firePoint == null || targetPoint == null) return;

        Vector2 direction = ((Vector2)targetPoint.position - (Vector2)firePoint.position).normalized;

        // กระจายกระสุนนิดหน่อย
        float spread = Random.Range(-accuracySpread, accuracySpread);
        direction = Quaternion.Euler(0, 0, spread) * direction;

        if (enemyBulletPrefab != null)
        {
            GameObject bullet = Instantiate(enemyBulletPrefab, firePoint.position, Quaternion.identity);
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null) eb.Initialize(direction, bulletSpeed);
        }

        // เอฟเฟกต์แสงไฟปลายปืน
        if (HitEffect.Instance != null)
            HitEffect.Instance.PlayMuzzleFlash(firePoint.position, direction);
    }
}
