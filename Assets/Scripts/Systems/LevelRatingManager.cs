using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบประเมินเกรด (Penalty System) + เควส + UI จบด่าน
/// </summary>
public class LevelRatingManager : MonoBehaviour
{
    public static LevelRatingManager Instance { get; private set; }

    [Header("Score Settings")]
    public int maxScore = 10000;
    public int currentScore;

    [Header("Penalty Settings")]
    public int damagePenalty = 500;
    public int missedEnemyPenalty = 1000; // หักเมื่อฆ่าศัตรูไม่ครบ

    [Header("Katana Zero Countdown Timer (ระบบเวลากดดัน)")]
    [Tooltip("เปิดใช้ระบบจับเวลานับถอยหลัง")]
    public bool enableCountdownTimer = true;

    [Tooltip("เวลาจำกัดของด่าน (วินาที) เช่น 45 หรือ 60 วิ")]
    public float levelTimeLimit = 60f;

    [Tooltip("โดนยิงแล้วหักเวลาลงกี่วินาที")]
    public float timePenaltyOnHit = 5f;

    [Tooltip("เวลาที่เหลือต่ำกว่ากี่วินาที จึงจะเริ่มกระพริบเตือน (ค่าเริ่มต้น 20 วินาที ปรับได้ตามใจชอบ)")]
    public float warningTimeThreshold = 20f;

    public float RemainingTime { get; private set; }

    [Header("Vampiric Healing (ระบบเลือดสายบวก - เด้งทีละ 1 ขีด)")]
    [Tooltip("เลือดที่ได้เมื่อฆ่าศัตรูธรรมดา")]
    public float healPerKill = 1f;
    [Tooltip("เลือดที่ได้เมื่อยิงจุดตาย (Critical)")]
    public float healPerCriticalKill = 1f;

    [Header("Level Stats")]
    private float timeElapsed = 0f;
    private int totalEnemiesInLevel = 0;
    public bool isLevelActive { get; private set; } = true;
    public int TotalKills { get; private set; } = 0;

    [Header("Custom UI Bindings")]
    [Tooltip("Panel หน้าสรุปผลตอนจบด่าน (ซ่อนไว้ตอนเริ่มเกม)")]
    public GameObject victoryPanel;
    
    [Tooltip("Text แสดงเวลาที่ใช้ไป")]
    public TMPro.TMP_Text timeText;
    
    [Tooltip("Text แสดงจำนวนศัตรูที่จัดการได้ (เช่น 5/5)")]
    public TMPro.TMP_Text enemiesKilledText;
    
    [Tooltip("Text แสดงคะแนนสุทธิ")]
    public TMPro.TMP_Text finalScoreText;
    
    [Tooltip("Text แสดงตัวอักษร Rank (S, A, B, C)")]
    public TMPro.TMP_Text finalGradeText;
    
    [Tooltip("Panel หน้า Game Over")]
    public GameObject gameOverPanel;

