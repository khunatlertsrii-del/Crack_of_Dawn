using UnityEngine;
using System.Collections;

/// <summary>
/// ระบบประตูสปอนศัตรู (แรงบันดาลใจ: My Friend Pedro)
/// เมื่อผู้เล่นเดินเข้าใกล้ ประตูจะเปิดและศัตรูจะทยอยเดินออกมา
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DoorSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("จำนวนไข่ดาวที่จะสปอนออกมา")]
    [SerializeField] private int enemyCount = 3;
    
    [Tooltip("ระยะเวลาดีเลย์ระหว่างสปอนแต่ละตัว (วินาที)")]
    [SerializeField] private float spawnDelay = 0.8f;
    
    [Tooltip("Prefab ของศัตรู (ถ้าไม่ใส่ จะพยายามหาจาก Resources หรือใช้ Default)")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("ถ้าติ๊กถูก ประตูจะไม่เปิดเองตอนเดินชน ต้องรอให้ระบบอื่น (เช่น ArenaManager) สั่งเปิด")]
    public bool requireManualTrigger = false;

    [Header("Door Visuals")]
    [Tooltip("ขนาดของประตู")]
    [SerializeField] private Vector2 doorSize = new Vector2(1f, 2f);

    [Tooltip("Sprite ประตูตอนปิด (ถ้าไม่ใส่จะใช้เป็นสีแทน)")]
    [SerializeField] private Sprite closedSprite;
    [Tooltip("Sprite ประตูตอนเปิด (ถ้าไม่ใส่จะใช้เป็นสีแทน)")]
    [SerializeField] private Sprite openSprite;

    [Tooltip("สีประตูตอนปิด")]
    [SerializeField] private Color closedColor = new Color(0.4f, 0.2f, 0.1f); // สีน้ำตาลเข้ม
    
    [Tooltip("สีประตูตอนเปิด")]
    [SerializeField] private Color openColor = new Color(0.1f, 0.1f, 0.1f); // สีดำ (มืดๆ)

    // Internal
    private bool isOpened = false;
    private SpriteRenderer doorSprite;
    private Transform spawnPoint;
    public bool IsSpawningComplete { get; private set; } = false;

    private void Awake()
    {
        // สร้าง Visual ของประตูถ้ายังไม่มี
        doorSprite = GetComponentInChildren<SpriteRenderer>();
        if (doorSprite == null)
        {
            GameObject visual = new GameObject("DoorVisual");
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(doorSize.x, doorSize.y, 1f); 
            
            doorSprite = visual.AddComponent<SpriteRenderer>();
            doorSprite.sprite = closedSprite != null ? closedSprite : CreatePlaceholderSprite();
            doorSprite.color = closedSprite != null ? Color.white : closedColor;
            doorSprite.sortingOrder = -1; // ให้อยู่หลัง Player
        }
        else
        {
            // อัปเดตสเกลกรณีที่มี Visual อยู่แล้ว
            doorSprite.transform.localScale = new Vector3(doorSize.x, doorSize.y, 1f);
            if (closedSprite != null)
            {
                doorSprite.sprite = closedSprite;
                doorSprite.color = Color.white;
            }
        }

        // ตั้งค่า Trigger Collider ให้ใหญ่พอที่ผู้เล่นจะเดินมาชน
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (col.size == Vector2.one) // ถ้าเป็นค่าเริ่มต้น ให้ขยายออกมาด้านหน้า
        {
            col.size = new Vector2(4f, 2f);
            col.offset = new Vector2(0f, 0f);
        }

        // สร้างจุด Spawn ด้านหลังประตู
        spawnPoint = transform.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            GameObject sp = new GameObject("SpawnPoint");
            sp.transform.SetParent(transform);
            sp.transform.localPosition = new Vector3(0, -0.5f, 0); // ตรงฐานประตู
            spawnPoint = sp.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isOpened) return;
        if (requireManualTrigger) return; // ถ้าให้ Arena คุม จะไม่เปิดเองเวลาเดินชน

        if (collision.CompareTag("Player"))
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// ให้ ArenaManager สั่งเปิดประตูจากภายนอก
    /// </summary>
    public void TriggerSpawner()
    {
        if (!isOpened)
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpened = true;
        if (doorSprite != null)
        {
            if (openSprite != null)
            {
                doorSprite.sprite = openSprite;
                doorSprite.color = Color.white;
            }
            else
            {
                doorSprite.color = openColor; // เปลี่ยนสีเป็นเปิด
            }
        }
        
        Debug.Log("[DoorSpawner] Door Opened! Spawning enemies...");
        StartCoroutine(SpawnEnemiesRoutine());
    }

    private IEnumerator SpawnEnemiesRoutine()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnSingleEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        IsSpawningComplete = true; // สปอนเสร็จครบทุกตัวแล้ว
    }

    private void SpawnSingleEnemy()
    {
        FriedEggEnemy eggScript = null;

        if (enemyPrefab != null)
        {
            GameObject obj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            eggScript = obj.GetComponent<FriedEggEnemy>();
        }
        else
        {
            // ถ้าไม่ได้กำหนด Prefab ให้สร้าง Empty + ใส่ Script FriedEggEnemy
            GameObject enemyObj = new GameObject("FriedEgg_FromDoor");
            enemyObj.transform.position = spawnPoint.position;
            eggScript = enemyObj.AddComponent<FriedEggEnemy>(); // Script นี้จะ auto-create visuals เอง
        }

        if (eggScript != null)
        {
            // ดูว่าประตูอยู่ทางไหน สมมติให้หันไปทางขวา (ถ้า Player อยู่ขวา ก็ควรเดินไปขวา)
            // เช็คทิศทางที่ผู้เล่นเดินเข้ามา (โดยอ้างอิงจากตำแหน่ง)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            int walkDir = -1; // เดินซ้ายปกติ
            if (player != null && player.transform.position.x > transform.position.x)
            {
                walkDir = 1; // ผู้เล่นอยู่ขวา ให้เดินออกขวา
            }

            eggScript.SpawnFromDoor(walkDir);
        }
    }

    // =========================================================
    // UTILITIES
    // =========================================================
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
}
