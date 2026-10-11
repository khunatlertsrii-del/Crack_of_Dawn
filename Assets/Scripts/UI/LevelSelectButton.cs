using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// แปะไว้บนปุ่มเลือกด่านแต่ละปุ่มในหน้า Level Select
/// จัดการล็อค/ปลดล็อคด่านอัตโนมัติ
/// 
/// วิธีใช้:
/// 1. ลากสคริปต์นี้ไปแปะบนปุ่มด่าน (เช่น Level_2)
/// 2. ตั้ง sceneName = ชื่อ Scene ที่ปุ่มนี้จะโหลด (เช่น "Level_2")
/// 3. ตั้ง requiredLevelScene = ชื่อ Scene ที่ต้องผ่านก่อน (เช่น "Tutorial")
///    ถ้าเป็นด่านแรก ปล่อยว่างไว้ = ไม่ต้องล็อค
/// 4. ลาก Image ที่จะแสดงตอนล็อคมาใส่ lockedOverlay (ถ้าไม่ใส่จะสร้างอัตโนมัติ)
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour
{
    [Header("=== ตั้งค่าด่าน ===")]
    [Tooltip("ชื่อ Scene ที่ปุ่มนี้จะโหลด")]
    public string sceneName;

    [Tooltip("ชื่อ Scene ด่านก่อนหน้าที่ต้องผ่านก่อนถึงจะปลดล็อค (ปล่อยว่างถ้าเป็นด่านแรก)")]
    public string requiredLevelScene;

    [Header("=== ภาพล็อค ===")]
    [Tooltip("ภาพ Overlay แสดงตอนด่านล็อคอยู่ (ถ้าไม่ลากใส่จะสร้างอัตโนมัติ)")]
    public GameObject lockedOverlay;

    [Tooltip("Sprite แสดงตอนด่านล็อค (ถ้าไม่ใส่จะใช้สี่เหลี่ยมทึบแทน)")]
    public Sprite lockedSprite;

    [Tooltip("สีของรูปล็อค")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);

    [Tooltip("ข้อความแสดงตอนล็อค (เช่น 'ล็อคอยู่')")]
    public string lockedText = "🔒";

    [Header("=== เสียง ===")]
    [Tooltip("เสียงตอนกดปุ่มที่ล็อค")]
    public AudioClip lockedSound;

    private Button button;
    private bool isUnlocked = false;
    private TMP_Text buttonText;
    private string originalButtonText;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            originalButtonText = buttonText.text;
        }
    }

    private void Start()
    {
        CheckUnlockStatus();
    }

    private void OnEnable()
    {
        // ทุกครั้งที่ Panel เปิดมา เช็คใหม่ (เผื่อผู้เล่นเพิ่งผ่านด่านมา)
        CheckUnlockStatus();
    }

    /// <summary>
    /// เช็คว่าด่านนี้ปลดล็อคแล้วหรือยัง
    /// </summary>
    public void CheckUnlockStatus()
    {
        // ถ้าไม่ได้ตั้ง requiredLevelScene = ด่านแรก = ปลดล็อคเสมอ
        if (string.IsNullOrEmpty(requiredLevelScene))
        {
            SetUnlocked(true);
            return;
        }

        // เช็คว่าด่านที่กำหนดผ่านแล้วหรือยัง
        bool completed = PlayerPrefs.GetInt("LevelCompleted_" + requiredLevelScene, 0) == 1;
        SetUnlocked(completed);
    }

    private void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        if (button != null)
        {
            button.interactable = unlocked;
        }

        // แสดง/ซ่อน Locked Overlay
        if (unlocked)
        {
            // ปลดล็อคแล้ว — ซ่อนภาพล็อค
            if (lockedOverlay != null) lockedOverlay.SetActive(false);

            // คืนข้อความเดิม
            if (buttonText != null && !string.IsNullOrEmpty(originalButtonText))
            {
                buttonText.text = originalButtonText;
            }
        }
        else
        {
            // ยังล็อคอยู่ — แสดงภาพล็อค
            EnsureLockedOverlay();
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
        }
    }

    /// <summary>
    /// สร้าง Locked Overlay อัตโนมัติถ้ายังไม่มี
    /// </summary>
    private void EnsureLockedOverlay()
    {
        if (lockedOverlay != null) return;

        // สร้าง Overlay ใหม่เป็นลูกของปุ่มนี้
        GameObject overlay = new GameObject("LockedOverlay");
        overlay.transform.SetParent(transform, false);

        // ตั้ง RectTransform ให้เต็มปุ่ม
        RectTransform rect = overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // ใส่ Image เป็นพื้นหลังทึบ
        Image img = overlay.AddComponent<Image>();
        if (lockedSprite != null)
        {
            img.sprite = lockedSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = lockedColor;
        }
        img.raycastTarget = false; // ไม่กั้น Raycast ของปุ่มเดิม

        // ใส่ Text แสดงกุญแจ 🔒
        if (!string.IsNullOrEmpty(lockedText))
        {
            GameObject textObj = new GameObject("LockedText");
            textObj.transform.SetParent(overlay.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = lockedText;
            tmpText.fontSize = 24;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;
        }

        lockedOverlay = overlay;
    }

    /// <summary>
    /// เรียกจาก Button OnClick (ใช้แทน MainMenuManager.LoadLevel ได้)
    /// ถ้าล็อคอยู่จะเตือน ถ้าปลดล็อคแล้วจะโหลดด่าน
    /// </summary>
    public void TryLoadLevel()
    {
        if (!isUnlocked)
        {
            // เล่นเสียงเตือน
            if (lockedSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(lockedSound);
            }

            Debug.Log($"[LevelSelect] ด่าน {sceneName} ยังล็อคอยู่! ต้องผ่านด่าน {requiredLevelScene} ก่อน");
            return;
        }

        // โหลดด่าน
        Debug.Log($"[LevelSelect] กำลังโหลดด่าน: {sceneName}");
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    // =========================================================
    // DEBUG — สำหรับทดสอบ (ลบทิ้งตอนส่งเกมจริงได้)
    // =========================================================

    /// <summary>
    /// ปลดล็อคด่านทุกด่าน (สำหรับทดสอบ)
    /// </summary>
    [ContextMenu("DEBUG: ปลดล็อคทุกด่าน")]
    public void DebugUnlockAll()
    {
        PlayerPrefs.SetInt("LevelCompleted_Tutorial", 1);
        PlayerPrefs.SetInt("LevelCompleted_Level_2", 1);
        PlayerPrefs.Save();
        CheckUnlockStatus();
        Debug.Log("[DEBUG] ปลดล็อคทุกด่านแล้ว!");
    }

    /// <summary>
    /// ล็อคด่านทุกด่าน (สำหรับทดสอบ)
    /// </summary>
    [ContextMenu("DEBUG: ล็อคทุกด่าน")]
    public void DebugLockAll()
    {
        PlayerPrefs.DeleteKey("LevelCompleted_Tutorial");
        PlayerPrefs.DeleteKey("LevelCompleted_Level_2");
        PlayerPrefs.Save();
        CheckUnlockStatus();
        Debug.Log("[DEBUG] ล็อคทุกด่านแล้ว!");
    }
}
