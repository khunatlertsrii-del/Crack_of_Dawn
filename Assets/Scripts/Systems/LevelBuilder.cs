// ============================================================
// LevelBuilder.cs — สร้าง Test Level อัตโนมัติ
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// สร้าง Test Level แบบ Procedural สำหรับทดสอบระบบ
/// วาง Script นี้บน Empty GameObject ใน Scene
/// จะสร้าง: พื้น, แพลตฟอร์ม, ผนัง, Player, ศัตรู, กล้อง, Managers
/// 
/// === ลบ Script นี้ได้เลยเมื่อมี Level จริง ===
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private float groundWidth = 40f;
    [SerializeField] private int enemyCount = 5;

    [Header("Colors")]
    [SerializeField] private Color groundColor = new Color(0.25f, 0.2f, 0.15f);
    [SerializeField] private Color platformColor = new Color(0.35f, 0.3f, 0.2f);
    [SerializeField] private Color wallColor = new Color(0.2f, 0.18f, 0.12f);
    [SerializeField] private Color bgColor = new Color(0.08f, 0.06f, 0.12f);

    private void Start()
    {
        if (buildOnStart)
        {
            BuildLevel();
        }
    }

    public void BuildLevel()
    {
        // Set background color
        Camera.main.backgroundColor = bgColor;

        // === GROUND ===
        CreatePlatform("Ground", new Vector3(0, -3, 0), new Vector3(groundWidth, 1, 1), groundColor, true);

        // === PLATFORMS ===
        CreatePlatform("Platform_1", new Vector3(-5, -1, 0), new Vector3(4, 0.3f, 1), platformColor, true);
        CreatePlatform("Platform_2", new Vector3(3, 0, 0), new Vector3(3, 0.3f, 1), platformColor, true);
        CreatePlatform("Platform_3", new Vector3(8, 1.5f, 0), new Vector3(3, 0.3f, 1), platformColor, true);
        CreatePlatform("Platform_4", new Vector3(-2, 2.5f, 0), new Vector3(4, 0.3f, 1), platformColor, true);
        CreatePlatform("Platform_5", new Vector3(13, -1, 0), new Vector3(5, 0.3f, 1), platformColor, true);
        CreatePlatform("Platform_6", new Vector3(-8, 1, 0), new Vector3(3, 0.3f, 1), platformColor, true);

        // === WALLS (for wall jump testing) ===
        CreatePlatform("Wall_Left", new Vector3(-groundWidth / 2, 0, 0), new Vector3(1, 10, 1), wallColor, true);
        CreatePlatform("Wall_Right", new Vector3(groundWidth / 2, 0, 0), new Vector3(1, 10, 1), wallColor, true);
        CreatePlatform("Wall_Mid", new Vector3(6, 0.5f, 0), new Vector3(0.5f, 4, 1), wallColor, true);

        // === PLAYER ===
        CreatePlayer(new Vector3(-12, -1.5f, 0));

        // === ENEMIES ===
        float startX = -3f;
        for (int i = 0; i < enemyCount; i++)
        {
            float x = startX + i * 5f;
            float y = -2f;

            // วางบนแพลตฟอร์มถ้ามี
            if (i == 1) y = 0.5f;
            if (i == 2) y = 2f;
            if (i == 3) y = 3f;

            CreateEnemy($"FriedEgg_{i}", new Vector3(x, y, 0));
        }

        // === MANAGERS ===
        CreateManagers();

        Debug.Log("🍳 [LevelBuilder] Test Level สร้างเสร็จแล้ว! กด Play เพื่อทดสอบ");
    }

    // =========================================================
    // CREATE FUNCTIONS
    // =========================================================

    private void CreatePlatform(string name, Vector3 position, Vector3 scale, Color color, bool isStatic)
    {
        GameObject platform = new GameObject(name);
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.tag = "Ground";
        platform.isStatic = isStatic;

        // Visual
        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = -1;

        // Collider (non-trigger → กระสุนจะชนได้)
        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
    }

    private void CreatePlayer(Vector3 position)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = position;
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        // Visual (ไข่เจียวสี่เหลี่ยมมน)
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(1f, 0.85f, 0.2f); // สีเหลืองไข่เจียว
        sr.sortingOrder = 5;
        player.transform.localScale = new Vector3(0.7f, 0.9f, 1f);

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider
        CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1f);

        // Scripts
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerShooting>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerAnimator>();
    }

    private void CreateEnemy(string name, Vector3 position)
    {
        GameObject enemy = new GameObject(name);
        enemy.transform.position = position;
        enemy.tag = "Enemy";

        // Rigidbody (Kinematic)
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Collider
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        // Enemy Script (สร้าง visual อัตโนมัติ)
        enemy.AddComponent<FriedEggEnemy>();
    }

    private void CreateManagers()
    {
        // Level Rating Manager (แทน GameManager)
        if (LevelRatingManager.Instance == null)
        {
            GameObject lrm = new GameObject("LevelRatingManager");
            lrm.AddComponent<LevelRatingManager>();
        }

        // Slow Motion Manager
        if (SlowMotionManager.Instance == null)
        {
            GameObject sm = new GameObject("SlowMotionManager");
            sm.AddComponent<SlowMotionManager>();
        }

        // Hit Effect
        if (HitEffect.Instance == null)
        {
            GameObject he = new GameObject("HitEffect");
            he.AddComponent<HitEffect>();
        }

        // UI Manager
        if (UIManager.Instance == null)
        {
            GameObject ui = new GameObject("UI_Canvas");
            ui.AddComponent<UIManager>();
        }

        // Camera Follow
        if (CameraFollow.Instance == null)
        {
            Camera.main.gameObject.AddComponent<CameraFollow>();
        }
    }

    // =========================================================
    // SPRITE HELPER
    // =========================================================

    private Sprite CreateSquareSprite()
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
