using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบแสดงภาพและข้อมูลพรีวิวด่าน (Level Preview Monitor)
/// ในหน้าเลือกด่าน (Level Select)
/// 
/// คุณสมบัติ:
/// 1. ขยายภาพพรีวิวเต็มกรอบ (Full Frame) ไม่หดเล็กลง พร้อมตัวเลือก preserveAspectRatio
/// 2. ปรับแต่งข้อความสีแดงตอนล็อคได้ตามใจชอบ (ทั้งใน LevelSelectButton และที่นี่)
/// 3. ปราศจาก Unicode Emojis ที่ทำให้เกิดกล่องสี่เหลี่ยม (□) ในฟอนต์พิกเซล
/// 4. ตรวจจับและเชื่อมต่อกับ UI ที่ผู้ใช้สร้างไว้ในฉากอัตโนมัติ (เช่น LevelPerview) หรือสร้างใหม่ให้ถ้ายังไม่มี
/// </summary>
public class LevelPreviewManager : MonoBehaviour
{
    public static LevelPreviewManager Instance { get; private set; }

    [Header("=== ตั้งค่ารูปภาพพรีวิว (Image Settings) ===")]
    [Tooltip("รักษาสัดส่วนภาพหรือไม่ (False = ยืดเต็มกรอบจอ 100%, True = รักษาสัดส่วนไม่ให้ภาพเบี้ยว)")]
    public bool preserveAspectRatio = false;

    [Header("=== ข้อความเริ่มต้นเมื่อด่านล็อค (Default Locked Texts) ===")]
    [Tooltip("หัวข้อสีแดงค่าเริ่มต้น (หากปุ่มด่านนั้นไม่ได้ระบุไว้)")]
    public string defaultLockedTitle = "RESTRICTED ACCESS";

    [TextArea(1, 3)]
    [Tooltip("ข้อความสีแดงบรรทัดล่างค่าเริ่มต้น")]
    public string defaultLockedReason = "CLEAR PREVIOUS STAGE TO UNLOCK";

    [Header("=== UI References (ถ้าเว้นว่างจะหาในฉากหรือสร้างให้อัตโนมัติ) ===")]
    [Tooltip("Panel หน้าต่างพรีวิวหลัก")]
    public GameObject previewPanel;

    [Tooltip("Image แสดงภาพพรีวิวด่าน")]
    public Image previewImage;

    [Tooltip("Text แสดงชื่อด่านหลัก (เช่น LEVEL 01)")]
    public TMP_Text titleText;

    [Tooltip("Text แสดงชื่อบทย่อย / สถานที่")]
    public TMP_Text subtitleText;

    [Tooltip("Text แสดงคำอธิบายภารกิจย่อ")]
    public TMP_Text descriptionText;

    [Tooltip("Text แสดงระดับความยาก / อันตราย")]
    public TMP_Text threatText;

    [Tooltip("ป้ายสถานะ (LOCKED / READY / CLEARED)")]
    public TMP_Text statusBadgeText;
    public Image statusBadgeBg;

    [Tooltip("หน้าต่างทับภาพตอนด่านล็อค")]
    public GameObject lockedOverlay;
    public TMP_Text lockedReasonText;

    [Tooltip("จุดไฟกระพริบสีแดง [● REC] จำลองกล้องวงจรปิด")]
    public Graphic recDotGraphic;

    [Header("=== การตั้งค่าเอฟเฟกต์ ===")]
    [Tooltip("เล่นเสียงตอนสลับพรีวิว")]
    public AudioClip previewSwitchSound;

    [Tooltip("ความเร็วในการกระพริบไฟ REC")]
    public float recBlinkSpeed = 2f;

    // Sprite สำรองเมื่อไม่มีรูปภาพ
    private Sprite defaultLevel1Sprite;
    private Sprite defaultLevel2Sprite;

    private Coroutine recBlinkCoroutine;
    private Coroutine punchScaleCoroutine;

