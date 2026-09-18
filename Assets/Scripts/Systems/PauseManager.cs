using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบหยุดเกม (Pause Menu)
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel หน้าต่าง Pause Menu ที่สร้างไว้ใน Canvas")]
    public GameObject pausePanel;

    public bool IsPaused { get; private set; } = false;

    private InputAction escapeAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ซ่อนหน้า Pause ไว้ตอนเริ่มเกม
        if (pausePanel != null) pausePanel.SetActive(false);

        // ตั้งค่าให้กดปุ่ม ESC เพื่อหยุด/เล่นต่อ
        escapeAction = new InputAction("Pause", InputActionType.Button);
        escapeAction.AddBinding("<Keyboard>/escape");
        escapeAction.Enable();
    }

    private void OnDestroy()
    {
        escapeAction.Disable();
    }

    private void Update()
    {
        // เช็คว่าถ้าด่านยังเล่นอยู่ (ยังไม่จบด่าน หรือ ตาย) ถึงจะกด Pause ได้
        bool canPause = LevelRatingManager.Instance == null || LevelRatingManager.Instance.isLevelActive;

        if (canPause && escapeAction.WasPressedThisFrame())
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f; // หยุดเวลาในเกม (ศัตรู/กระสุน จะหยุดนิ่ง)
        if (pausePanel != null) pausePanel.SetActive(true); // โชว์ UI
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f; // ให้เวลาเดินตามปกติ
        if (pausePanel != null) pausePanel.SetActive(false); // ซ่อน UI
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Retry ในหน้า Pause
    public void RetryLevel()
    {
        Time.timeScale = 1f; // อย่าลืมคืนค่าเวลาก่อนโหลดฉากใหม่!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Main Menu ในหน้า Pause
    public void GoToMainMenu(string mainMenuSceneName = "MainMenu")
    {
        Time.timeScale = 1f; // อย่าลืมคืนค่าเวลาก่อนเปลี่ยนฉาก!
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
