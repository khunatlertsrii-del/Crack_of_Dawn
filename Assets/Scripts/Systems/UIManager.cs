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
    [SerializeField] private Text killCountText;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text victoryScoreText;

    [Header("Settings")]
    [SerializeField] private Color hpBarColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color hpBarLowColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color slowMoBarColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private bool autoCreateUI = true;

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
        // Subscribe to events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnEnemyKilled += HandleEnemyKilled;
            GameManager.Instance.OnScoreChanged += HandleScoreChanged;
        }

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
            GameManager.Instance.OnEnemyKilled += HandleEnemyKilled;
            GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
            GameManager.Instance.OnScoreChanged += HandleScoreChanged;
        }

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

        // Hide panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Update()
    {

        // Dynamic HUD หลบขอบจอ Cinematic
        if (SlowMotionManager.Instance != null)
        {
            // เซฟค่าเริ่มต้นถ้าเพิ่งเคยเจอ Slider
            if (hpSlider != null && hpRect == null)
            {
                hpRect = hpSlider.GetComponent<RectTransform>();
                if (hpRect != null) hpOriginalPos = hpRect.anchoredPosition;
            }
            if (slowMoSlider != null && slowMoRect == null)
            {
                slowMoRect = slowMoSlider.GetComponent<RectTransform>();
                if (slowMoRect != null) slowMoOriginalPos = slowMoRect.anchoredPosition;
            }

            // ขยับ HP Slider
            if (hpRect != null)
            {
                float direction = (hpRect.anchorMin.y >= 0.5f) ? -1f : 1f;
                float targetY = SlowMotionManager.Instance.IsSlowMotionActive ? hpOriginalPos.y + (150f * direction) : hpOriginalPos.y;
                Vector2 targetPos = new Vector2(hpOriginalPos.x, targetY);
                hpRect.anchoredPosition = Vector2.Lerp(hpRect.anchoredPosition, targetPos, 10f * Time.unscaledDeltaTime);
            }

            // ขยับ SlowMo Slider
            if (slowMoRect != null)
            {
                float direction = (slowMoRect.anchorMin.y >= 0.5f) ? -1f : 1f;
                float targetY = SlowMotionManager.Instance.IsSlowMotionActive ? slowMoOriginalPos.y + (150f * direction) : slowMoOriginalPos.y;
                Vector2 targetPos = new Vector2(slowMoOriginalPos.x, targetY);
                slowMoRect.anchoredPosition = Vector2.Lerp(slowMoRect.anchoredPosition, targetPos, 10f * Time.unscaledDeltaTime);
            }
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
            GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }

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
        UpdateKillCount(totalKills);
    }

    private void HandleScoreChanged(int score)
    {
        UpdateScore(score);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.GameOver:
                ShowGameOver();
                break;
            case GameManager.GameState.Victory:
                ShowVictory();
                break;
        }
    }

    // =========================================================
    // UI UPDATES
    // =========================================================

    private void UpdateKillCount(int kills)
    {
        if (killCountText != null)
            killCountText.text = $"KILLS: {kills}";
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score:N0}";
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null && GameManager.Instance != null)
            {
                gameOverScoreText.text = $"SCORE: {GameManager.Instance.Score:N0}\n" +
                                         $"KILLS: {GameManager.Instance.TotalKills}\n" +
                                         $"CRITICAL: {GameManager.Instance.CriticalKills}\n\n" +
                                         $"Press R to Restart";
            }
        }
    }

    private void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryScoreText != null && GameManager.Instance != null)
            {
                victoryScoreText.text = $"SCORE: {GameManager.Instance.Score:N0}\n" +
                                        $"KILLS: {GameManager.Instance.TotalKills}\n" +
                                        $"CRITICAL: {GameManager.Instance.CriticalKills}\n\n" +
                                        $"OMELET RUSH!";
            }
        }
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

        // === Kill Count (มุมขวาบน) ===
        if (killCountText == null)
        {
            killCountText = CreateText("KillCount",
                new Vector2(-20, -20), "KILLS: 0",
                TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1));
        }

        // === Score (ใต้ Kill Count) ===
        if (scoreText == null)
        {
            scoreText = CreateText("Score",
                new Vector2(-20, -45), "SCORE: 0",
                TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1));
        }

        // === Game Over Panel ===
        if (gameOverPanel == null)
        {
            gameOverPanel = CreatePanel("GameOver_Panel",
                new Color(0.1f, 0.05f, 0.05f, 0.85f));
            gameOverScoreText = CreateCenterText(gameOverPanel, "GAME OVER\n\nPress R to Restart");
            var restartHandler = gameOverPanel.AddComponent<RestartHandler>();
            gameOverPanel.SetActive(false);
        }

        // === Victory Panel ===
        if (victoryPanel == null)
        {
            victoryPanel = CreatePanel("Victory_Panel",
                new Color(0.05f, 0.1f, 0.05f, 0.85f));
            victoryScoreText = CreateCenterText(victoryPanel, "VICTORY!\n\nOMELET RUSH!");
            var restartHandler = victoryPanel.AddComponent<RestartHandler>();
            victoryPanel.SetActive(false);
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
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
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
        labelText.color = Color.white;
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }
    }
}
