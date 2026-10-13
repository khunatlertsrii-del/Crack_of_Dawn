using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel หน้าจอหลัก (ที่มีปุ่ม Play, Quit)")]
    public GameObject mainMenuPanel;
    
    [Tooltip("Panel หน้าจอเลือกด่าน (ที่มีปุ่มด่านต่างๆ และปุ่ม Back)")]
    public GameObject levelSelectPanel;

    private void Awake()
    {
        // สร้าง LevelPreviewManager ให้อัตโนมัติหากยังไม่มีในฉาก
        if (FindFirstObjectByType<LevelPreviewManager>() == null)
        {
            gameObject.AddComponent<LevelPreviewManager>();
        }
    }

    private void Start()
    {
        // เริ่มมาให้โชว์หน้า Main Menu และซ่อนหน้า Level Select
        ShowMainMenu();
    }

    // ==========================================
    // ฟังก์ชันสำหรับผูกกับปุ่มต่างๆ (Button OnClick)
    // ==========================================

    /// <summary>
    /// ผูกกับปุ่ม Play Game ในหน้าแรก
    /// </summary>
    public void ShowLevelSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);

        // รีเฟรชสถานะล็อคของปุ่มทุกปุ่ม
        RefreshLevelButtons();

        // ซ่อนหน้าต่างพรีวิวไว้ก่อน จนกว่าผู้เล่นจะเอาเมาส์ไปชี้ปุ่มเลเวล
        if (LevelPreviewManager.Instance != null)
        {
            LevelPreviewManager.Instance.HidePreview();
        }
    }

    /// <summary>
    /// ผูกกับปุ่ม Back ในหน้าเลือกด่าน
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    /// <summary>
    /// ผูกกับปุ่ม Level ต่างๆ โดยพิมพ์ชื่อ Scene ด่านลงไปตรงๆ ใน Unity ได้เลย
    /// </summary>
    /// <param name="sceneName">ชื่อด่านที่จะโหลด เช่น "Level_1"</param>
    public void LoadLevel(string sceneName)
    {
        Debug.Log($"กำลังโหลดด่าน: {sceneName}");
        Time.timeScale = 1f; // รีเซ็ต TimeScale ก่อนเสมอ (กัน Victory Screen ค้าง TimeScale = 0)
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ผูกกับปุ่ม Quit
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("ออกจากเกม!");
        Application.Quit();
    }

    // ==========================================
    // LEVEL LOCK UTILITIES
    // ==========================================

    /// <summary>
    /// รีเฟรชสถานะล็อค/ปลดล็อคของปุ่มเลือกด่านทุกปุ่ม
    /// </summary>
    public void RefreshLevelButtons()
    {
        LevelSelectButton[] buttons = FindObjectsByType<LevelSelectButton>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            btn.CheckUnlockStatus();
        }
    }

    /// <summary>
    /// เช็คว่าด่านนี้ผ่านแล้วหรือยัง
    /// </summary>
    public static bool IsLevelCompleted(string sceneName)
    {
        return PlayerPrefs.GetInt("LevelCompleted_" + sceneName, 0) == 1;
    }

    /// <summary>
    /// ผูกกับปุ่ม "Reset Progress" บน UI ได้โดยตรง เพื่อล้างข้อมูลเซฟทั้งหมดในเครื่อง!
    /// (สามารถปิดซ่อนปุ่มนี้ก่อนบิวด์ได้เลย ไม่มีผลเสียใดๆ)
    /// </summary>
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        RefreshLevelButtons();
        
        if (LevelPreviewManager.Instance != null)
        {
            LevelPreviewManager.Instance.HidePreview();
        }

        Debug.Log("[MainMenuManager] ล้างข้อมูลเซฟทั้งหมด (PlayerPrefs) เรียบร้อยแล้ว! ทุกด่านกลับมาล็อคเหมือนเริ่มเกมใหม่");
    }

    /// <summary>
    /// ปลดล็อคทุกด่าน (สำหรับ Debug)
    /// </summary>
    [ContextMenu("DEBUG: ปลดล็อคทุกด่าน")]
    public void DebugUnlockAll()
    {
        PlayerPrefs.SetInt("LevelCompleted_Tutorial", 1);
        PlayerPrefs.SetInt("LevelCompleted_Level_2", 1);
        PlayerPrefs.Save();
        RefreshLevelButtons();
        Debug.Log("[DEBUG] ปลดล็อคทุกด่านแล้ว!");
    }

    /// <summary>
    /// ล็อคทุกด่านกลับ (สำหรับ Debug)
    /// </summary>
    [ContextMenu("DEBUG: ล็อคทุกด่าน")]
    public void DebugLockAll()
    {
        ResetAllProgress();
    }
}
