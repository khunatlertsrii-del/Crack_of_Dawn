// ============================================================
// UIManager.cs — HUD และ UI ของเกม
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Segmented Health Blocks (ระบบช่องเลือดทีละขีด)")]
    [Tooltip("ลาก GameObject ช่องเลือดแต่ละช่องมาใส่ตรงนี้ (เช่น 3 ช่องตามในภาพ) โดนยิงจะปิดทีละช่อง")]
    [SerializeField] private GameObject[] healthBlocks;

    [Header("Katana Zero Time Bar HUD (Auto-create if empty)")]
    [SerializeField] private GameObject timerContainer;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private RectTransform timerRect;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text timerPenaltyText;
    [Tooltip("สีข้อความตัวเลขเวลาปกติ (ค่าเริ่มต้น สีดำ)")]
    [SerializeField] private Color timerTextColor = Color.black;
    [Tooltip("สีหลอดเวลาตอนเต็ม (ค่าเริ่มต้น สีเหลืองสด)")]
    [SerializeField] private Color timeBarFullColor = new Color(1f, 0.9f, 0.1f);
    [Tooltip("สีหลอดเวลาตอนปานกลาง (ค่าเริ่มต้น สีส้ม)")]
    [SerializeField] private Color timeBarMidColor = new Color(1f, 0.5f, 0f);
    [Tooltip("สีหลอดเวลาตอนใกล้หมด (ค่าเริ่มต้น สีแดงสด)")]
    [SerializeField] private Color timeBarLowColor = new Color(0.95f, 0.15f, 0.15f);
    private Coroutine penaltyCoroutine;
    
    // Dynamic HUD (หลบขอบจอตอนเปิด Slow-Mo)
    private RectTransform hpRect;
    private RectTransform slowMoRect;
    private Vector2 hpOriginalPos;
    private Vector2 slowMoOriginalPos;
    private Vector2 timerOriginalPos;
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

        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.OnTimePenaltyApplied += HandleTimePenalty;
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

        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.OnTimePenaltyApplied -= HandleTimePenalty;
            LevelRatingManager.Instance.OnTimePenaltyApplied += HandleTimePenalty;
        }

        // Initialize values
        UpdateKillCount(0);
        UpdateScore(0);

        if (hpSlider != null)
        {
            // อัปเดตสถานะเลือดเริ่มต้น
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
        if (timerRect == null)
        {
            if (timerContainer != null)
                timerRect = timerContainer.GetComponent<RectTransform>();
            else if (timerSlider != null)
                timerRect = timerSlider.GetComponent<RectTransform>();
        }
        if (timerRect != null)
        {
            timerOriginalPos = timerRect.anchoredPosition;
        }

        // Hide panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Update()
    {
        // ตรวจสอบสถานะเกม: ถ้าจบด่านหรือ Game Over ให้ซ่อน Gameplay HUD ทั้งหมดทันที
        bool isActive = LevelRatingManager.Instance == null || LevelRatingManager.Instance.isLevelActive;
        if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameManager.GameState.GameOver || GameManager.Instance.CurrentState == GameManager.GameState.Victory))
        {
            isActive = false;
        }

        if (!isActive)
        {
            HideGameplayHUD();
            return;
        }

        // เมื่ออยู่ในเกมเพลย์ปกติ แสดงองค์ประกอบ HUD ตามสถานะ
        if (hpSlider != null && !hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(true);
        if (slowMoSlider != null && !slowMoSlider.gameObject.activeSelf) slowMoSlider.gameObject.SetActive(true);
        bool showTimer = !isTimerHiddenByTrigger;
        if (timerContainer != null && timerContainer.activeSelf != showTimer) timerContainer.SetActive(showTimer);
        if (timerSlider != null && timerSlider.gameObject.activeSelf != showTimer) timerSlider.gameObject.SetActive(showTimer);
        if (timerRect != null && timerRect.gameObject.activeSelf != showTimer) timerRect.gameObject.SetActive(showTimer);

        // Dynamic HUD (หลบขอบจอตอนเปิด Slow-Mo หรือ Cinematic Borders)
        if (SlowMotionManager.Instance != null)
        {
            bool isSlowMo = SlowMotionManager.Instance.IsSlowMotionActive;

            if (hpRect != null)
            {
                float hpDodgeDir = (hpRect.anchorMin.y < 0.5f || hpOriginalPos.y > 0) ? Mathf.Abs(hudDodgeOffsetY) : -Mathf.Abs(hudDodgeOffsetY);
                float hpTargetY = isSlowMo ? (hpOriginalPos.y + hpDodgeDir) : hpOriginalPos.y;
                Vector2 hpPos = hpRect.anchoredPosition;
                hpPos.y = Mathf.Lerp(hpPos.y, hpTargetY, 15f * Time.unscaledDeltaTime);
                hpRect.anchoredPosition = hpPos;
            }

            if (slowMoRect != null)
            {
                float slowDodgeDir = (slowMoRect.anchorMin.y < 0.5f || slowMoOriginalPos.y > 0) ? Mathf.Abs(hudDodgeOffsetY) : -Mathf.Abs(hudDodgeOffsetY);
                float slowTargetY = isSlowMo ? (slowMoOriginalPos.y + slowDodgeDir) : slowMoOriginalPos.y;
                Vector2 slowPos = slowMoRect.anchoredPosition;
                slowPos.y = Mathf.Lerp(slowPos.y, slowTargetY, 15f * Time.unscaledDeltaTime);
                slowMoRect.anchoredPosition = slowPos;
            }

            if (timerRect != null)
            {
                float timerDodgeDir = (timerRect.anchorMin.y < 0.5f || timerOriginalPos.y > 0) ? Mathf.Abs(hudDodgeOffsetY) : -Mathf.Abs(hudDodgeOffsetY);
                float timerTargetY = isSlowMo ? (timerOriginalPos.y + timerDodgeDir) : timerOriginalPos.y;
                Vector2 timerPos = timerRect.anchoredPosition;
                timerPos.y = Mathf.Lerp(timerPos.y, timerTargetY, 15f * Time.unscaledDeltaTime);
                timerRect.anchoredPosition = timerPos;
            }
        }

        // อัปเดต Katana Zero Countdown Timer & หลอดเวลา (Katana Zero Time Bar)
        if (LevelRatingManager.Instance != null && LevelRatingManager.Instance.enableCountdownTimer)
        {
            float t = LevelRatingManager.Instance.RemainingTime;
            float maxTime = LevelRatingManager.Instance.levelTimeLimit;
            float timePercent = maxTime > 0f ? Mathf.Clamp01(t / maxTime) : 0f;

            int minutes = Mathf.FloorToInt(t / 60F);
            int seconds = Mathf.FloorToInt(t % 60F);

            if (timerText != null)
            {
                timerText.text = t > 0f ? string.Format("{0:00}:{1:00}", minutes, seconds) : "00:00";
            }

            // ปรับขนาดหลอดเวลาและไล่สี (เหลือง -> ส้ม -> แดง)
            if (timerSlider != null)
            {
                timerSlider.value = timePercent; // Slider normalized 0-1
                Image fillImg = timerSlider.fillRect != null ? timerSlider.fillRect.GetComponent<Image>() : null;
                if (fillImg == null)
                {
                    var fillChild = timerSlider.transform.Find("Fill Area/Fill");
                    if (fillChild != null) fillImg = fillChild.GetComponent<Image>();
                }
                if (fillImg != null)
                {
                    Color currentColor;
                    if (timePercent >= 0.5f)
                    {
                        float subT = (timePercent - 0.5f) / 0.5f;
                        currentColor = Color.Lerp(timeBarMidColor, timeBarFullColor, subT);
                    }
                    else
                    {
                        float subT = timePercent / 0.5f;
                        currentColor = Color.Lerp(timeBarLowColor, timeBarMidColor, subT);
                    }
                    fillImg.color = currentColor;
                }
            }

            // กระพริบสีเตือนเมื่อเวลาเหลือน้อยกว่า warningTimeThreshold ที่ตั้งไว้
            float warnTime = LevelRatingManager.Instance.warningTimeThreshold;
            if (timerText != null)
            {
                if (t <= warnTime && t > 0f)
                {
                    float pulse = Mathf.PingPong(Time.unscaledTime * 6f, 1f);
                    timerText.color = Color.Lerp(Color.red, Color.yellow, pulse);
                }
                else if (t <= 0f)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = timerTextColor;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.OnMeterChanged -= HandleSlowMoChanged;
        }

        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.OnTimePenaltyApplied -= HandleTimePenalty;
        }
    }

    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    private void HandleTimePenalty(float penaltySeconds)
    {
        if (timerPenaltyText != null)
        {
            if (penaltyCoroutine != null) StopCoroutine(penaltyCoroutine);
            penaltyCoroutine = StartCoroutine(ShowPenaltyPopupRoutine(penaltySeconds));
        }
    }

    private System.Collections.IEnumerator ShowPenaltyPopupRoutine(float penalty)
    {
        timerPenaltyText.text = $"-{penalty:0}s";
        timerPenaltyText.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = 0.8f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            timerPenaltyText.color = new Color(1f, 0.2f, 0.2f, alpha);
            yield return null;
        }

        timerPenaltyText.gameObject.SetActive(false);
    }

    private void HandleHealthChanged(float percent)
    {
        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        int currentHits = ph != null ? Mathf.RoundToInt(ph.CurrentHealth) : Mathf.RoundToInt(percent * 3f);

        // ระบบช่องเลือดทีละขีด (Segmented Health Blocks)
        // เมื่อโดนยิง ช่องจะหายไปทีละช่องตามจำนวนเลือดที่เหลือ
        if (healthBlocks != null && healthBlocks.Length > 0)
        {
            for (int i = 0; i < healthBlocks.Length; i++)
            {
                if (healthBlocks[i] != null)
                {
                    healthBlocks[i].SetActive(i < currentHits);
                }
            }
        }

        if (hpSlider != null)
        {
            hpSlider.value = percent;

            Image fill = hpSlider.fillRect != null ? hpSlider.fillRect.GetComponent<Image>() : null;
            if (fill == null)
            {
                Image[] images = hpSlider.GetComponentsInChildren<Image>();
                foreach (var img in images) { if (img.type == Image.Type.Filled) { fill = img; break; } }
            }

            if (fill != null)
            {
                // เปลี่ยนสีเมื่อ HP ต่ำ
                fill.color = percent < 0.35f ? hpBarLowColor : hpBarColor;

                // ถ้ายกเลิกการผูก Fill Rect เอาไว้ (เพื่อทำ Center Fill)
                // เราจะใช้การสเกลแกน X แทน (ต้องตั้ง Pivot X ของรูปให้เป็น 0.5)
                if (hpSlider.fillRect == null)
                {
                    fill.transform.localScale = new Vector3(percent, 1f, 1f);
                }
            }
        }
    }

    private void HandleSlowMoChanged(float percent)
    {
        if (slowMoSlider != null)
        {
            slowMoSlider.value = percent;

            Image fill = slowMoSlider.fillRect != null ? slowMoSlider.fillRect.GetComponent<Image>() : null;
            if (fill == null)
            {
                Image[] images = slowMoSlider.GetComponentsInChildren<Image>();
                foreach (var img in images) { if (img.name == "Fill" || img.type == Image.Type.Simple || img.type == Image.Type.Sliced) { fill = img; break; } }
            }

            if (fill != null && slowMoSlider.fillRect == null)
            {
                fill.transform.localScale = new Vector3(percent, 1f, 1f);
            }
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

        // === Katana Zero Timer (ตรงกลางด้านบน) ===
        if (timerText == null)
        {
            CreateTimerUI();
        }
    }

    private void CreateTimerUI()
    {
        // Container หลอดเวลาสไตล์ Katana Zero
        timerContainer = new GameObject("Timer_Container");
        timerContainer.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = timerContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0, -15);
        containerRect.sizeDelta = new Vector2(240, 26);

        // กรอบพื้นหลังสีดำทึบสไตล์ Katana Zero
        Image bgImg = timerContainer.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        // Slider สำหรับหลอดเวลา (Slider แท้ๆ ย่อจากขวามาซ้าย)
        GameObject sliderObj = new GameObject("Timer_Slider");
        sliderObj.transform.SetParent(timerContainer.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(2, 2);
        sliderRect.offsetMax = new Vector2(-2, -2);

        // Background
        Image bg = sliderObj.AddComponent<Image>();
        bg.color = Color.black;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2, 2);
        fillAreaRect.offsetMax = new Vector2(-2, -2);

        // Fill (หลอด Slider สีเต็ม)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = timeBarFullColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        timerSlider = slider;

        // บันทึก timerRect และ timerOriginalPos เพื่อให้หลบขอบจอสโลว์โมชั่นได้
        timerRect = containerRect;
        timerOriginalPos = containerRect.anchoredPosition;

        // Timer Text (TMP)
        GameObject textObj = new GameObject("Timer_Text");
        textObj.transform.SetParent(timerContainer.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        timerText = tmp;
        timerText.fontSize = 16;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = timerTextColor;
        timerText.text = "00:00";

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(1f, 1f, 1f, 0.35f);
        shadow.effectDistance = new Vector2(1, -1);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Penalty Text (TMP)
        GameObject penaltyObj = new GameObject("Penalty_Text");
        penaltyObj.transform.SetParent(timerContainer.transform, false);
        TextMeshProUGUI tmpPenalty = penaltyObj.AddComponent<TextMeshProUGUI>();
        timerPenaltyText = tmpPenalty;
        timerPenaltyText.fontSize = 18;
        timerPenaltyText.fontStyle = FontStyles.Bold;
        timerPenaltyText.alignment = TextAlignmentOptions.Left;
        timerPenaltyText.color = Color.red;
        timerPenaltyText.text = "-5s";

        RectTransform penaltyRect = penaltyObj.GetComponent<RectTransform>();
        penaltyRect.anchorMin = new Vector2(1f, 0.5f);
        penaltyRect.anchorMax = new Vector2(1f, 0.5f);
        penaltyRect.pivot = new Vector2(0f, 0.5f);
        penaltyRect.anchoredPosition = new Vector2(10, 0);
        penaltyRect.sizeDelta = new Vector2(60, 30);

        penaltyObj.SetActive(false);
    }

    private bool isTimerHiddenByTrigger = false;

    /// <summary>
    /// สั่งเปิด/ปิดการแสดงผลของ UI หลอดเวลา (ใช้โดย TimerStartTrigger เมื่อต้องการซ่อนหลอดไว้ก่อน)
    /// </summary>
    public void SetTimerUIVisible(bool visible)
    {
        isTimerHiddenByTrigger = !visible;
        if (timerContainer != null)
        {
            timerContainer.SetActive(visible);
        }
    }

    /// <summary>
    /// สั่งปิดซ่อน UI Gameplay ทั้งหมด (หลอดเลือด, สโลว์, หลอดเวลา, กล่องกระสุน) ตอนจบด่านและ Game Over
    /// </summary>
    public void HideGameplayHUD()
    {
        if (hpSlider != null && hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(false);
        if (slowMoSlider != null && slowMoSlider.gameObject.activeSelf) slowMoSlider.gameObject.SetActive(false);

        // ซ่อนหลอดเวลาและคอมโพเนนต์ที่เกี่ยวข้องทั้งหมด
        if (timerContainer != null && timerContainer.activeSelf) timerContainer.SetActive(false);
        if (timerSlider != null && timerSlider.gameObject.activeSelf) timerSlider.gameObject.SetActive(false);
        if (timerRect != null && timerRect.gameObject.activeSelf) timerRect.gameObject.SetActive(false);
        if (timerText != null && timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
        if (timerPenaltyText != null && timerPenaltyText.gameObject.activeSelf) timerPenaltyText.gameObject.SetActive(false);

        if (timerSlider != null && timerSlider.transform.parent != null && timerSlider.transform.parent.GetComponent<Canvas>() == null)
        {
            if (timerSlider.transform.parent.gameObject.activeSelf)
                timerSlider.transform.parent.gameObject.SetActive(false);
        }
        if (timerText != null && timerText.transform.parent != null && timerText.transform.parent.GetComponent<Canvas>() == null)
        {
            if (timerText.transform.parent.gameObject.activeSelf)
                timerText.transform.parent.gameObject.SetActive(false);
        }

        var timerObj = GameObject.Find("Timer_Container");
        if (timerObj != null && timerObj.activeSelf)
        {
            timerObj.SetActive(false);
        }

        // ซ่อน Segmented Health Blocks (ถ้ามี)
        if (healthBlocks != null)
        {
            for (int i = 0; i < healthBlocks.Length; i++)
            {
                if (healthBlocks[i] != null && healthBlocks[i].activeSelf)
                    healthBlocks[i].SetActive(false);
            }
        }

        // ซ่อนกล่องกระสุน
        var playerShooting = FindFirstObjectByType<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.HideAmmoUI();
        }
        else
        {
            var ammoBoxObj = GameObject.Find("AmmoBox_BG");
            if (ammoBoxObj != null && ammoBoxObj.activeSelf)
                ammoBoxObj.SetActive(false);
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
