using UnityEngine;

/// <summary>
/// แสงปลอม (Fake Light) แบบ Katana Zero
/// ไม่กิน GPU เลย — เป็นแค่ Sprite ที่ใช้ Shader แบบ Additive (เรืองแสง)
/// 
/// วิธีใช้: สร้าง Empty → ลากสคริปต์นี้ใส่ → เลือกรูปทรงแสง → ปรับสี/ขนาด
/// ทุกอย่างจะสร้างให้อัตโนมัติ!
/// </summary>
[ExecuteInEditMode] // ให้เห็นผลตอน Edit Mode ด้วย ไม่ต้องกด Play
public class FakeLight2D : MonoBehaviour
{
    public enum LightShape
    {
        [Tooltip("วงกลมนุ่มๆ (เหมาะกับโคมไฟ หลอดไฟ)")]
        SoftCircle,
        [Tooltip("ใช้ Sprite ที่คุณกำหนดเอง (สามเหลี่ยม รูปทรงอะไรก็ได้)")]
        CustomSprite
    }

    [Header("=== รูปทรงแสง ===")]
    public LightShape shape = LightShape.SoftCircle;
    
    [Tooltip("Sprite ที่คุณวาดเอง (ใช้เฉพาะเมื่อเลือก CustomSprite)")]
    public Sprite customSprite;

    [Header("=== สีและความเข้ม ===")]
    [Tooltip("สีของแสง (แนะนำให้ Alpha ต่ำๆ ประมาณ 0.1-0.3)")]
    public Color lightColor = new Color(1f, 0.95f, 0.8f, 0.15f); // เหลืองอ่อนจางๆ
    
    [Tooltip("ความเข้มของแสง (ยิ่งมาก ยิ่งสว่าง)")]
    [Range(0f, 3f)]
    public float intensity = 1f;

    [Header("=== กระพริบ (Optional) ===")]
    public bool flicker = false;
    [Range(0f, 10f)]
    public float flickerSpeed = 3f;
    [Range(0f, 0.5f)]
    public float flickerAmount = 0.1f;

    // Internal
    private SpriteRenderer sr;
    private Material additiveMat;
    private Texture2D softCircleTexture;

    private void OnEnable()
    {
        SetupFakeLight();
    }

    private void OnValidate()
    {
        // เมื่อปรับค่าใน Inspector จะอัปเดตทันที
        SetupFakeLight();
    }

    private void Update()
    {
        if (sr == null) return;

        Color c = lightColor * intensity;
        
        if (flicker)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            float flickerMult = 1f - (noise * flickerAmount);
            c *= flickerMult;
        }
        
        c.a = lightColor.a * intensity;
        sr.color = c;
    }

    private void SetupFakeLight()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // === สร้าง Material Additive (สิ่งที่ทำให้เรืองแสงได้!) ===
        if (additiveMat == null)
        {
            // ใช้ Shader "Sprites/Default" แล้วตั้ง Blend Mode เป็น Additive
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                additiveMat = new Material(spriteShader);
                additiveMat.name = "FakeLight_Additive";
                // Additive Blending: Source = SrcAlpha, Dest = One
                // ทำให้สีรวมกัน (สว่างขึ้น) แทนที่จะทับกัน
                additiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                additiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                additiveMat.renderQueue = 3001; // Transparent+1
            }
        }
        sr.material = additiveMat;

        // === ตั้ง Sprite ตามรูปทรงที่เลือก ===
        if (shape == LightShape.SoftCircle)
        {
            sr.sprite = CreateSoftCircleSprite();
        }
        else if (shape == LightShape.CustomSprite && customSprite != null)
        {
            sr.sprite = customSprite;
        }

        // ตั้ง Sorting Layer ให้อยู่หน้า Background
        sr.sortingOrder = 10;

        // ตั้งสี
        Color c = lightColor * intensity;
        c.a = lightColor.a * intensity;
        sr.color = c;
    }

    /// <summary>
    /// สร้าง Texture วงกลมนุ่มๆ (Radial Gradient) ขึ้นมาเอง
    /// จะได้ไม่ต้องหารูปมาใส่
    /// </summary>
    private Sprite CreateSoftCircleSprite()
    {
        if (softCircleTexture != null)
        {
            return Sprite.Create(softCircleTexture, 
                new Rect(0, 0, softCircleTexture.width, softCircleTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        int size = 128;
        softCircleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        softCircleTexture.filterMode = FilterMode.Bilinear;
        softCircleTexture.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Smooth falloff — ขอบนุ่มมาก
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // ทำให้ขอบนุ่มขึ้นอีก
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        softCircleTexture.SetPixels(pixels);
        softCircleTexture.Apply();

        return Sprite.Create(softCircleTexture, 
            new Rect(0, 0, size, size), 
            new Vector2(0.5f, 0.5f), 100f);
    }

    // =========================================================
    // GIZMOS — ให้เห็นแสงใน Scene View
    // =========================================================
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}
