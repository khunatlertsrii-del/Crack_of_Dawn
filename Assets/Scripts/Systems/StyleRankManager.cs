using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public enum StyleRank
{
    D, // Dull
    C, // Cooked
    B, // Bussin'
    A, // Appetizing
    S, // Smokin'
    SS, // Sizzlin'
    SSS // Smokin' Sriracha Style!
}

/// <summary>
/// ระบบประเมินความเท่ (Style Rank) แบบ Devil May Cry 
/// สะสมแต้ม Style จากการต่อสู้ต่อเนื่องเพื่อเลื่อนแรงค์ให้ถึง SSS!
/// </summary>
public class StyleRankManager : MonoBehaviour
{
    public static StyleRankManager Instance { get; private set; }

    [Header("Style Thresholds")]
    public float thresholdC = 30f;
    public float thresholdB = 80f;
    public float thresholdA = 150f;
    public float thresholdS = 250f;
    public float thresholdSS = 400f;
    public float thresholdSSS = 600f;

    [Header("Points Configuration")]
    public float killPoints = 10f;
    public float criticalBonus = 20f;
    public float decayRate = 15f; // ลดลงกี่หน่วยต่อวินาที

    // ===================== State =====================
    public float CurrentStylePoints { get; private set; } = 0f;
    public StyleRank CurrentRank { get; private set; } = StyleRank.D;
    public StyleRank PeakRank { get; private set; } = StyleRank.D; // แรงค์สูงสุดที่เคยทำได้ในรอบนี้

    public event Action<StyleRank> OnRankChanged;

    [Header("UI")]
    [Tooltip("ลาก TextMeshPro ที่ใช้แสดง Rank มาใส่ตรงนี้")]
    [SerializeField] private TextMeshProUGUI rankText;
    
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (rankText != null)
        {
            baseScale = rankText.transform.localScale;
            rankText.text = ""; // ซ่อนตอนเริ่มเกม
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (CurrentStylePoints > 0)
        {
            CurrentStylePoints -= decayRate * Time.deltaTime;
            CurrentStylePoints = Mathf.Max(0, CurrentStylePoints);
            UpdateRankFromPoints();
        }

        UpdateUIWobble();
    }

    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    /// <summary>เรียกเมื่อฆ่าศัตรู หรือทำท่ายาก</summary>
    public void AddStyle(bool isCritical)
    {
        float pointsToAdd = killPoints;
        if (isCritical) pointsToAdd += criticalBonus;

        CurrentStylePoints += pointsToAdd;
        UpdateRankFromPoints();
    }

    /// <summary>สรุปผลตอนจบด่าน แจกดาวมิชลิน</summary>
    public int GetMichelinStars()
    {
        if (PeakRank >= StyleRank.SSS) return 3;
        if (PeakRank >= StyleRank.S) return 2;
        if (PeakRank >= StyleRank.B) return 1;
        return 0;
    }

    // =========================================================
    // INTERNAL
    // =========================================================

    private void UpdateRankFromPoints()
    {
        StyleRank newRank = StyleRank.D;
        if (CurrentStylePoints >= thresholdSSS) newRank = StyleRank.SSS;
        else if (CurrentStylePoints >= thresholdSS) newRank = StyleRank.SS;
        else if (CurrentStylePoints >= thresholdS) newRank = StyleRank.S;
        else if (CurrentStylePoints >= thresholdA) newRank = StyleRank.A;
        else if (CurrentStylePoints >= thresholdB) newRank = StyleRank.B;
        else if (CurrentStylePoints >= thresholdC) newRank = StyleRank.C;

        if (newRank != CurrentRank)
        {
            CurrentRank = newRank;
            
            // เก็บ Peak Rank ไว้แจกดาวตอนจบ
            if (CurrentRank > PeakRank) PeakRank = CurrentRank;

            OnRankChanged?.Invoke(CurrentRank);
            UpdateRankVisuals();
        }
    }

    // =========================================================
    // UI VISUALS
    // =========================================================

    private void UpdateUIWobble()
    {
        if (rankText == null || CurrentRank == StyleRank.D) return;

        // ความแรงสั่นขึ้นอยู่กับ Rank (C=1, B=2, A=3, S=4, SS=5, SSS=6)
        float intensity = (int)CurrentRank * 0.1f; 
        
        // หมุนส่ายไปมา
        float angle = Mathf.Sin(Time.time * 20f) * intensity * 5f;
        rankText.transform.localRotation = Quaternion.Euler(0, 0, angle);

        // ย่อขยายสั่นๆ
        // ไม่ต้องเช็ค scaleBase แล้วเพราะเรามี baseScale ที่แท้จริง
        float scaleWobble = 1f + Mathf.Sin(Time.time * 15f) * intensity * 0.05f;
        rankText.transform.localScale = baseScale * scaleWobble;

        // วาวแสง (สำหรับ S ขึ้นไป)
        if (CurrentRank >= StyleRank.S)
        {
            Color c = rankText.color;
            c.a = 0.7f + Mathf.PingPong(Time.time * 10f, 0.3f);
            rankText.color = c;
        }
    }

    private void UpdateRankVisuals()
    {
        if (rankText == null) return;

        if (CurrentRank == StyleRank.D)
        {
            rankText.text = ""; // ซ่อนถ้าแรงค์ต่ำเกินไป (Dull)
            return;
        }

        rankText.text = CurrentRank.ToString();
        
        switch (CurrentRank)
        {
            case StyleRank.C: rankText.color = Color.white; break;
            case StyleRank.B: rankText.color = Color.green; break;
            case StyleRank.A: rankText.color = Color.cyan; break;
            case StyleRank.S: rankText.color = Color.magenta; break;
            case StyleRank.SS: rankText.color = Color.yellow; break;
            case StyleRank.SSS: rankText.color = Color.red; break;
        }

        // ทำอนิเมชั่นเด้งเบาๆ
        StartCoroutine(PopTextRoutine());
    }

    private IEnumerator PopTextRoutine()
    {
        rankText.transform.localScale = baseScale * 1.5f;
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            rankText.transform.localScale = Vector3.Lerp(baseScale * 1.5f, baseScale, t / 0.2f);
            yield return null;
        }
        rankText.transform.localScale = baseScale;
    }
}
