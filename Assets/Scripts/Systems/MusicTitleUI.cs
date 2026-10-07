using UnityEngine;
using TMPro;
using System.Collections;

public class MusicTitleUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;       // Text (TMP) สำหรับแสดงชื่อเพลง
    public GameObject uiPanel;   // Panel หรือพื้นหลังของข้อความ (ถ้ามี)

    [Header("Animation Settings")]
    public float displayDuration = 3f;  // โชว์ค้างไว้กี่วินาที
    
    [Tooltip("ความเร็วในการเลื่อนและเฟด")]
    public float animationSpeed = 5f;        
    
    [Tooltip("ระยะที่เลื่อนออกไปซ่อนตัว (เช่น X=300 คือซ่อนไปทางขวา, Y=-200 คือซ่อนลงล่าง)")]
    public Vector2 slideOffset = new Vector2(300f, 0f);

    private Coroutine displayCoroutine;
    private CanvasGroup canvasGroup;
    private RectTransform targetRect;
    
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        // 1. จัดการ Canvas Group (สำหรับเฟดโปร่งใส)
        canvasGroup = uiPanel != null ? uiPanel.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = (uiPanel != null ? uiPanel : gameObject).AddComponent<CanvasGroup>();
        }
        
        // 2. จัดการ RectTransform (สำหรับเลื่อน)
        targetRect = uiPanel != null ? uiPanel.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        
        // 3. จำตำแหน่งเริ่มต้น (ตอนโชว์) และคำนวณตำแหน่งตอนซ่อน
        if (targetRect != null)
        {
            visiblePosition = targetRect.anchoredPosition;
            hiddenPosition = visiblePosition + slideOffset;
            
            // ย้ายไปซ่อนไว้ก่อนตั้งแต่เริ่ม
            targetRect.anchoredPosition = hiddenPosition;
        }
        
        canvasGroup.alpha = 0f; // ซ่อนไว้ก่อนตอนเริ่ม
    }

    private void Start()
    {
        // สมัครรับ Event จาก AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicChanged += ShowMusicTitle;
        }
    }

    private void OnDestroy()
    {
        // ยกเลิกรับ Event ป้องกัน Error
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicChanged -= ShowMusicTitle;
        }
    }

    // ฟังก์ชันที่ถูกเรียกอัตโนมัติเมื่อเพลงเปลี่ยน
    private void ShowMusicTitle(string trackName)
    {
        if (titleText != null)
        {
            titleText.text = trackName;
        }

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        displayCoroutine = StartCoroutine(AnimateUI());
    }

    private IEnumerator AnimateUI()
    {
        // 1. เลื่อนเข้าและเฟดเข้า (Slide In & Fade In)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * animationSpeed;
            float easeT = EaseOutQuart(Mathf.Clamp01(t)); // ทำให้ลื่นๆ เด้งนิดๆ ตอนจบ
            
            canvasGroup.alpha = easeT;
            if (targetRect != null)
            {
                targetRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, easeT);
            }
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (targetRect != null) targetRect.anchoredPosition = visiblePosition;

        // 2. รอ (Wait)
        yield return new WaitForSecondsRealtime(displayDuration);

        // 3. เลื่อนออกและเฟดออก (Slide Out & Fade Out)
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * animationSpeed;
            float easeT = EaseInQuart(Mathf.Clamp01(t)); // ทำให้เริ่มช้าไปเร็วตอนออก
            
            canvasGroup.alpha = 1f - easeT;
            if (targetRect != null)
            {
                targetRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, easeT);
            }
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        if (targetRect != null) targetRect.anchoredPosition = hiddenPosition;
    }

    // ฟังก์ชันช่วยคำนวณความสมูท (Easing)
    private float EaseOutQuart(float x)
    {
        return 1f - Mathf.Pow(1f - x, 4f);
    }
    
    private float EaseInQuart(float x)
    {
        return x * x * x * x;
    }
}
