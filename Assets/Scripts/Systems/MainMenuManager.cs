using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel หน้าจอหลัก (ที่มีปุ่ม Play, Quit)")]
    public GameObject mainMenuPanel;
    
    [Tooltip("Panel หน้าจอเลือกด่าน (ที่มีปุ่มด่านต่างๆ และปุ่ม Back)")]
    public GameObject levelSelectPanel;

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
}