    // ปุ่มที่กำลังชี้อยู่ปัจจุบัน
    private LevelSelectButton currentActiveButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        GenerateDefaultSprites();
        EnsureUIExists();
        HidePreview();
    }

    private void Start()
    {
        HidePreview();
    }

    private void OnEnable()
    {
        if (recDotGraphic != null)
        {
            recBlinkCoroutine = StartCoroutine(BlinkRecRoutine());
        }

        // เริ่มต้นด้วยการซ่อนหน้าต่างพรีวิวไว้ก่อน จนกว่าผู้เล่นจะเอาเมาส์มาชี้
        HidePreview();
    }

    private void OnDisable()
    {
        if (recBlinkCoroutine != null)
        {
            StopCoroutine(recBlinkCoroutine);
            recBlinkCoroutine = null;
        }

        HidePreview();
    }

    /// <summary>
    /// ซ่อนหน้าต่างพรีวิวเมื่อผู้เล่นเลื่อนเมาส์ออกจากปุ่ม
    /// </summary>
    public void HidePreview(LevelSelectButton buttonData = null)
    {
        // ถ้าส่ง buttonData มา ให้เช็คว่าเป็นปุ่มเดิมที่กำลังแสดงอยู่หรือไม่
        // ป้องกันกรณีที่ผู้เล่นเลื่อนเมาส์จากปุ่ม Level 1 ไป Level 2 ทันที แล้วปุ่มเดิมมาสั่งปิดทับ
        if (buttonData != null && currentActiveButton != buttonData)
        {
            return;
        }

        currentActiveButton = null;
        if (previewPanel != null && previewPanel.activeSelf)
        {
            previewPanel.SetActive(false);
        }
    }

    /// <summary>
    /// อัปเดตข้อมูลพรีวิวตามปุ่มที่เอาเมาส์ไปชี้
    /// </summary>
    public void ShowPreview(LevelSelectButton buttonData)
    {
        if (buttonData == null) return;
        currentActiveButton = buttonData;
        EnsureUIExists();

        if (previewPanel != null && !previewPanel.activeSelf)
        {
            previewPanel.SetActive(true);
        }

        bool isUnlocked = buttonData.IsUnlocked;
        bool isCompleted = PlayerPrefs.GetInt("LevelCompleted_" + buttonData.sceneName, 0) == 1;

        // 1. ตั้งชื่อด่าน
        string displayName = !string.IsNullOrEmpty(buttonData.displayName) 
            ? buttonData.displayName 
            : buttonData.sceneName.ToUpper();

        if (titleText != null)
        {
            titleText.text = displayName;
        }

        // 2. ข้อมูลย่อย & สถานที่
        if (subtitleText != null)
        {
            subtitleText.text = !string.IsNullOrEmpty(buttonData.subTitle) 
                ? buttonData.subTitle 
                : "SECTOR // " + buttonData.sceneName;
        }

        // 3. คำอธิบายภารกิจ
        if (descriptionText != null)
        {
            if (!string.IsNullOrEmpty(buttonData.description))
            {
                descriptionText.text = buttonData.description;
            }
            else
            {
                descriptionText.text = isUnlocked 
                    ? "MISSION: INFILTRATE AND ELIMINATE TARGETS." 
                    : "ENCRYPTED FILE: CLEAR PREVIOUS SECTOR FIRST.";
            }
        }

        // 4. ระดับความอันตราย (Threat Level)
        if (threatText != null)
        {
            threatText.text = !string.IsNullOrEmpty(buttonData.threatLevel) 
                ? buttonData.threatLevel 
                : "THREAT: NORMAL";
        }

        // 5. รูปพรีวิวด่าน (จัดตำแหน่งเต็มกรอบเสมอ ไม่หดเล็กลง)
        if (previewImage != null)
        {
            SetupImageRect(previewImage);

            if (buttonData.previewSprite != null)
            {
                previewImage.sprite = buttonData.previewSprite;
                previewImage.color = isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
            else
            {
                Sprite fallback = buttonData.sceneName.Contains("2") ? defaultLevel2Sprite : defaultLevel1Sprite;
                previewImage.sprite = fallback;
                previewImage.color = isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
        }

        // 6. หน้าต่างล็อค (Locked Overlay) และข้อความสีแดง (ปรับแต่งได้อิสระ)
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
            if (!isUnlocked && lockedReasonText != null)
            {
                string title = !string.IsNullOrEmpty(buttonData.lockedPreviewTitle) 
                    ? buttonData.lockedPreviewTitle 
                    : defaultLockedTitle;

                string reason = !string.IsNullOrEmpty(buttonData.lockedPreviewReason)
                    ? buttonData.lockedPreviewReason
                    : defaultLockedReason;

                lockedReasonText.text = $"{title}\n[ {reason} ]";
            }
        }

        // 7. ป้ายสถานะ (Status Badge) — ไม่มีอีโมจิ เพื่อไม่ให้เกิดกล่อง □ บนฟอนต์พิกเซล
        if (statusBadgeText != null)
        {
            if (!isUnlocked)
            {
                statusBadgeText.text = "LOCKED";
                if (statusBadgeBg != null) statusBadgeBg.color = new Color(0.85f, 0.2f, 0.2f, 0.9f); // สีแดง
            }
            else if (isCompleted)
            {
                statusBadgeText.text = "CLEARED";
                if (statusBadgeBg != null) statusBadgeBg.color = new Color(0.2f, 0.8f, 0.35f, 0.9f); // สีเขียว
            }
            else
            {
                statusBadgeText.text = "READY";
                if (statusBadgeBg != null) statusBadgeBg.color = new Color(0.95f, 0.65f, 0.1f, 0.9f); // สีส้มทอง
            }
        }

        // เล่นเอฟเฟกต์ Scale กระตุกเบาๆ ตอนสลับภาพ
        if (previewPanel != null)
        {
            if (punchScaleCoroutine != null) StopCoroutine(punchScaleCoroutine);
            punchScaleCoroutine = StartCoroutine(PunchPreviewScale());
        }

        // เสียง Hover
        if (AudioManager.Instance != null && previewSwitchSound != null)
        {
            AudioManager.Instance.PlayUISound(previewSwitchSound);
        }
    }

    /// <summary>
    /// ปรับ RectTransform ของรูปภาพพรีวิวให้ขึงเต็มกรอบ 100% ไม่หดเล็กลง
    /// </summary>
    private void SetupImageRect(Image img)
    {
        if (img == null) return;
        RectTransform rt = img.rectTransform;
        if (rt != null)
        {
            // ขึงเต็มกรอบพ่อแม่ (Stretch to Fill)
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;
        }
        img.preserveAspect = preserveAspectRatio;
    }

    private IEnumerator PunchPreviewScale()
    {
        if (previewPanel == null) yield break;
        Transform target = previewImage != null ? previewImage.transform : previewPanel.transform;
        Vector3 baseScale = Vector3.one;

        float t = 0;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin((t / 0.15f) * Mathf.PI) * 0.035f;
            target.localScale = baseScale * s;
            yield return null;
        }
        target.localScale = baseScale;
    }

    private IEnumerator BlinkRecRoutine()
    {
        while (true)
        {
            if (recDotGraphic != null)
            {
                recDotGraphic.enabled = !recDotGraphic.enabled;
            }
            yield return new WaitForSecondsRealtime(0.5f / recBlinkSpeed);
        }
    }

    // =========================================================
    // การค้นหาและสร้าง UI
    // =========================================================

    private void EnsureUIExists()
    {
        if (previewPanel != null)
        {
            if (previewImage != null) SetupImageRect(previewImage);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform parentTransform = canvas.transform;
        MainMenuManager menuMgr = FindFirstObjectByType<MainMenuManager>();
        if (menuMgr != null && menuMgr.levelSelectPanel != null)
        {
            parentTransform = menuMgr.levelSelectPanel.transform;
        }

        // 1. ตรวจสอบว่าในฉากมี Panel ที่ผู้ใช้วางไว้แล้วหรือไม่ (เช่น LevelPerview หรือ LevelPreview)
        Transform existing = parentTransform.Find("LevelPerview");
        if (existing == null) existing = parentTransform.Find("LevelPreview");
        if (existing == null) existing = parentTransform.Find("LevelPreviewCard");

        if (existing != null)
        {
            previewPanel = existing.gameObject;
            BindExistingUIElements(existing);
            return;
        }

        // 2. ถ้ายังไม่มี ให้สร้างขึ้นมาใหม่แบบจัดเต็ม
        CreateProceduralCard(parentTransform);
    }

    private void BindExistingUIElements(Transform panel)
    {
        // ค้นหา ImageFrame
        Transform imgFrame = panel.Find("ImageFrame");
        if (imgFrame != null)
        {
            // หา Image ที่อยู่ใต้ ImageFrame
            Image childImg = imgFrame.GetComponentInChildren<Image>();
            if (childImg != null && childImg.gameObject != imgFrame.gameObject)
            {
                previewImage = childImg;
            }
            else
            {
                previewImage = imgFrame.GetComponent<Image>();
            }

            SetupImageRect(previewImage);
        }
        else
        {
            previewImage = panel.GetComponentInChildren<Image>();
            SetupImageRect(previewImage);
        }

        // ค้นหา Text อื่นๆ
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string n = t.name.ToLower();
            if (n.Contains("title") && titleText == null) titleText = t;
            else if (n.Contains("sub") && subtitleText == null) subtitleText = t;
            else if (n.Contains("desc") && descriptionText == null) descriptionText = t;
            else if (n.Contains("threat") && threatText == null) threatText = t;
            else if (n.Contains("badge") && statusBadgeText == null) statusBadgeText = t;
            else if (n.Contains("lock") && lockedReasonText == null) lockedReasonText = t;
        }

        // ค้นหา LockedOverlay
        Transform lockTrans = panel.Find("LockedOverlay");
        if (lockTrans == null && imgFrame != null) lockTrans = imgFrame.Find("LockedOverlay");
        if (lockTrans != null)
        {
            lockedOverlay = lockTrans.gameObject;
            if (lockedReasonText == null) lockedReasonText = lockTrans.GetComponentInChildren<TMP_Text>();
        }
        else if (imgFrame != null)
        {
            // สร้าง LockedOverlay ให้หากยังไม่มี
            GameObject lockOverlayObj = new GameObject("LockedOverlay");
            lockOverlayObj.transform.SetParent(imgFrame, false);
            RectTransform lockRect = lockOverlayObj.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;

            Image lockBg = lockOverlayObj.AddComponent<Image>();
            lockBg.color = new Color(0.05f, 0.05f, 0.07f, 0.88f);

            GameObject lockTextObj = new GameObject("LockText");
            lockTextObj.transform.SetParent(lockOverlayObj.transform, false);
            RectTransform lockTextRect = lockTextObj.AddComponent<RectTransform>();
            lockTextRect.anchorMin = Vector2.zero;
            lockTextRect.anchorMax = Vector2.one;
            lockTextRect.offsetMin = Vector2.zero;
            lockTextRect.offsetMax = Vector2.zero;

            lockedReasonText = lockTextObj.AddComponent<TextMeshProUGUI>();
            lockedReasonText.text = $"{defaultLockedTitle}\n[ {defaultLockedReason} ]";
            lockedReasonText.alignment = TextAlignmentOptions.Center;
            lockedReasonText.fontSize = 18;
            lockedReasonText.fontStyle = FontStyles.Bold;
            lockedReasonText.color = new Color(1f, 0.35f, 0.35f, 1f);

            lockedOverlay = lockOverlayObj;
            lockedOverlay.SetActive(false);
        }

        // ค้นหาจุดแดง REC
        Transform recDot = panel.Find("REC_Dot");
        if (recDot == null) recDot = panel.Find("HeaderBar/REC_Dot");
        if (recDot != null) recDotGraphic = recDot.GetComponent<Graphic>();
    }

    private void CreateProceduralCard(Transform parentTransform)
    {
        GameObject cardObj = new GameObject("LevelPreviewCard");
        cardObj.transform.SetParent(parentTransform, false);

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.52f, 0.15f);
        cardRect.anchorMax = new Vector2(0.92f, 0.88f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.65f, 0.15f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header Bar
        GameObject headerObj = new GameObject("HeaderBar");
        headerObj.transform.SetParent(cardObj.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.91f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(12f, 0f);
        headerRect.offsetMax = new Vector2(-12f, -6f);

        TMP_Text headerTxt = headerObj.AddComponent<TextMeshProUGUI>();
        headerTxt.text = "SURVEILLANCE FEED // CAM-01";
        headerTxt.fontSize = 15;
        headerTxt.fontStyle = FontStyles.Bold;
        headerTxt.color = new Color(0.8f, 0.85f, 0.9f, 0.8f);
        headerTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // จุดแดง REC
        GameObject recObj = new GameObject("REC_Dot");
        recObj.transform.SetParent(headerObj.transform, false);
        RectTransform recRect = recObj.AddComponent<RectTransform>();
        recRect.anchorMin = new Vector2(1f, 0f);
        recRect.anchorMax = new Vector2(1f, 1f);
        recRect.anchoredPosition = new Vector2(-40f, 0f);
        recRect.sizeDelta = new Vector2(14f, 14f);

        Image recImg = recObj.AddComponent<Image>();
        recImg.color = new Color(1f, 0.2f, 0.2f, 1f);
        recDotGraphic = recImg;

        GameObject recTextObj = new GameObject("REC_Text");
        recTextObj.transform.SetParent(headerObj.transform, false);
        RectTransform recTxtRect = recTextObj.AddComponent<RectTransform>();
        recTxtRect.anchorMin = new Vector2(1f, 0f);
        recTxtRect.anchorMax = new Vector2(1f, 1f);
        recTxtRect.anchoredPosition = new Vector2(-15f, 0f);
        recTxtRect.sizeDelta = new Vector2(35f, 20f);
        TMP_Text recTxt = recTextObj.AddComponent<TextMeshProUGUI>();
        recTxt.text = "REC";
        recTxt.fontSize = 13;
        recTxt.fontStyle = FontStyles.Bold;
        recTxt.color = Color.red;

        // Image Frame
        GameObject imgFrameObj = new GameObject("ImageFrame");
        imgFrameObj.transform.SetParent(cardObj.transform, false);
        RectTransform imgFrameRect = imgFrameObj.AddComponent<RectTransform>();
        imgFrameRect.anchorMin = new Vector2(0.04f, 0.42f);
        imgFrameRect.anchorMax = new Vector2(0.96f, 0.90f);
        imgFrameRect.offsetMin = Vector2.zero;
        imgFrameRect.offsetMax = Vector2.zero;

        Image frameBg = imgFrameObj.AddComponent<Image>();
        frameBg.color = Color.black;

        // Preview Image
        GameObject previewImgObj = new GameObject("PreviewImage");
        previewImgObj.transform.SetParent(imgFrameObj.transform, false);
        RectTransform previewImgRect = previewImgObj.AddComponent<RectTransform>();
        previewImgRect.anchorMin = Vector2.zero;
        previewImgRect.anchorMax = Vector2.one;
        previewImgRect.offsetMin = Vector2.zero;
        previewImgRect.offsetMax = Vector2.zero;

        previewImage = previewImgObj.AddComponent<Image>();
        previewImage.preserveAspect = preserveAspectRatio;

        // Locked Overlay
        GameObject lockOverlayObj = new GameObject("LockedOverlay");
        lockOverlayObj.transform.SetParent(imgFrameObj.transform, false);
        RectTransform lockRect = lockOverlayObj.AddComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.offsetMin = Vector2.zero;
        lockRect.offsetMax = Vector2.zero;

        Image lockBg = lockOverlayObj.AddComponent<Image>();
        lockBg.color = new Color(0.05f, 0.05f, 0.07f, 0.88f);

        GameObject lockTextObj = new GameObject("LockText");
        lockTextObj.transform.SetParent(lockOverlayObj.transform, false);
        RectTransform lockTextRect = lockTextObj.AddComponent<RectTransform>();
        lockTextRect.anchorMin = Vector2.zero;
        lockTextRect.anchorMax = Vector2.one;
        lockTextRect.offsetMin = Vector2.zero;
        lockTextRect.offsetMax = Vector2.zero;

        lockedReasonText = lockTextObj.AddComponent<TextMeshProUGUI>();
        lockedReasonText.text = $"{defaultLockedTitle}\n[ {defaultLockedReason} ]";
        lockedReasonText.alignment = TextAlignmentOptions.Center;
        lockedReasonText.fontSize = 18;
        lockedReasonText.fontStyle = FontStyles.Bold;
        lockedReasonText.color = new Color(1f, 0.35f, 0.35f, 1f);

        lockedOverlay = lockOverlayObj;
        lockedOverlay.SetActive(false);

        // Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.32f);
        titleRect.anchorMax = new Vector2(0.70f, 0.40f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "LEVEL 01";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Status Badge
        GameObject badgeObj = new GameObject("StatusBadge");
        badgeObj.transform.SetParent(cardObj.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.72f, 0.32f);
        badgeRect.anchorMax = new Vector2(0.96f, 0.40f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;

        statusBadgeBg = badgeObj.AddComponent<Image>();
        statusBadgeBg.color = new Color(0.95f, 0.65f, 0.1f, 0.9f);

        GameObject badgeTxtObj = new GameObject("BadgeText");
        badgeTxtObj.transform.SetParent(badgeObj.transform, false);
        RectTransform badgeTxtRect = badgeTxtObj.AddComponent<RectTransform>();
        badgeTxtRect.anchorMin = Vector2.zero;
        badgeTxtRect.anchorMax = Vector2.one;
        badgeTxtRect.offsetMin = Vector2.zero;
        badgeTxtRect.offsetMax = Vector2.zero;

        statusBadgeText = badgeTxtObj.AddComponent<TextMeshProUGUI>();
        statusBadgeText.text = "READY";
        statusBadgeText.fontSize = 14;
        statusBadgeText.fontStyle = FontStyles.Bold;
        statusBadgeText.alignment = TextAlignmentOptions.Center;
        statusBadgeText.color = Color.black;

        // Subtitle Text
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(cardObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.05f, 0.25f);
        subRect.anchorMax = new Vector2(0.95f, 0.32f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "TUTORIAL: FIRST CONTACT";
        subtitleText.fontSize = 16;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.color = new Color(0.95f, 0.65f, 0.15f, 1f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Description Text
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(cardObj.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.05f);
        descRect.anchorMax = new Vector2(0.95f, 0.24f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;

        descriptionText = descObj.AddComponent<TextMeshProUGUI>();
        descriptionText.text = "MISSION: INFILTRATE AND ELIMINATE TARGETS.";
        descriptionText.fontSize = 14;
        descriptionText.color = new Color(0.85f, 0.88f, 0.9f, 0.85f);
        descriptionText.alignment = TextAlignmentOptions.TopLeft;

        previewPanel = cardObj;
    }

    private void GenerateDefaultSprites()
    {
        defaultLevel1Sprite = CreateGridTexture(new Color(0.12f, 0.15f, 0.25f), new Color(0.95f, 0.65f, 0.1f), "01");
        defaultLevel2Sprite = CreateGridTexture(new Color(0.22f, 0.10f, 0.12f), new Color(0.95f, 0.3f, 0.2f), "02");
    }

    private Sprite CreateGridTexture(Color bg, Color gridColor, string label)
    {
        int w = 160;
        int h = 90;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x < 2 || x >= w - 2 || y < 2 || y >= h - 2);
                bool isGrid = (x % 16 == 0 || y % 15 == 0);
                bool isCenterCross = (Mathf.Abs(x - w / 2) < 2 && Mathf.Abs(y - h / 2) < 12) || 
                                     (Mathf.Abs(y - h / 2) < 2 && Mathf.Abs(x - w / 2) < 12);

                if (isBorder || isCenterCross)
                {
                    pixels[y * w + x] = gridColor;
                }
                else if (isGrid)
                {
                    pixels[y * w + x] = Color.Lerp(bg, gridColor, 0.25f);
                }
                else
                {
                    pixels[y * w + x] = bg;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
    }
}
