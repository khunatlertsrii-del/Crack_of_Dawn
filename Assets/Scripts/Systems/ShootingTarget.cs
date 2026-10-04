using UnityEngine;

/// <summary>
/// เป้าซ้อมยิง สำหรับฉาก Tutorial
/// เมื่อโดนกระสุนจะสั่นเล็กน้อยแล้วกลับมาเหมือนเดิม
/// สร้าง Visual อัตโนมัติ (วงกลมซ้อนกัน 3 ชั้น สีแดง-ขาว-แดง แบบเป้ายิงจริง)
/// </summary>
public class ShootingTarget : MonoBehaviour
{
    [Header("=== ตั้งค่า ===")]
    [Tooltip("เป้ากี่นัดถึงจะพัง? (0 = พังไม่ได้)")]
    public int hitsToBreak = 0;
    
    [Tooltip("สั่นแค่ไหนเวลาโดนยิง")]
    public float shakeAmount = 0.15f;

    private int hitCount = 0;
    private Vector3 originalPos;
    private float shakeTimer = 0f;

    private void Start()
    {
        originalPos = transform.localPosition;
        
        // สร้างภาพเป้าอัตโนมัติถ้ายังไม่มี Sprite
        if (GetComponent<SpriteRenderer>() == null && GetComponentInChildren<SpriteRenderer>() == null)
        {
            CreateTargetVisual();
        }

        // ต้องมี Collider เพื่อรับกระสุน
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
        }
    }

    private void Update()
    {
        // สั่นแล้วกลับมาตำแหน่งเดิม
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float offset = Mathf.Sin(shakeTimer * 50f) * shakeAmount * (shakeTimer / 0.3f);
            transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
            
            if (shakeTimer <= 0f)
                transform.localPosition = originalPos;
        }
    }

    /// <summary>
    /// เรียกเมื่อเป้าโดนยิง (จากกระสุนศัตรู / กระสุนผู้เล่น)
    /// </summary>
    public void OnHit()
    {
        shakeTimer = 0.3f;
        hitCount++;

        if (hitsToBreak > 0 && hitCount >= hitsToBreak)
        {
            // เป้าพัง!
            Destroy(gameObject, 0.1f);
        }
    }

    // กระสุนศัตรู (EnemyBullet) ชนเข้ามา
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<EnemyBullet>() != null)
        {
            OnHit();
            Destroy(collision.gameObject);
        }
    }

    /// <summary>
    /// สร้างภาพเป้ายิงอัตโนมัติ (วงกลมซ้อน 3 ชั้น)
    /// </summary>
    private void CreateTargetVisual()
    {
        // ชั้นนอก — วงกลมใหญ่สีขาว
        GameObject outer = new GameObject("TargetOuter");
        outer.transform.SetParent(transform);
        outer.transform.localPosition = Vector3.zero;
        SpriteRenderer outerSR = outer.AddComponent<SpriteRenderer>();
        outerSR.sprite = CreateCircleSprite();
        outerSR.color = new Color(0.9f, 0.9f, 0.85f);
        outer.transform.localScale = Vector3.one * 1f;
        outerSR.sortingOrder = 5;

        // ชั้นกลาง — วงกลมสีแดง
        GameObject mid = new GameObject("TargetMiddle");
        mid.transform.SetParent(transform);
        mid.transform.localPosition = Vector3.zero;
        SpriteRenderer midSR = mid.AddComponent<SpriteRenderer>();
        midSR.sprite = CreateCircleSprite();
        midSR.color = new Color(0.9f, 0.2f, 0.2f);
        mid.transform.localScale = Vector3.one * 0.65f;
        midSR.sortingOrder = 6;

        // ชั้นใน — วงกลมเล็กสีเหลือง (bullseye)
        GameObject inner = new GameObject("TargetBullseye");
        inner.transform.SetParent(transform);
        inner.transform.localPosition = Vector3.zero;
        SpriteRenderer innerSR = inner.AddComponent<SpriteRenderer>();
        innerSR.sprite = CreateCircleSprite();
        innerSR.color = new Color(1f, 0.85f, 0.2f);
        inner.transform.localScale = Vector3.one * 0.3f;
        innerSR.sortingOrder = 7;

        // ขาตั้ง (แท่งสีน้ำตาล)
        GameObject stand = new GameObject("TargetStand");
        stand.transform.SetParent(transform);
        stand.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        SpriteRenderer standSR = stand.AddComponent<SpriteRenderer>();
        standSR.sprite = CreateSquareSprite();
        standSR.color = new Color(0.45f, 0.3f, 0.15f);
        stand.transform.localScale = new Vector3(0.1f, 0.5f, 1f);
        standSR.sortingOrder = 4;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = dist <= 1f ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
