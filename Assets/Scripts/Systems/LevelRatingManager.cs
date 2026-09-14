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

    [Header("Level Stats")]
    private float timeElapsed = 0f;
    private int totalEnemiesInLevel = 0;
    private bool isLevelActive = true;

    [Header("Custom UI Bindings")]
    [Tooltip("Panel หน้าสรุปผลตอนจบด่าน (ซ่อนไว้ตอนเริ่มเกม)")]
    public GameObject victoryPanel;
    
    [Tooltip("Text แสดงเวลาที่ใช้ไป")]
    public Text timeText;
    
    [Tooltip("Text แสดงจำนวนศัตรูที่จัดการได้ (เช่น 5/5)")]
    public Text enemiesKilledText;
    
    [Tooltip("Text แสดงคะแนนสุทธิ")]
    public Text finalScoreText;
    
    [Tooltip("Text แสดงตัวอักษร Rank (S, A, B, C)")]
    public Text finalGradeText;
    
    // Events
    public event Action<int> OnScoreChanged;
    public event Action<string> OnGradeCalculated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentScore = maxScore;
            if (victoryPanel != null) victoryPanel.SetActive(false);
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
        }
    }

    public void ApplyPenalty(string reason, int penaltyAmount)
    {
        currentScore -= penaltyAmount;
        if (currentScore < 0) currentScore = 0;
        Debug.Log($"[LevelRating] Penalty! -{penaltyAmount} from {reason}. Current Score: {currentScore}");
        OnScoreChanged?.Invoke(currentScore);
    }

    public string CalculateFinalGrade()
    {
        string grade = "D";
        if (currentScore >= 9500) grade = "S";
        else if (currentScore >= 8000) grade = "A";
        else if (currentScore >= 6000) grade = "B";
        else if (currentScore >= 4000) grade = "C";

        OnGradeCalculated?.Invoke(grade);
        return grade;
    }

    /// <summary>
    /// เรียกเมื่อเดินเข้าเส้นชัย (LevelExit)
    /// </summary>
    public void ShowVictorySummary()
    {
        isLevelActive = false; // หยุดเวลา

        // ตรวจสอบเควส: ฆ่าศัตรูครบไหม?
        int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
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

            if (timeText != null) timeText.text = $"Time: {timeString}";
            if (enemiesKilledText != null) enemiesKilledText.text = $"Enemies Killed: {kills} / {totalEnemiesInLevel}";
            if (finalScoreText != null) finalScoreText.text = $"Score: {currentScore}";
            
            if (finalGradeText != null)
            {
                finalGradeText.text = finalGrade;
                // เปลี่ยนสีตามเกรด
                if (finalGrade == "S") finalGradeText.color = Color.yellow;
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel(string nextSceneName)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
