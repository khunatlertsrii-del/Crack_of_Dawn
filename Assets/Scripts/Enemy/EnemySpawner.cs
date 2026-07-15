// ============================================================
// EnemySpawner.cs — ระบบ Spawn ศัตรูไข่ดาว
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// ระบบ Spawn ไข่ดาวศัตรู:
/// - Spawn ที่จุดที่กำหนด
/// - ควบคุมจำนวนสูงสุด
/// - สามารถ Spawn เป็น Wave ได้
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab ของศัตรูไข่ดาว")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("จุด Spawn (ถ้าไม่ใส่จะใช้ตำแหน่งของ Spawner)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("จำนวนศัตรูสูงสุดที่ spawn พร้อมกัน")]
    [SerializeField] private int maxEnemies = 5;

    [Tooltip("จำนวนศัตรูทั้งหมดที่จะ Spawn (-1 = ไม่จำกัด)")]
    [SerializeField] private int totalEnemies = 10;

    [Tooltip("ระยะเวลาระหว่างการ Spawn (วินาที)")]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("เริ่ม Spawn ทันทีเมื่อ Scene เริ่ม")]
    [SerializeField] private bool autoStart = true;

    [Header("Spawn Effects")]
    [Tooltip("Delay ก่อน Spawn (ให้เวลา Effect เล่น)")]
    [SerializeField] private float spawnDelay = 0.5f;

    // ===================== State =====================
    private int currentEnemyCount = 0;
    private int totalSpawned = 0;
    private bool isSpawning = false;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    /// <summary>เริ่ม Spawn ศัตรู</summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        StartCoroutine(SpawnLoop());
    }

    /// <summary>หยุด Spawn</summary>
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    // =========================================================
    // SPAWN LOOP
    // =========================================================

    private IEnumerator SpawnLoop()
    {
        // รอสักครู่ก่อนเริ่ม Spawn
        yield return new WaitForSeconds(1f);

        while (isSpawning)
        {
            // เช็คว่ายัง Spawn ได้อีกไหม
            if (totalEnemies >= 0 && totalSpawned >= totalEnemies)
            {
                isSpawning = false;
                yield break;
            }

            // เช็คจำนวนศัตรูปัจจุบัน
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // เลือกจุด Spawn
        Vector3 spawnPos;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = point.position;
        }
        else
        {
            // สุ่มตำแหน่งรอบ Spawner
            spawnPos = transform.position + new Vector3(Random.Range(-2f, 2f), 0, 0);
        }

        // สร้าง Enemy
        GameObject enemy;
        if (enemyPrefab != null)
        {
            enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // สร้าง Default Enemy ถ้าไม่มี Prefab
            enemy = CreateDefaultEnemy(spawnPos);
        }

        // นับจำนวน
        currentEnemyCount++;
        totalSpawned++;

        // ลงทะเบียน callback เมื่อศัตรูตาย
        // (ใช้ OnDestroy ของ enemy)
        var tracker = enemy.AddComponent<EnemyDestroyTracker>();
        tracker.Initialize(this);
    }

    /// <summary>เรียกเมื่อศัตรูถูกทำลาย</summary>
    public void OnEnemyDestroyed()
    {
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
    }

    // =========================================================
    // DEFAULT ENEMY (ถ้าไม่มี Prefab)
    // =========================================================

    private GameObject CreateDefaultEnemy(Vector3 position)
    {
        GameObject enemy = new GameObject("FriedEgg_Enemy");
        enemy.transform.position = position;
        enemy.tag = "Enemy";

        // Rigidbody2D
        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Collider
        var col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        // Enemy Script (จะสร้าง visual เองอัตโนมัติ)
        enemy.AddComponent<FriedEggEnemy>();

        return enemy;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

        if (spawnPoints != null)
        {
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}

/// <summary>
/// Helper component ที่ติดตาม Enemy เมื่อถูกทำลาย → แจ้ง Spawner
/// </summary>
public class EnemyDestroyTracker : MonoBehaviour
{
    private EnemySpawner spawner;

    public void Initialize(EnemySpawner owner)
    {
        spawner = owner;
    }

    private void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnEnemyDestroyed();
        }
    }
}