    // Events
    public event Action<int> OnScoreChanged;
    public event Action<string> OnGradeCalculated;
    public event Action<float> OnRemainingTimeChanged;
    public event Action<float> OnTimePenaltyApplied;
    public event Action OnTimeOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentScore = maxScore;
            RemainingTime = levelTimeLimit;
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // นับจำนวนศัตรูทั้งหมดในฉากตอนเริ่มเกม
        FriedEggEnemy[] enemies = FindObjectsByType<FriedEggEnemy>(FindObjectsSortMode.None);
        totalEnemiesInLevel = enemies.Length;
    }

    private void Update()
    {
        if (isLevelActive)
        {
            timeElapsed += Time.deltaTime;

            // ระบบนับถอยหลัง Katana Zero Timer
            if (enableCountdownTimer)
            {
                RemainingTime -= Time.deltaTime;
                OnRemainingTimeChanged?.Invoke(RemainingTime);

                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    TriggerTimeOver();
                }
            }
        }
    }

    /// <summary>
    /// หักเวลาจำกัดเมื่อโดนยิง (Katana Zero Time Penalty)
    /// </summary>
    public void ApplyTimePenalty()
    {
        if (!isLevelActive || !enableCountdownTimer) return;

        RemainingTime = Mathf.Max(0f, RemainingTime - timePenaltyOnHit);
        OnRemainingTimeChanged?.Invoke(RemainingTime);
        OnTimePenaltyApplied?.Invoke(timePenaltyOnHit);
        Debug.Log($"[Timer] โดนยิง! เวลาลด {timePenaltyOnHit}s เหลือ: {RemainingTime:F2}s");

        if (RemainingTime <= 0f)
        {
            TriggerTimeOver();
        }
    }

    /// <summary>
    /// เรียกเมื่อหมดเวลา
    /// </summary>
    public void TriggerTimeOver()
    {
        if (!isLevelActive) return;
        isLevelActive = false;

        Debug.Log("[Timer] หมดเวลา! TIME OVER -> GAME OVER");
        OnTimeOver?.Invoke();

        // หยุด Slow-Mo ก่อน
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.DeactivateSlowMotion();
        }

        // แจ้ง Player ให้ตาย
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player != null && player.IsAlive)
        {
            player.Die();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.GameOver);
        }

        // เล่นเพลง Game Over
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverBGM != null && AudioManager.Instance.gameOverBGM.clip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverBGM);
        }
    }

    public void ApplyPenalty(string reason, int penaltyAmount)
    {
        currentScore -= penaltyAmount;
        if (currentScore < 0) currentScore = 0;
        Debug.Log($"[LevelRating] Penalty! -{penaltyAmount} from {reason}. Current Score: {currentScore}");
        OnScoreChanged?.Invoke(currentScore);
    }

    public void RegisterKill(bool isCritical)
    {
        TotalKills++;
        Debug.Log($"[LevelRating] Enemy killed! Total: {TotalKills} / {totalEnemiesInLevel}");

        // --- ระบบฟื้นฟูเลือดสายบวก (Vampiric / DOOM Style) ---
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            float healAmount = isCritical ? healPerCriticalKill : healPerKill; 
            playerHealth.Heal(healAmount);
        }
    }

    public void PlayerDied()
    {
        isLevelActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // เล่นเพลง Game Over
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverBGM != null && AudioManager.Instance.gameOverBGM.clip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverBGM);
        }
    }

    public string CalculateFinalGrade()
    {
        string grade = "D";
        if (currentScore >= 8000) grade = "S";
        else if (currentScore >= 6000) grade = "A";
        else if (currentScore >= 4000) grade = "B";
        else grade = "C";

        OnGradeCalculated?.Invoke(grade);
        return grade;
    }

    /// <summary>
    /// เรียกเมื่อเดินเข้าเส้นชัย (LevelExit)
    /// </summary>
    public void ShowVictorySummary()
    {
        isLevelActive = false; // หยุดเวลา

        // บังคับหยุด Slow-Mo ก่อน เพื่อไม่ให้ขอบจอค้างอยู่
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.DeactivateSlowMotion();
        }

        Time.timeScale = 0f; // หยุดเกม

        // เล่นเพลงหน้าชนะ
        if (AudioManager.Instance != null && AudioManager.Instance.victoryBGM != null && AudioManager.Instance.victoryBGM.clip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.victoryBGM);
        }

        // ตรวจสอบเควส: ฆ่าศัตรูครบไหม?
        int kills = TotalKills;
        if (kills < totalEnemiesInLevel)
        {
            int missed = totalEnemiesInLevel - kills;
            ApplyPenalty("Missed Enemies", missedEnemyPenalty * missed);
        }

        string finalGrade = CalculateFinalGrade();

        // อัปเดต UI 
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            // แปลงเวลาเป็น นาที:วินาที
            int minutes = Mathf.FloorToInt(timeElapsed / 60F);
            int seconds = Mathf.FloorToInt(timeElapsed - minutes * 60);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timeText != null) timeText.text = timeString;
            if (enemiesKilledText != null) enemiesKilledText.text = $"{kills} / {totalEnemiesInLevel}";
            if (finalScoreText != null) finalScoreText.text = currentScore.ToString();
            
            if (finalGradeText != null)
            {
                finalGradeText.text = finalGrade;
                // เปลี่ยนสีตามเกรด
                if (finalGrade == "S") finalGradeText.color = Color.red;
                else if (finalGrade == "A") finalGradeText.color = Color.green;
                else if (finalGrade == "B") finalGradeText.color = Color.blue;
                else if (finalGrade == "C") finalGradeText.color = new Color(1f, 0.5f, 0f);
                else finalGradeText.color = Color.gray;
            }
        }
        else
        {
            Debug.LogWarning("Victory Panel ยังไม่ได้ใส่ใน LevelRatingManager!");
        }
    }

    // ฟังก์ชันสำหรับปุ่ม
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel(string nextSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}
