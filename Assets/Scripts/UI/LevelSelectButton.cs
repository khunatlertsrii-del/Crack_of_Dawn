using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// แปะไว้บนปุ่มเลือกด่านแต่ละปุ่มในหน้า Level Select
/// - จัดการล็อค/ปลดล็อคด่านอัตโนมัติ
/// - ส่งข้อมูลพรีวิว (ชื่อด่าน, ภาพ, คำอธิบาย, ระดับความยาก) ไปที่ LevelPreviewManager เมื่อเอาเมาส์ชี้
/// - ซ่อนตัวหนังสือเดิมเมื่อล็อค ป้องกันปัญหาตัวหนังสือซ้อนกัน
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("=== ตั้งค่าด่าน ===")]
    [Tooltip("ชื่อ Scene ที่ปุ่มนี้จะโหลด")]
    public string sceneName;

    [Tooltip("ชื่อ Scene ด่านก่อนหน้าที่ต้องผ่านก่อนถึงจะปลดล็อค (ปล่อยว่างถ้าเป็นด่านแรก)")]
    public string requiredLevelScene;

    [Header("=== ข้อมูลสำหรับพรีวิว (Preview Details) ===")]
    [Tooltip("ชื่อด่านที่จะแสดงบนจอพรีวิว (เช่น LEVEL 01)")]
    public string displayName = "LEVEL 01";

    [Tooltip("ชื่อตอน / สถานที่ (เช่น TUTORIAL: KITCHEN INVASION)")]
    public string subTitle = "แหล่งกบดานไข่ดาว";

    [TextArea(2, 4)]
    [Tooltip("คำอธิบายภารกิจย่อ")]
    public string description = "บุกทลายรังศัตรูไข่ดาว และฝึกฝนการใช้งานระบบเวลาสโลว์โมชั่น";

    [Tooltip("รูปภาพพรีวิวของด่านนี้ (ลากรูป Screenshot ด่านมาใส่ได้เลย)")]
    public Sprite previewSprite;

    [Tooltip("ระดับความยาก / อันตราย (เช่น THREAT: LOW หรือ ★☆☆☆☆)")]
    public string threatLevel = "THREAT: LOW";

    [Header("=== ข้อความเมื่อด่านล็อค (Locked Info) ===")]
    [Tooltip("ข้อความบนปุ่มเมื่อล็อคอยู่")]
    public string lockedText = "LOCKED";

    [Tooltip("หัวข้อตัวหนังสือสีแดงตอนล็อคบนจอมอนิเตอร์ (แก้ได้ตามใจชอบ เช่น RESTRICTED ACCESS)")]
    public string lockedPreviewTitle = "RESTRICTED ACCESS";

    [TextArea(2, 3)]
    [Tooltip("ข้อความสีแดงบรรทัดล่างบนจอมอนิเตอร์ (แก้ได้ตามใจชอบ เช่น COMPLETE TUTORIAL FIRST หรือ เคลียร์ด่าน 1 ก่อน)")]
    public string lockedPreviewReason = "COMPLETE TUTORIAL TO UNLOCK";

    [Header("=== ภาพล็อค ===")]
    [Tooltip("ภาพ Overlay แสดงตอนด่านล็อคอยู่ (ถ้าไม่ลากใส่จะสร้างอัตโนมัติ)")]
    public GameObject lockedOverlay;

    [Tooltip("Sprite แสดงตอนด่านล็อค (ถ้าไม่ใส่จะใช้สี่เหลี่ยมทึบแทน)")]
    public Sprite lockedSprite;

    [Tooltip("สีของรูปล็อค")]
    public Color lockedColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);

    [Header("=== เสียง ===")]
    [Tooltip("เสียงตอนกดปุ่มที่ล็อค")]
    public AudioClip lockedSound;

    private Button button;
    private bool isUnlocked = false;
    public bool IsUnlocked => isUnlocked;

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

        // ตั้งค่าชื่อเริ่มต้นถ้ายังไม่ได้กรอก
        if (string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(sceneName))
        {
            displayName = sceneName.ToUpper();
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

        // แสดง/ซ่อน Locked Overlay + จัดการตัวหนังสือไม่ให้ทับซ้อนกัน!
        if (unlocked)
        {
            // ปลดล็อคแล้ว — ซ่อนภาพล็อค คืนตัวหนังสือเดิม
            if (lockedOverlay != null) lockedOverlay.SetActive(false);

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(originalButtonText))
                {
                    buttonText.text = originalButtonText;
                }
            }
        }
        else
        {
            // ยังล็อคอยู่ — แสดงภาพล็อค และซ่อนตัวหนังสือเดิม ป้องกันบั๊กซ้อนกัน!
            EnsureLockedOverlay();
            if (lockedOverlay != null) lockedOverlay.SetActive(true);

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(false); // ซ่อนตัวหนังสือเดิมเพื่อไม่ให้ซ้อนกับ LOCKED
            }
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

        // ใส่ Text แสดงสถานะล็อค
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
            tmpText.fontSize = 20;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = new Color(0.9f, 0.4f, 0.4f, 1f); // สีแดงส้มบ่งบอกว่าล็อค
            tmpText.raycastTarget = false;
        }

        lockedOverlay = overlay;
    }

    // =========================================================
    // เมาส์ชี้ / ไฮไลต์ปุ่ม -> อัปเดตพรีวิว
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        TriggerPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DismissPreview();
    }

    public void OnSelect(BaseEventData eventData)
    {
        TriggerPreview();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        DismissPreview();
    }

    private void TriggerPreview()
    {
        if (LevelPreviewManager.Instance != null)
        {
            LevelPreviewManager.Instance.ShowPreview(this);
        }
    }

    private void DismissPreview()
    {
        if (LevelPreviewManager.Instance != null)
        {
            LevelPreviewManager.Instance.HidePreview(this);
        }
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
    // DEBUG — สำหรับทดสอบ
    // =========================================================

    [ContextMenu("DEBUG: ปลดล็อคทุกด่าน")]
    public void DebugUnlockAll()
    {
        PlayerPrefs.SetInt("LevelCompleted_Tutorial", 1);
        PlayerPrefs.SetInt("LevelCompleted_Level_2", 1);
        PlayerPrefs.Save();
        CheckUnlockStatus();
        TriggerPreview();
        Debug.Log("[DEBUG] ปลดล็อคทุกด่านแล้ว!");
    }

    [ContextMenu("DEBUG: ล็อคทุกด่าน")]
    public void DebugLockAll()
    {
        PlayerPrefs.DeleteKey("LevelCompleted_Tutorial");
        PlayerPrefs.DeleteKey("LevelCompleted_Level_2");
        PlayerPrefs.Save();
        CheckUnlockStatus();
        TriggerPreview();
        Debug.Log("[DEBUG] ล็อคทุกด่านแล้ว!");
    }
}
