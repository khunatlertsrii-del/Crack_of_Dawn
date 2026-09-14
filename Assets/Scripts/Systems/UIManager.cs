// ============================================================
// UIManager.cs — HUD และ UI ของเกม
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จัดการ UI ทั้งหมด:
/// - HP Bar (แถบเลือดผู้เล่น)
/// - Slow-Mo Meter (แถบพลัง Bullet Time)
/// - Kill Count + Score
/// - Game Over / Victory Panel
/// - Crosshair (เล็ง)
/// 
/// Singleton → UIManager.Instance
/// 
/// วิธีใช้: สร้าง Canvas → ลาก UIManager ไปวาง → ทุกอย่างจะสร้างอัตโนมัติ
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("References (Auto-create if empty)")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider slowMoSlider;
    
    // Dynamic HUD (หลบขอบจอตอนเปิด Slow-Mo)
    private RectTransform hpRect;
    private RectTransform slowMoRect;
    private Vector2 hpOriginalPos;
    private Vector2 slowMoOriginalPos;
    // UI
    // (Removed Score and KillCount)
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text victoryScoreText;

    [Header("Settings")]
    [SerializeField] private Color hpBarColor = Color.white; // สีขาวทึบสไตล์คอมมิค
    [SerializeField] private Color hpBarLowColor = new Color(0.9f, 0.1f, 0.1f); // สีแดงสดตัดกับขาวดำ
    [SerializeField] private Color slowMoBarColor = new Color(0.8f, 0.8f, 0.8f); // สีเทาอ่อน
    [SerializeField] private bool autoCreateUI = true;

    [Header("Dynamic HUD Settings (หลบขอบจอตอนเปิดสโลว์)")]
    [Tooltip("ระยะที่ UI หลอดเลือดและสโลว์เลื่อนหลบขอบจอ (ค่าลบ = เลื่อนลงข้างล่าง แนะนำ -65)")]
    public float hudDodgeOffsetY = -65f;

    private Canvas canvas;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (autoCreateUI)
        {
            SetupCanvas();
            CreateUI();
        }
    }

    private void OnEnable()
    {
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.OnMeterChanged += HandleSlowMoChanged;
        }

        // Subscribe to player health
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void Start()
    {
        // Re-subscribe (in case managers initialized after us)
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.OnMeterChanged -= HandleSlowMoChanged;
            SlowMotionManager.Instance.OnMeterChanged += HandleSlowMoChanged;
        }

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        // Initialize values
        UpdateKillCount(0);
        UpdateScore(0);

        // บังคับเปลี่ยนสี UI (ในกรณีที่ Component มีการเซฟค่าสีเก่าไว้ใน Inspector)
        if (hpSlider != null)
        {
            var bg = hpSlider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = Color.black;
            
            var label = hpSlider.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.color = Color.black;
            
            // อัปเดตสีหลอดเลือดปัจจุบัน
            HandleHealthChanged(hpSlider.value);
        }

        if (slowMoSlider != null)
        {
            var bg = slowMoSlider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = Color.black;
            
            var label = slowMoSlider.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.color = Color.black;
            
            var fill = slowMoSlider.fillRect?.GetComponent<Image>();
            if (fill != null) fill.color = slowMoBarColor;
        }

        if (hpSlider != null)
        {
            hpRect = hpSlider.GetComponent<RectTransform>();
            if (hpRect != null) hpOriginalPos = hpRect.anchoredPosition;
        }
        if (slowMoSlider != null)
        {
            slowMoRect = slowMoSlider.GetComponent<RectTransform>();
            if (slowMoRect != null) slowMoOriginalPos = slowMoRect.anchoredPosition;
        }

        // Hide panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Update()
    {
        // Dynamic HUD หลบขอบจอตอนเปิด Slow-Mo หรือ Cinematic Borders
        if (SlowMotionManager.Instance != null && hpRect != null && slowMoRect != null)
        {
            bool isSlowMo = SlowMotionManager.Instance.IsSlowMotionActive;
            float hpDodgeDir = (hpRect.anchorMin.y < 0.5f || hpOriginalPos.y > 0) ? Mathf.Abs(hudDodgeOffsetY) : -Mathf.Abs(hudDodgeOffsetY);
            float slowDodgeDir = (slowMoRect.anchorMin.y < 0.5f || slowMoOriginalPos.y > 0) ? Mathf.Abs(hudDodgeOffsetY) : -Mathf.Abs(hudDodgeOffsetY);

            float hpTargetY = isSlowMo ? (hpOriginalPos.y + hpDodgeDir) : hpOriginalPos.y;
            float slowTargetY = isSlowMo ? (slowMoOriginalPos.y + slowDodgeDir) : slowMoOriginalPos.y;

            Vector2 hpPos = hpRect.anchoredPosition;
            hpPos.y = Mathf.Lerp(hpPos.y, hpTargetY, 15f * Time.unscaledDeltaTime);
            hpRect.anchoredPosition = hpPos;

            Vector2 slowPos = slowMoRect.anchoredPosition;
            slowPos.y = Mathf.Lerp(slowPos.y, slowTargetY, 15f * Time.unscaledDeltaTime);
            slowMoRect.anchoredPosition = slowPos;
        }

        // ซ่อน HUD เมื่อจบด่าน
        bool isActive = LevelRatingManager.Instance == null || LevelRatingManager.Instance.isLevelActive;
        if (hpSlider != null && hpSlider.gameObject.activeSelf != isActive)
            hpSlider.gameObject.SetActive(isActive);
        if (slowMoSlider != null && slowMoSlider.gameObject.activeSelf != isActive)
            slowMoSlider.gameObject.SetActive(isActive);
    }

    private void OnDisable()
    {
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.OnMeterChanged -= HandleSlowMoChanged;
        }
    }

    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    private void HandleHealthChanged(float percent)
    {
        if (hpSlider != null)
        {
            hpSlider.value = percent;

            // เปลี่ยนสีเมื่อ HP ต่ำ
            Image fill = hpSlider.fillRect?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = percent < 0.3f ? hpBarLowColor : hpBarColor;
            }
        }
    }

    private void HandleSlowMoChanged(float percent)
    {
        if (slowMoSlider != null)
        {
            slowMoSlider.value = percent;
        }
    }

    private void HandleEnemyKilled(int totalKills)
    {
        // UI removed
    }

    private void HandleScoreChanged(int score)
    {
        // UI removed
    }

    // =========================================================
    // UI UPDATES
    // =========================================================

    private void UpdateKillCount(int kills)
    {
        // UI removed
    }

    private void UpdateScore(int score)
    {
        // UI removed
    }

    // =========================================================
    // AUTO UI CREATION — สร้าง HUD อัตโนมัติ
    // =========================================================

    private void SetupCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void CreateUI()
    {
        // === HP Bar (มุมซ้ายบน) ===
        if (hpSlider == null)
        {
            hpSlider = CreateSlider("HP_Bar",
                new Vector2(20, -20), new Vector2(200, 20),
                hpBarColor, "HP");
        }

        // === Slow-Mo Meter (ใต้ HP Bar) ===
        if (slowMoSlider == null)
        {
            slowMoSlider = CreateSlider("SlowMo_Bar",
                new Vector2(20, -50), new Vector2(200, 15),
                slowMoBarColor, "SLOW-MO");
        }
    }

    // =========================================================
    // UI HELPERS
    // =========================================================

    private Slider CreateSlider(string name, Vector2 position, Vector2 size, Color fillColor, string label)
    {
        // Container
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = Color.black; // พื้นหลังดำสนิท
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2, 2);
        fillAreaRect.offsetMax = new Vector2(-2, -2);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.interactable = false;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(sliderObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 10;
        labelText.color = Color.black; // เปลี่ยนเป็นดำเพื่อให้อ่านง่ายบนพื้นขาว
        labelText.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(5, 0);
        labelRect.offsetMax = Vector2.zero;

        return slider;
    }

    private Text CreateText(string name, Vector2 position, string content,
        TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(canvas.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = anchor;

        // Shadow for readability
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 30);

        return text;
    }

    private GameObject CreatePanel(string name, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = bgColor;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        return panel;
    }

    private Text CreateCenterText(GameObject parent, string content)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        return text;
    }
}

/// <summary>
/// Helper: กด R เพื่อ Restart — ใส่ไว้บน Game Over / Victory Panel
/// </summary>
public class RestartHandler : MonoBehaviour
{
    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
