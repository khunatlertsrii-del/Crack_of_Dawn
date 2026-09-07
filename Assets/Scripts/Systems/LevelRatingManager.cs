using UnityEngine;
using System;
using UnityEngine.UI;

/// <summary>
/// ระบบประเมินเกรด (Penalty System) 
/// เริ่มที่ 10,000 คะแนน ถ้ายิ่งทำพลาดคะแนนจะยิ่งลด
/// S = 9500+, A = 8000+, B = 6000+, C = 4000+, D = ต่ำกว่า 4000
/// </summary>
public class LevelRatingManager : MonoBehaviour
{
    public static LevelRatingManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("คะแนนเริ่มต้นสูงสุด")]
    public int maxScore = 10000;
    
    [Tooltip("คะแนนปัจจุบัน")]
    public int currentScore;

    [Header("Penalty Settings")]
    [Tooltip("หักคะแนนเมื่อโดนดาเมจ 1 ครั้ง")]
    public int damagePenalty = 500;
    
    [Tooltip("หักคะแนนเมื่อทำเควสล้มเหลว")]
    public int questFailedPenalty = 2000;

    [Header("UI (Optional)")]
    [Tooltip("Text แสดงคะแนน")]
    public Text scoreText;
    [Tooltip("Text แสดงเกรด (S, A, B, C, D)")]
    public Text gradeText;

    // Events
    public event Action<int> OnScoreChanged;
    public event Action<string> OnGradeCalculated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentScore = maxScore;
            UpdateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// หักคะแนน และระบุสาเหตุ
    /// </summary>
    public void ApplyPenalty(string reason, int penaltyAmount)
    {
        currentScore -= penaltyAmount;
        if (currentScore < 0) currentScore = 0;

        Debug.Log($"[LevelRating] Penalty! -{penaltyAmount} from {reason}. Current Score: {currentScore}");

        // ถ้ามีเอฟเฟกต์โชว์ตัวเลขลบ สามารถใส่ตรงนี้ได้ (เช่น Floating Text)

        OnScoreChanged?.Invoke(currentScore);
        UpdateUI();
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้ตอนจบด่านเพื่อคำนวณเกรด
    /// </summary>
    public string CalculateFinalGrade()
    {
        string grade = "D";

        if (currentScore >= 9500) grade = "S";
        else if (currentScore >= 8000) grade = "A";
        else if (currentScore >= 6000) grade = "B";
        else if (currentScore >= 4000) grade = "C";

        Debug.Log($"[LevelRating] Final Score: {currentScore} -> Grade: {grade}");
        
        OnGradeCalculated?.Invoke(grade);
        
        if (gradeText != null)
        {
            gradeText.text = grade;
            // เปลี่ยนสีตามเกรด
            if (grade == "S") gradeText.color = Color.yellow;
            else if (grade == "A") gradeText.color = Color.green;
            else if (grade == "B") gradeText.color = Color.blue;
            else if (grade == "C") gradeText.color = new Color(1f, 0.5f, 0f); // Orange
            else gradeText.color = Color.gray;
        }

        return grade;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore.ToString();
        }
    }

    /// <summary>
    /// สร้าง UI สรุปผลตอนจบด่าน (Placeholder)
    /// </summary>
    public void ShowVictorySummary()
    {
        string finalGrade = CalculateFinalGrade();

        // สร้าง Canvas ชั่วคราวเพื่อแสดงผล
        GameObject canvasObj = new GameObject("VictoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<GraphicRaycaster>();

        // สร้างพื้นหลังดำโปร่งแสง
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // ข้อความจบด่าน
        CreateText(canvasObj.transform, "LEVEL COMPLETE!", 50, new Vector2(0, 150), Color.white);
        
        // คะแนน
        CreateText(canvasObj.transform, $"FINAL SCORE: {currentScore}", 40, new Vector2(0, 50), Color.white);

        // เกรด
        Color gradeColor = Color.white;
        if (finalGrade == "S") gradeColor = Color.yellow;
        else if (finalGrade == "A") gradeColor = Color.green;
        else if (finalGrade == "B") gradeColor = Color.blue;
        else if (finalGrade == "C") gradeColor = new Color(1f, 0.5f, 0f);
        else gradeColor = Color.gray;

        CreateText(canvasObj.transform, $"GRADE: {finalGrade}", 80, new Vector2(0, -50), gradeColor);

        // คำแนะนำให้กดออก
        CreateText(canvasObj.transform, "Press ESC to restart / quit", 20, new Vector2(0, -200), Color.gray);
    }

    private void CreateText(Transform parent, string text, int fontSize, Vector2 anchoredPosition, Color color)
    {
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(parent, false);
        Text t = txtObj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 100);
        rt.anchoredPosition = anchoredPosition;
    }
}
