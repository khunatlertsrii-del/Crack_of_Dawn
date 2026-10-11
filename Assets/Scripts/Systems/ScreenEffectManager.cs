using UnityEngine;
using UnityEngine.UI;

public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance { get; private set; }

    [Header("Damage Vignette (ขอบจอแดงตอนโดนตี)")]
    [Tooltip("เปิด/ปิด จอแดงและจอเทา")]
    public bool enableDamageEffect = true;
    [Tooltip("สีของขอบจอแดงตอนโดนตี (ปรับความเข้มที่ค่า Alpha)")]
    public Color damageColor = new Color(0.8f, 0f, 0f, 0.7f);
    [Tooltip("ระยะเวลาที่จอจะแดงค้างไว้ก่อนจะค่อยๆ หายไป")]
    public float damageEffectDuration = 0.5f;

    [Header("Health & Grayscale Settings (หน้าเทา)")]
    [Tooltip("สีเทาที่จะมาบังจอเวลาโดนตี หรือ เลือดน้อย")]
    public Color grayscaleColor = new Color(0.1f, 0.1f, 0.1f, 0.85f); // เทาเข้มทึบๆ
    [Tooltip("เปิด/ปิด ให้หน้าจอเทาค้างไว้เวลาเลือดน้อย")]
    public bool enableLowHealthGray = true;
    [Tooltip("เปอร์เซ็นต์เลือดที่ถือว่าน้อย (0.0 - 1.0)")]
    public float lowHealthThreshold = 0.3f;
    
    private float currentHealthPercent = 1f;

    [Header("Slow Motion Filter (ฟิลเตอร์ตอนเปิดสโลว์)")]
    [Tooltip("เปิด/ปิด ฟิลเตอร์ตอน Slow")]
    public bool enableSlowMoFilter = true;
    [Tooltip("สีของฟิลเตอร์ตอน Slow (แนะนำให้ใช้สีเหลือง/ส้มอ่อนๆ หรือสีฟ้าซีดๆ)")]
    public Color slowMoColor = new Color(1f, 0.9f, 0.5f, 0.15f); // สีเหลืองจางๆ แบบ My Friend Pedro
    [Tooltip("ความหนาของ Vignette ตอนสโลว์และจอแดง (ยิ่งค่าน้อย ขอบยิ่งหนา ปกติคือ 3)")]
    public float vignettePower = 3f;
    [Tooltip("ความเร็วในการค่อยๆ เปลี่ยนสีฟิลเตอร์")]
    public float filterTransitionSpeed = 5f;

    [Header("Cinematic Borders 2.0 (ขอบฟิล์มภาพยนตร์บางโปร่งแสง)")]
    [Tooltip("เปิด/ปิดระบบขอบฟิล์มบาง")]
    public bool enableCinematicBorders = true;
    [Tooltip("ความสูงของขอบจอ (พิกเซล) แนะนำ 55 เพื่อไม่ให้บังทางด้านบน/ล่าง")]
    public float borderHeight = 55f;
    [Tooltip("สีและความโปร่งใสของขอบฟิล์ม (Alpha ปรับความเข้มได้ตามชอบ ค่าเริ่มต้น 0.95 คือเข้มชัด)")]
    public Color borderColor = new Color(0f, 0f, 0f, 0.95f);
    [Tooltip("ใส่รูป Texture ขอบฟิล์มของคุณเองที่นี่ (ถ้าไม่ใส่ ระบบจะสร้างลายฟิล์มทองให้เอง)")]
    public Texture customBorderTexture;
    [Tooltip("ความเร็วปกติในการเลื่อนฟิล์มตอนเปิดสโลว์")]
    public float normalBorderSpeed = 0.5f;
    [Tooltip("ความเร็วที่พุ่งสุดขีดตอนแพรี่หรือฟันดาบโดน (Adrenaline Boost)")]
    public float adrenalineBorderSpeed = 15f;
    [Tooltip("จำนวนครั้งที่ลายฟิล์มซ้ำบนหน้าจอ (ยิ่งค่าน้อย ยิ่งไม่ถี่ ปกติคือ 6)")]
    public float borderRepeatCount = 6f;

    private RawImage topBorder;
    private RawImage bottomBorder;
    private float currentBorderSpeed = 0f;
    private float adrenalineTimer = 0f;
    private Rect topUvRect = new Rect(0, 0, 20, 1);
    private Rect bottomUvRect = new Rect(0, 0, 20, 1);



    [Header("Screen Blood Settings (เลือดเปื้อนจอ)")]
    [Tooltip("ขนาดคราบเลือดบนจอ (Min/Max)")]
    public Vector2 screenBloodSizeRange = new Vector2(60f, 180f);
    [Tooltip("ความโปร่งแสงตอนแรกเริ่ม (0.0 - 1.0)")]
    public float screenBloodInitialAlpha = 0.7f;
    [Tooltip("ระยะเวลาที่เลือดค่อยๆ ไหลและจางหายไป (วินาที)")]
    public float screenBloodFadeDuration = 2f;
    [Tooltip("ความเร็วในการไหลย้อยลงมา")]
    public float screenBloodDripSpeed = 50f;
    [Tooltip("ใส่รูปเลือดที่คุณต้องการที่นี่ (ถ้าไม่ใส่ ระบบจะสร้างรูป Procedural ให้เอง)")]
    public Sprite[] customBloodSplats;



    [Header("Katana Zero Time Urgency (ขอบจอค่อยๆ แดงเมื่อใกล้หมดเวลา)")]
    [Tooltip("เปิด/ปิด ระบบจอค่อยๆ แดงเตือนเมื่อใกล้หมดเวลา")]
    public bool enableTimeUrgencyVignette = true;
    [Tooltip("สีแดงของขอบจอ")]
    public Color timeUrgencyColor = new Color(0.85f, 0.05f, 0.05f, 1f);
    [Tooltip("ความเข้มสีแดงสูงสุดตอนเหลือ 0 วินาที (Alpha: 0.0 - 1.0 เช่น 0.75)")]
    [Range(0f, 1f)] public float maxTimeUrgencyAlpha = 0.75f;
    [Tooltip("ความเร็วกระพริบหัวใจเต้นตึกตักตอนวิกฤต (0 = แดงนิ่งๆ ไม่กระพริบ)")]
    public float urgencyPulseSpeed = 4f;

    private Image damageImage;
    private Image slowMoImage;
    private Image timeUrgencyImage;
    private float currentDamageTimer = 0f;
    private Canvas screenCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // แปะ ComicTextSpawner ไว้ที่เดียวกัน จะได้ไม่ต้องลากใส่ Scene เอง
        if (GetComponent<ComicTextSpawner>() == null)
        {
            gameObject.AddComponent<ComicTextSpawner>();
        }

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
        slowMoImage.sprite = CreateVignetteSprite(); // เพิ่ม Vignette ตอนสโลว์แบบ My Friend Pedro
        SetFullScreen(slowMoImage.rectTransform);



        // 4. สร้าง Image สำหรับ Grayscale Overlay (หน้าเทา) (อยู่ใต้ Vignette)
        GameObject grayObj = new GameObject("GrayscaleOverlay");
        grayObj.transform.SetParent(canvasObj.transform, false);
        grayscaleImage = grayObj.AddComponent<Image>();
        grayscaleImage.raycastTarget = false;
        grayscaleImage.color = new Color(grayscaleColor.r, grayscaleColor.g, grayscaleColor.b, 0f);
        SetFullScreen(grayscaleImage.rectTransform);

        // 5. สร้าง Image สำหรับ Damage Vignette (ขอบจอแดง อยู่บนสุด)
        GameObject dmgObj = new GameObject("DamageVignette");
        dmgObj.transform.SetParent(canvasObj.transform, false);
        damageImage = dmgObj.AddComponent<Image>();
        damageImage.raycastTarget = false;
        damageImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        damageImage.sprite = CreateVignetteSprite(); // สร้างภาพขอบแดงไล่สี
        SetFullScreen(damageImage.rectTransform);

        // 6. สร้าง Image สำหรับ Time Urgency Vignette (ขอบจอแดงเตือนเวลา ค่อยๆ แดงขึ้นตามเวลาที่เหลือน้อย)
        GameObject urgencyObj = new GameObject("TimeUrgencyVignette");
        urgencyObj.transform.SetParent(canvasObj.transform, false);
        timeUrgencyImage = urgencyObj.AddComponent<Image>();
        timeUrgencyImage.raycastTarget = false;
        timeUrgencyImage.color = new Color(timeUrgencyColor.r, timeUrgencyColor.g, timeUrgencyColor.b, 0f);
        timeUrgencyImage.sprite = CreateVignetteSprite();
        SetFullScreen(timeUrgencyImage.rectTransform);

        CreateCinematicBorders(canvasObj);
    }

    private void CreateCinematicBorders(GameObject canvasObj)
    {
        Texture filmTex = customBorderTexture != null ? customBorderTexture : CreateFilmStripTexture(64, 32);
        filmTex.wrapMode = TextureWrapMode.Repeat;

        // Top Border (บางเฉียบติดขอบบน)
        GameObject topObj = new GameObject("TopCinematicBorder");
        topObj.transform.SetParent(canvasObj.transform, false);
        topBorder = topObj.AddComponent<RawImage>();
        topBorder.raycastTarget = false;
        topBorder.texture = filmTex;
        topBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        RectTransform rtTop = topBorder.rectTransform;
        rtTop.anchorMin = new Vector2(0, 1);
        rtTop.anchorMax = new Vector2(1, 1);
        rtTop.pivot = new Vector2(0.5f, 1);
        rtTop.sizeDelta = new Vector2(0, borderHeight);
        rtTop.anchoredPosition = Vector2.zero;

        // Bottom Border (บางเฉียบติดขอบล่าง)
        GameObject botObj = new GameObject("BottomCinematicBorder");
        botObj.transform.SetParent(canvasObj.transform, false);
        bottomBorder = botObj.AddComponent<RawImage>();
        bottomBorder.raycastTarget = false;
        bottomBorder.texture = filmTex;
        bottomBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        RectTransform rtBot = bottomBorder.rectTransform;
        rtBot.anchorMin = new Vector2(0, 0);
        rtBot.anchorMax = new Vector2(1, 0);
        rtBot.pivot = new Vector2(0.5f, 0);
        rtBot.sizeDelta = new Vector2(0, borderHeight);
        rtBot.anchoredPosition = Vector2.zero;
    }

    private Texture2D CreateFilmStripTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = Color.white;

                // รูฟิล์มสีทองสว่างชัดเจน
                if (y >= height * 0.35f && y <= height * 0.65f)
                {
                    if ((x % 16) >= 4 && (x % 16) <= 12)
                    {
                        pixels[y * width + x] = new Color(1f, 0.85f, 0.1f, 1f); // สีทองสว่าง 100%
                    }
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    private Image grayscaleImage;



    /// <summary>สร้างภาพ Vignette (ตรงกลางใส ขอบทึบ) แบบ Procedural</summary>
    private Sprite CreateVignetteSprite()
    {
        int size = 128; // เล็กๆ แล้วให้ Canvas ขยายเอา
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // หาระยะห่างจากตรงกลางจอ
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // ขอบจอจะค่าใกล้ 1 ตรงกลางใกล้ 0
                float factor = Mathf.Clamp01(dist / maxDist);
                // ยกกำลังให้ตรงกลางกว้างๆ แล้วขอบแดง/มืดจัดๆ
                factor = Mathf.Pow(factor, vignettePower);

                pixels[y * size + x] = new Color(1f, 1f, 1f, factor); // ใช้สีขาวไล่ Alpha แล้วให้ Image.color เปลี่ยนสีเอา
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
        float targetDamageAlpha = 0f;
        
        // ถ้าเลือดน้อย ให้ขอบจอแดงกระพริบตลอดเวลา
        if (enableLowHealthGray && currentHealthPercent <= lowHealthThreshold)
        {
            // กระพริบด้วย Sin wave (0 ถึง 1)
            float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f; 
            targetDamageAlpha = damageColor.a * (0.3f + pulse * 0.4f); // กระพริบระหว่าง 30% - 70% ของ Alpha สูงสุด
        }

        if (currentDamageTimer > 0)
        {
            currentDamageTimer -= Time.deltaTime; // ใช้ deltaTime ปกติ เพราะไม่อยากให้จอแดงช้าลงตอน SlowMo
            float factor = (currentDamageTimer / damageEffectDuration);
            
            // ตอนโดนตี จะเอาอันที่มากกว่าระหว่าง เลือดน้อยกระพริบ กับ ตอนเพิ่งโดนตี
            float impactAlpha = factor * damageColor.a;
            targetDamageAlpha = Mathf.Max(targetDamageAlpha, impactAlpha);
        }
        
        // อัปเดตสีขอบจอแดง
        if (damageImage != null)
        {
            Color c = damageImage.color;
            // Lerp เล็กน้อยให้สมูท
            c.a = Mathf.Lerp(c.a, targetDamageAlpha, 15f * Time.unscaledDeltaTime);
            damageImage.color = c;
        }

        // 1.5 อัปเดตขอบจอค่อยๆ แดงเมื่อใกล้หมดเวลา (Katana Zero Time Urgency)
        if (enableTimeUrgencyVignette && timeUrgencyImage != null)
        {
            float targetUrgencyAlpha = 0f;
            if (LevelRatingManager.Instance != null && LevelRatingManager.Instance.isLevelActive && LevelRatingManager.Instance.enableCountdownTimer && LevelRatingManager.Instance.IsTimerRunning)
            {
                float rem = LevelRatingManager.Instance.RemainingTime;
                float warn = LevelRatingManager.Instance.warningTimeThreshold;
                if (rem < warn && warn > 0f)
                {
                    // ค่อยๆ ไต่จาก 0.0 -> 1.0 ตามเวลาที่เหลือน้อยลงเรื่อยๆ
                    float t = 1f - Mathf.Clamp01(rem / warn);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    targetUrgencyAlpha = smoothT * maxTimeUrgencyAlpha;

                    // มีจังหวะกระพริบเต้นตุบๆ ตามความกดดัน
                    if (urgencyPulseSpeed > 0f && t > 0.2f)
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * urgencyPulseSpeed * (1f + t * 0.5f)) + 1f) * 0.5f;
                        targetUrgencyAlpha += pulse * 0.15f * t;
                        targetUrgencyAlpha = Mathf.Clamp01(targetUrgencyAlpha);
                    }
                }
            }

            Color uc = timeUrgencyColor;
            uc.a = Mathf.Lerp(timeUrgencyImage.color.a, targetUrgencyAlpha, 10f * Time.unscaledDeltaTime);
            timeUrgencyImage.color = uc;
        }

        // หน้าเทาเอาออกไปตามคำขอ แต่เผื่อไว้ให้มันค่อยๆ จางหายไปถ้าค้างอยู่
        if (grayscaleImage != null)
        {
            Color g = grayscaleImage.color;
            g.a = Mathf.Lerp(g.a, 0f, 5f * Time.deltaTime);
            grayscaleImage.color = g;
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

        // 3. อัปเดต Cinematic Borders 2.0 (ขอบฟิล์มภาพยนตร์บางโปร่งแสง)
        if (enableCinematicBorders && topBorder != null && bottomBorder != null && SlowMotionManager.Instance != null)
        {
            bool isSlowMo = SlowMotionManager.Instance.IsSlowMotionActive;
            float targetBorderAlpha = isSlowMo ? borderColor.a : 0f;

            Color curColor = topBorder.color;
            curColor.a = Mathf.Lerp(curColor.a, targetBorderAlpha, 15f * Time.unscaledDeltaTime);
            curColor.r = borderColor.r; curColor.g = borderColor.g; curColor.b = borderColor.b;
            topBorder.color = curColor;
            bottomBorder.color = curColor;

            // คำนวณความเร็ววิ่งของฟิล์ม (Adrenaline Speed Boost!)
            if (adrenalineTimer > 0)
            {
                adrenalineTimer -= Time.unscaledDeltaTime;
                currentBorderSpeed = Mathf.Lerp(currentBorderSpeed, adrenalineBorderSpeed, 20f * Time.unscaledDeltaTime);
            }
            else
            {
                currentBorderSpeed = Mathf.Lerp(currentBorderSpeed, normalBorderSpeed, 5f * Time.unscaledDeltaTime);
            }

            if (curColor.a > 0.01f)
            {
                // ขอบบนกับขอบล่างวิ่งสวนทางกันสุดเท่ (Opposite direction!)
                topUvRect.x += currentBorderSpeed * Time.unscaledDeltaTime;
                topUvRect.width = borderRepeatCount;
                topUvRect.height = 1f;

                bottomUvRect.x -= currentBorderSpeed * Time.unscaledDeltaTime;
                bottomUvRect.width = borderRepeatCount;
                bottomUvRect.height = 1f;

                topBorder.uvRect = topUvRect;
                bottomBorder.uvRect = bottomUvRect;
            }
        }
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้จาก PlayerHealth เพื่อบอกว่าเลือดเหลือกี่เปอร์เซ็นต์
    /// </summary>
    public void UpdateHealthPercent(float percent)
    {
        currentHealthPercent = percent;
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้เมื่อเกิดการปะทะ (โดนดาเมจ, แพรี่) เพื่อให้ขอบจอวิ่งเร็วขึ้น
    /// </summary>
    public void TriggerAdrenaline()
    {
        adrenalineTimer = 0.6f; // เร่งความเร็วฟิล์มสุดขีด 0.6 วินาที
        currentBorderSpeed = adrenalineBorderSpeed;
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
        
        // ใช้ Sprite รอยหยดจาก Array (ถ้ามี) หรือใช้ Procedural (ถ้าไม่มี)
        if (customBloodSplats != null && customBloodSplats.Length > 0)
        {
            img.sprite = customBloodSplats[Random.Range(0, customBloodSplats.Length)];
        }
        else
        {
            if (cachedCircleSprite == null) cachedCircleSprite = CreateSplatSprite();
            img.sprite = cachedCircleSprite;
        }
        
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
    private Sprite CreateSplatSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        
        float randomSeed = Random.Range(0f, 100f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float angle = Mathf.Atan2(y - center.y, x - center.x);
                // ใช้ PerlinNoise แบบวงกลม (Polar to Cartesian สำหรับ UV)
                float noiseX = Mathf.Cos(angle) * 2f + randomSeed;
                float noiseY = Mathf.Sin(angle) * 2f + randomSeed;
                float noise = Mathf.PerlinNoise(noiseX, noiseY);
                
                // ระยะหวังผลให้มีรอยหยัก
                float maxDist = (size * 0.3f) + (noise * size * 0.15f);
                
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // สร้างขอบเบลอๆ เล็กน้อย
                float alpha = 1f - Mathf.Clamp01((dist - maxDist) / 4f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
