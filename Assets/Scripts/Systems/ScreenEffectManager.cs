using UnityEngine;
using UnityEngine.UI;

public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance { get; private set; }

    [Header("Damage Vignette (ขอบจอแดงตอนโดนตี)")]
    [Tooltip("เปิด/ปิด จอแดง")]
    public bool enableDamageEffect = true;
    [Tooltip("สีของจอตอนโดนตี (ปรับความเข้มที่ค่า Alpha)")]
    public Color damageColor = new Color(1f, 0f, 0f, 0.4f);
    [Tooltip("ระยะเวลาที่จอจะแดงค้างไว้ก่อนจะค่อยๆ หายไป")]
    public float damageEffectDuration = 0.5f;

    [Header("Slow Motion Filter (ฟิลเตอร์ตอนเปิดสโลว์)")]
    [Tooltip("เปิด/ปิด ฟิลเตอร์ตอน Slow")]
    public bool enableSlowMoFilter = true;
    [Tooltip("สีของฟิลเตอร์ตอน Slow (แนะนำให้ใช้สีเหลือง/ส้มอ่อนๆ หรือสีฟ้าซีดๆ)")]
    public Color slowMoColor = new Color(1f, 0.9f, 0.5f, 0.15f); // สีเหลืองจางๆ แบบ My Friend Pedro
    [Tooltip("ความเร็วในการค่อยๆ เปลี่ยนสีฟิลเตอร์")]
    public float filterTransitionSpeed = 5f;

    [Header("Cinematic Borders (ขอบจอตอนเปิดสโลว์)")]
    public bool enableCinematicBorders = true;
    [Tooltip("ลาก Texture ลายขอบมาใส่ตรงนี้ (อย่าลืมตั้ง Wrap Mode เป็น Repeat)")]
    public Texture borderTexture; 
    [Tooltip("สีของขอบ (แนะนำสีเหลือง หรือดำ)")]
    public Color borderColor = new Color(1f, 0.8f, 0f, 1f);
    [Tooltip("ความสูงของขอบจอ (0.15 = 15% ของจอ)")]
    public float borderHeightPercent = 0.15f;
    [Tooltip("ความเร็วตอนปกติ")]
    public float normalScrollSpeed = 0.5f;
    [Tooltip("ความเร็วตอนปะทะ (Adrenaline!)")]
    public float adrenalineScrollSpeed = 3f;
    [Tooltip("ความเร็วในการกลับคืนสู่สปีดปกติ")]
    public float adrenalineDecaySpeed = 2f;

    [Header("Screen Blood Settings (เลือดเปื้อนจอ)")]
    [Tooltip("ขนาดคราบเลือดบนจอ (Min/Max)")]
    public Vector2 screenBloodSizeRange = new Vector2(60f, 180f);
    [Tooltip("ความโปร่งแสงตอนแรกเริ่ม (0.0 - 1.0)")]
    public float screenBloodInitialAlpha = 0.7f;
    [Tooltip("ระยะเวลาที่เลือดค่อยๆ ไหลและจางหายไป (วินาที)")]
    public float screenBloodFadeDuration = 2f;
    [Tooltip("ความเร็วในการไหลย้อยลงมา")]
    public float screenBloodDripSpeed = 50f;

    private RawImage topBorder;
    private RawImage bottomBorder;
    private float currentScrollSpeed;
    private float scrollPositionTop;
    private float scrollPositionBottom;

    private Image damageImage;
    private Image slowMoImage;
    private float currentDamageTimer = 0f;
    private Canvas screenCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        CreateScreenCanvas();
    }

    private void CreateScreenCanvas()
    {
        // 1. สร้าง Canvas สำหรับเอฟเฟกต์หน้าจอโดยเฉพาะ (อยู่บนสุดของทุก UI)
        GameObject canvasObj = new GameObject("ScreenEffectsCanvas");
        canvasObj.transform.SetParent(transform);
        screenCanvas = canvasObj.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 999; // ให้อยู่บนสุดเหมือนเดิม เพื่ออารมณ์ Cinematic!

        canvasObj.AddComponent<CanvasScaler>();

        // 2. สร้าง Image สำหรับ Slow Motion Filter (อยู่ข้างใต้ Damage)
        GameObject slowObj = new GameObject("SlowMoFilter");
        slowObj.transform.SetParent(canvasObj.transform, false);
        slowMoImage = slowObj.AddComponent<Image>();
        slowMoImage.raycastTarget = false;
        slowMoImage.color = new Color(slowMoColor.r, slowMoColor.g, slowMoColor.b, 0f);
        SetFullScreen(slowMoImage.rectTransform);

        // 3. สร้าง Cinematic Borders (อยู่บนสุดของ Slow-Mo แต่ใต้ Damage)
        GameObject topObj = new GameObject("TopBorder");
        topObj.transform.SetParent(canvasObj.transform, false);
        topBorder = topObj.AddComponent<RawImage>();
        topBorder.raycastTarget = false;
        topBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        if (borderTexture != null) topBorder.texture = borderTexture;
        RectTransform rtTop = topBorder.rectTransform;
        rtTop.anchorMin = new Vector2(0, 1f - borderHeightPercent);
        rtTop.anchorMax = new Vector2(1, 1);
        rtTop.offsetMin = Vector2.zero;
        rtTop.offsetMax = Vector2.zero;

        GameObject botObj = new GameObject("BottomBorder");
        botObj.transform.SetParent(canvasObj.transform, false);
        bottomBorder = botObj.AddComponent<RawImage>();
        bottomBorder.raycastTarget = false;
        bottomBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        if (borderTexture != null) bottomBorder.texture = borderTexture;
        RectTransform rtBot = bottomBorder.rectTransform;
        rtBot.anchorMin = new Vector2(0, 0);
        rtBot.anchorMax = new Vector2(1, borderHeightPercent);
        rtBot.offsetMin = Vector2.zero;
        rtBot.offsetMax = Vector2.zero;

        // 4. สร้าง Image สำหรับ Damage Vignette (อยู่ข้างบนสุด)
        GameObject dmgObj = new GameObject("DamageVignette");
        dmgObj.transform.SetParent(canvasObj.transform, false);
        damageImage = dmgObj.AddComponent<Image>();
        damageImage.raycastTarget = false;
        damageImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        SetFullScreen(damageImage.rectTransform);

        // คุณสามารถใส่ Sprite ขอบดำ/แดง (Vignette) ลงใน Image แทนสีพื้นๆ ได้ ถ้ามีการโยน Sprite เข้ามา
    }

    private void SetFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        // 1. อัปเดตขอบจอแดง
        if (currentDamageTimer > 0)
        {
            currentDamageTimer -= Time.deltaTime; // ใช้ deltaTime ปกติ เพราะไม่อยากให้จอแดงช้าลงตอน SlowMo
            float alpha = (currentDamageTimer / damageEffectDuration) * damageColor.a;
            
            Color c = damageImage.color;
            c.a = alpha;
            damageImage.color = c;
        }
        else
        {
            Color c = damageImage.color;
            c.a = 0f;
            damageImage.color = c;
        }

        // 2. อัปเดตฟิลเตอร์ Slow-Mo
        if (enableSlowMoFilter && slowMoImage != null && SlowMotionManager.Instance != null)
        {
            float targetAlpha = SlowMotionManager.Instance.IsSlowMotionActive ? slowMoColor.a : 0f;
            
            Color c = slowMoImage.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            // อัปเดตสีเผื่อผู้เล่นปรับเล่นกลางเกม
            c.r = slowMoColor.r; c.g = slowMoColor.g; c.b = slowMoColor.b;
            slowMoImage.color = c;
        }

        // 3. อัปเดต Cinematic Borders (ขอบจอวิ่ง)
        if (enableCinematicBorders && topBorder != null && bottomBorder != null)
        {
            // ดึง Texture เผื่อมีการอัปเดตจาก Inspector
            if (borderTexture != null)
            {
                topBorder.texture = borderTexture;
                bottomBorder.texture = borderTexture;
            }

            // เลื่อนความเร็วกลับเข้าสู่โหมดปกติ
            currentScrollSpeed = Mathf.Lerp(currentScrollSpeed, normalScrollSpeed, adrenalineDecaySpeed * Time.unscaledDeltaTime);

            // ขยับแกน UV
            scrollPositionTop += currentScrollSpeed * Time.unscaledDeltaTime;
            scrollPositionBottom -= currentScrollSpeed * Time.unscaledDeltaTime; // เลื่อนสวนทางกัน

            topBorder.uvRect = new Rect(scrollPositionTop, 0, 1, 1);
            bottomBorder.uvRect = new Rect(scrollPositionBottom, 0, 1, 1);

            // เฟดความชัดเมื่อเปิดสโลว์
            float targetAlpha = SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive ? borderColor.a : 0f;
            Color cTop = topBorder.color;
            Color cBot = bottomBorder.color;
            
            cTop.a = Mathf.Lerp(cTop.a, targetAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            cBot.a = Mathf.Lerp(cBot.a, targetAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            
            cTop.r = borderColor.r; cTop.g = borderColor.g; cTop.b = borderColor.b;
            cBot.r = borderColor.r; cBot.g = borderColor.g; cBot.b = borderColor.b;

            topBorder.color = cTop;
            bottomBorder.color = cBot;
        }
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้เมื่อเกิดการปะทะ (โดนดาเมจ, แพรี่) เพื่อให้ขอบจอวิ่งเร็วขึ้น
    /// </summary>
    public void TriggerAdrenaline()
    {
        if (enableCinematicBorders)
        {
            currentScrollSpeed = adrenalineScrollSpeed;
        }
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้เมื่อ Player โดนดาเมจ
    /// </summary>
    public void PlayDamageEffect()
    {
        if (enableDamageEffect)
        {
            currentDamageTimer = damageEffectDuration;
            // ตั้งค่าสีปัจจุบันให้สว่างสุดทันที
            Color c = damageColor;
            damageImage.color = c;
        }
    }

    // =========================================================
    // SCREEN BLOOD SPLATTER (เลือดสาดติดกล้อง)
    // =========================================================

    public void PlayScreenBloodSplatter(int amount = 3)
    {
        if (screenCanvas == null) return;

        for (int i = 0; i < amount; i++)
        {
            StartCoroutine(ScreenBloodCoroutine());
        }
    }

    private System.Collections.IEnumerator ScreenBloodCoroutine()
    {
        // 1. สร้าง GameObject สำหรับคราบเลือด
        GameObject bloodObj = new GameObject("ScreenBlood");
        bloodObj.transform.SetParent(screenCanvas.transform, false);

        // 2. ใส่ Image Component
        Image img = bloodObj.AddComponent<Image>();
        img.raycastTarget = false;
        
        // ใช้ Sprite วงกลม (ถ้ายังไม่มีก็สร้าง Procedural ให้เลย)
        if (cachedCircleSprite == null) cachedCircleSprite = CreateCircleSprite();
        img.sprite = cachedCircleSprite;
        
        // สุ่มสีไข่แดง (เหลือง/ส้ม)
        Color bloodColor = new Color(1f, Random.Range(0.6f, 0.9f), 0.1f, 1f);
        img.color = bloodColor;

        // 3. สุ่มตำแหน่งและขนาด (เน้นให้กระเด็นติดขอบจอ เพื่อไม่ให้บังกลางจอ)
        RectTransform rt = img.rectTransform;
        
        bool isLeft = Random.value > 0.5f;
        bool isTop = Random.value > 0.5f;
        
        float randomX = isLeft 
            ? Random.Range(-50f, Screen.width * 0.3f) 
            : Random.Range(Screen.width * 0.7f, Screen.width + 50f);
            
        float randomY = isTop 
            ? Random.Range(Screen.height * 0.7f, Screen.height + 50f) 
            : Random.Range(-50f, Screen.height * 0.3f);
            
        Vector2 screenPos = new Vector2(randomX, randomY);
        
        // ทำให้ยืดๆ บี้ๆ แบบของเหลวกระแทกเลนส์
        float size = Random.Range(screenBloodSizeRange.x, screenBloodSizeRange.y);
        img.color = new Color(bloodColor.r, bloodColor.g, bloodColor.b, screenBloodInitialAlpha);
        rt.sizeDelta = new Vector2(size * Random.Range(0.8f, 1.2f), size * Random.Range(0.8f, 1.2f));
        
        // ตำแหน่ง Anchor ที่มุมซ้ายล่าง เพื่อให้อิงจาก pixel (Screen Position) ได้ง่ายๆ
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = screenPos;
        
        // สุ่มหมุน
        rt.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // 4. แสดงค้างไว้แป๊บนึง
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        // 5. ค่อยๆ Fade Out (ไหลย้อยลงมานิดๆ)
        float fadeTimer = screenBloodFadeDuration;
        while (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            float alpha = (fadeTimer / screenBloodFadeDuration) * screenBloodInitialAlpha;
            img.color = new Color(bloodColor.r, bloodColor.g, bloodColor.b, alpha);
            
            // ไหลลงช้าๆ
            rt.anchoredPosition -= new Vector2(0, screenBloodDripSpeed * Time.deltaTime);
            
            yield return null;
        }

        // 6. ทำลายทิ้ง
        Destroy(bloodObj);
    }

    private Sprite cachedCircleSprite;
    private Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        Vector2 center = new Vector2(16, 16);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // สร้างขอบเบลอๆ เล็กน้อยให้ดูเหมือนของเหลว
                float alpha = 1f - Mathf.Clamp01((dist - 12f) / 4f);
                pixels[y * 32 + x] = new Color(1, 1, 1, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
    }
}
