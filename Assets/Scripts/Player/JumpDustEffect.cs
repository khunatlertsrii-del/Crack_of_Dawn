// ============================================================
// JumpDustEffect.cs — ระบบเอฟเฟกต์ฝุ่นควันใต้เท้า (Jump & Landing Dust)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// สร้างกลุ่มควันฝุ่นใต้เท้าแบบพิกเซลอาร์ต (ไม่ต้องใช้ Prefab หรือ Sprite):
/// - เล่นตอนกระโดด (PlayJumpDust)
/// - เล่นตอนแลนดิ้งแตะพื้น (PlayLandingDust)
/// - เล่นตอน Dash / วิ่ง (PlayDashDust)
/// </summary>
public class JumpDustEffect : MonoBehaviour
{
    private static Sprite squareSprite;

    private static Sprite GetSquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
        }
        return squareSprite;
    }

    /// <summary>
    /// เอฟเฟกต์ฝุ่นตอนกระโดด (ควันพุ่งลงและออกข้าง)
    /// </summary>
    public static void PlayJumpDust(Vector2 feetPosition)
    {
        SpawnDustCloud(feetPosition, 5, new Vector2(-1.5f, -0.5f), new Vector2(1.5f, 0.2f), new Color(0.9f, 0.9f, 0.85f, 0.8f));
    }

    /// <summary>
    /// เอฟเฟกต์ฝุ่นตอนแลนดิ้งแตะพื้น (ควันกระจายออกข้างและลอยขึ้น)
    /// </summary>
    public static void PlayLandingDust(Vector2 feetPosition)
    {
        SpawnDustCloud(feetPosition, 7, new Vector2(-2.5f, 0.1f), new Vector2(2.5f, 1.2f), new Color(0.85f, 0.85f, 0.8f, 0.9f));
    }

    /// <summary>
    /// เอฟเฟกต์ฝุ่นตอน Dash / พุ่งตัว
    /// </summary>
    public static void PlayDashDust(Vector2 feetPosition, float dashDirection)
    {
        Vector2 minVel = new Vector2(-dashDirection * 1f, 0.2f);
        Vector2 maxVel = new Vector2(-dashDirection * 3f, 1.0f);
        SpawnDustCloud(feetPosition, 6, minVel, maxVel, new Color(1f, 0.95f, 0.85f, 0.9f));
    }

    private static void SpawnDustCloud(Vector2 origin, int count, Vector2 minVel, Vector2 maxVel, Color dustColor)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject dust = new GameObject("DustParticle");
            dust.transform.position = origin + new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0f, 0.1f));

            var sr = dust.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = dustColor;
            sr.sortingOrder = 5;

            float size = Random.Range(0.4f, 0.8f);
            dust.transform.localScale = new Vector3(size, size, 1f);

            var particle = dust.AddComponent<DustParticleAnim>();
            particle.Initialize(
                new Vector2(Random.Range(minVel.x, maxVel.x), Random.Range(minVel.y, maxVel.y)),
                Random.Range(0.2f, 0.35f)
            );
        }
    }
}

/// <summary>
/// แอนิเมชันของเม็ดฝุ่นแต่ละอัน: เคลื่อนที่ ลดขนาด และจางหาย
/// </summary>
public class DustParticleAnim : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;
    private float timer;
    private SpriteRenderer sr;
    private Vector3 initialScale;
    private Color initialColor;

    public void Initialize(Vector2 vel, float duration)
    {
        velocity = vel;
        lifetime = duration;
        timer = duration;
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        if (sr != null) initialColor = sr.color;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        float t = timer / lifetime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // ชะลอความเร็วลง
        velocity *= 0.92f;

        // หดเล็กลงและจางหาย
        transform.localScale = initialScale * t;
        if (sr != null)
        {
            Color c = initialColor;
            c.a = initialColor.a * (t * t);
            sr.color = c;
        }
    }
}
