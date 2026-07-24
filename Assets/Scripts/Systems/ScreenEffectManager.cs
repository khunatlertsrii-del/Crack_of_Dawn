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
    [Tooltip("ความเร็วในการค่อยๆ เปลี่ยนสีฟิลเตอร์")]
    public float filterTransitionSpeed = 5f;

    [Header("Cinematic Borders")]
    [Tooltip("เปิดระบบขอบจอบนล่างวิ่งเป็นฟิล์ม")]
    [SerializeField] private bool enableCinematicBorders = true;
    [Tooltip("ลาก Texture ลายขอบมาใส่ตรงนี้ (อย่าลืมตั้ง Wrap Mode เป็น Repeat)")]
    public Texture borderTexture; 
    [Tooltip("เปลี่ยนขอบจอตามเมาส์ (Dynamic)")]
    [SerializeField] private bool dynamicBorders = true;
    [SerializeField] private Color borderColor = new Color(0, 0, 0, 0.8f);
    [Tooltip("ความกว้างของขอบจอ (0.1 = 10% ของจอ)")]
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
    [Tooltip("ใส่รูปเลือดที่คุณต้องการที่นี่ (ถ้าไม่ใส่ ระบบจะสร้างรูป Procedural ให้เอง)")]
    public Sprite[] customBloodSplats;

    private RawImage topBorder;
    private RawImage bottomBorder;
    private RawImage leftBorder;
    private RawImage rightBorder;
    
    private float currentScrollSpeed = 0f;
    private float scrollPositionTop = 0f;
    private float scrollPositionBottom = 0f;
    private float scrollPositionLeft = 0f;
    private float scrollPositionRight = 0f;

    private Image damageImage;
    private Image slowMoImage;
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
        SetFullScreen(slowMoImage.rectTransform);

        // 3. สร้าง Cinematic Borders (อยู่บนสุดของ Slow-Mo แต่ใต้ Damage)
        GameObject topObj = new GameObject("TopBorder");
        topObj.transform.SetParent(canvasObj.transform, false);
        topBorder = topObj.AddComponent<RawImage>();
        topBorder.raycastTarget = false;
        topBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        if (borderTexture != null) topBorder.texture = borderTexture;
        else topBorder.texture = CreateFilmStripTexture(true);
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
        else bottomBorder.texture = CreateFilmStripTexture(false);
        RectTransform rtBot = bottomBorder.rectTransform;
        rtBot.anchorMin = new Vector2(0, 0);
        rtBot.anchorMax = new Vector2(1, borderHeightPercent);
        rtBot.offsetMin = Vector2.zero;
        rtBot.offsetMax = Vector2.zero;

        // --- Left & Right Borders (Rotate 90 degrees to use the same horizontal texture correctly) ---
        GameObject leftObj = new GameObject("LeftBorder");
        leftObj.transform.SetParent(canvasObj.transform, false);
        leftBorder = leftObj.AddComponent<RawImage>();
        leftBorder.raycastTarget = false;
        leftBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        if (borderTexture != null) leftBorder.texture = borderTexture;
        else leftBorder.texture = CreateFilmStripTexture(true); // ใช้แบบแนวนอนปกติ เพราะเราจะหมุนมัน
        RectTransform rtLeft = leftBorder.rectTransform;
        
        // หมุน -90 องศา (ขวาชี้ขึ้นบน, ล่างชี้ไปขวา)
        rtLeft.localEulerAngles = new Vector3(0, 0, -90);
        // Anchor ซ้ายกลาง
        rtLeft.anchorMin = new Vector2(0, 0.5f);
        rtLeft.anchorMax = new Vector2(0, 0.5f);
        rtLeft.pivot = new Vector2(0.5f, 0f); // แก้ Pivot เป็น 0 เพื่อให้อยู่ในจอพอดี
        // ตั้งขนาดให้ กว้าง = ความสูงจอ, สูง = ความหนาของขอบ
        rtLeft.sizeDelta = new Vector2(Screen.height, Screen.width * borderHeightPercent);
        rtLeft.anchoredPosition = Vector2.zero;

        GameObject rightObj = new GameObject("RightBorder");
        rightObj.transform.SetParent(canvasObj.transform, false);
        rightBorder = rightObj.AddComponent<RawImage>();
        rightBorder.raycastTarget = false;
        rightBorder.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        if (borderTexture != null) rightBorder.texture = borderTexture;
        else rightBorder.texture = CreateFilmStripTexture(false);
        RectTransform rtRight = rightBorder.rectTransform;

        // หมุน 90 องศา (ขวาชี้ลงล่าง, ล่างชี้ซ้าย)
        rtRight.localEulerAngles = new Vector3(0, 0, 90);
        // Anchor ขวากลาง
        rtRight.anchorMin = new Vector2(1, 0.5f);
        rtRight.anchorMax = new Vector2(1, 0.5f);
        rtRight.pivot = new Vector2(0.5f, 0f); // แก้ Pivot เป็น 0 เพื่อให้อยู่ในจอพอดี
        rtRight.sizeDelta = new Vector2(Screen.height, Screen.width * borderHeightPercent);
        rtRight.anchoredPosition = Vector2.zero;

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
    }
    
    private Image grayscaleImage;

    /// <summary>สร้าง Texture รูม้วนฟิล์ม (Film Strip)</summary>
    private Texture2D CreateFilmStripTexture(bool isTopBorder)
    {
        int width = 128;
        int height = 32;
        Texture2D tex = new Texture2D(width, height);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

        int holeWidth = 16;
        int holeHeight = 16;
        int spacing = 32;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inHoleY = isTopBorder ? (y > height - holeHeight - 4 && y < height - 4) : (y > 4 && y < holeHeight + 4);
                bool inHoleX = (x % spacing) < holeWidth;

                if (inHoleX && inHoleY)
                {
                    pixels[y * width + x] = new Color(1f, 1f, 1f, 0f); // โปร่งใส
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

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
                // ยกกำลังให้ตรงกลางกว้างๆ แล้วขอบแดงจัดๆ
                factor = Mathf.Pow(factor, 3f);

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

        // 3. อัปเดต Cinematic Borders (ขอบจอวิ่ง)
        if (enableCinematicBorders && topBorder != null && bottomBorder != null)
        {
            if (borderTexture != null)
            {
                topBorder.texture = borderTexture;
                bottomBorder.texture = borderTexture;
                leftBorder.texture = borderTexture;
                rightBorder.texture = borderTexture;
            }

            currentScrollSpeed = Mathf.Lerp(currentScrollSpeed, normalScrollSpeed, adrenalineDecaySpeed * Time.unscaledDeltaTime);

            scrollPositionTop += currentScrollSpeed * Time.unscaledDeltaTime;
            scrollPositionBottom -= currentScrollSpeed * Time.unscaledDeltaTime;
            scrollPositionLeft += currentScrollSpeed * Time.unscaledDeltaTime; // เลื่อนตามแนวที่หมุนแล้ว
            scrollPositionRight -= currentScrollSpeed * Time.unscaledDeltaTime;

            // คำนวณอัตราส่วนจอภาพเพื่อไม่ให้ Texture ยืดเกินไป
            float screenAspect = (float)Screen.width / Screen.height;
            float repeatX = 1f;
            float repeatY = 1f;

            if (borderTexture == null)
            {
                // ถ้าเป็นลาย Procedural ปกติที่ขนาด 128x32
                repeatX = Screen.width / 128f;
                repeatY = Screen.height / 128f;
            }
            else
            {
                // ถ้าใช้ Texture แบกำหนดเอง ให้คำนวณการ Tile ตาม Aspect Ratio เพื่อไม่ให้ภาพยืดอ้วน
                float texAspect = (float)borderTexture.width / borderTexture.height;
                
                // สำหรับขอบบน/ล่าง
                float pixelHeight = Screen.height * borderHeightPercent;
                float pixelWidth = pixelHeight * texAspect;
                repeatX = Screen.width / pixelWidth;

                // สำหรับขอบซ้าย/ขวา (หมุน 90 องศา)
                float vertPixelHeight = Screen.width * borderHeightPercent;
                float vertPixelWidth = vertPixelHeight * texAspect;
                repeatY = Screen.height / vertPixelWidth;
            }

            topBorder.uvRect = new Rect(scrollPositionTop, 0, repeatX, 1);
            bottomBorder.uvRect = new Rect(scrollPositionBottom, 0, repeatX, 1);
            // Left/Right หมุนแล้ว 90 องศา ความกว้างคือแกน Y ของจอ, ความสูงคือแกน X ของจอ (ขอบหนา)
            leftBorder.uvRect = new Rect(scrollPositionLeft, 0, repeatY, 1);
            rightBorder.uvRect = new Rect(scrollPositionRight, 0, repeatY, 1);

            float baseTargetAlpha = SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive ? borderColor.a : 0f;
            float topBotAlpha = baseTargetAlpha;
            float leftRightAlpha = baseTargetAlpha;

            if (dynamicBorders && baseTargetAlpha > 0)
            {
                Vector2 mousePos = Input.mousePosition;
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Vector2 dir = (mousePos - screenCenter).normalized;
                
                // ถ้าเมาส์ชี้ขึ้น/ลง (แกน Y มากกว่า X)
                if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
                {
                    topBotAlpha = 0f;
                    leftRightAlpha = baseTargetAlpha;
                }
                else // ถ้าเมาส์ชี้ซ้าย/ขวา
                {
                    topBotAlpha = baseTargetAlpha;
                    leftRightAlpha = 0f;
                }
            }
            else if (!dynamicBorders)
            {
                // ถ้าไม่เปิด Dynamic ก็มีแค่บน-ล่าง
                leftRightAlpha = 0f;
            }

            Color cTop = topBorder.color;
            Color cBot = bottomBorder.color;
            Color cLeft = leftBorder.color;
            Color cRight = rightBorder.color;
            
            cTop.a = Mathf.Lerp(cTop.a, topBotAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            cBot.a = Mathf.Lerp(cBot.a, topBotAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            cLeft.a = Mathf.Lerp(cLeft.a, leftRightAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            cRight.a = Mathf.Lerp(cRight.a, leftRightAlpha, filterTransitionSpeed * Time.unscaledDeltaTime);
            
            cTop.r = borderColor.r; cTop.g = borderColor.g; cTop.b = borderColor.b;
            cBot.r = borderColor.r; cBot.g = borderColor.g; cBot.b = borderColor.b;
            cLeft.r = borderColor.r; cLeft.g = borderColor.g; cLeft.b = borderColor.b;
            cRight.r = borderColor.r; cRight.g = borderColor.g; cRight.b = borderColor.b;

            topBorder.color = cTop;
            bottomBorder.color = cBot;
            leftBorder.color = cLeft;
            rightBorder.color = cRight;
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
