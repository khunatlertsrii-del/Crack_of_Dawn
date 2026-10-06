using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MusicTitleUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text titleText;       // Text สำหรับแสดงชื่อเพลง
    public GameObject uiPanel;   // Panel หรือพื้นหลังของข้อความ (ถ้ามี)

    [Header("Animation Settings")]
    public float displayDuration = 3f;  // โชว์ค้างไว้กี่วินาที
    public float fadeSpeed = 2f;        // ความเร็วในการเฟดเข้า-ออก

    private Coroutine displayCoroutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // สร้าง CanvasGroup อัตโนมัติถ้ายังไม่มี (เอาไว้ทำเฟดโปร่งใส)
        canvasGroup = uiPanel != null ? uiPanel.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = (uiPanel != null ? uiPanel : gameObject).AddComponent<CanvasGroup>();
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
        // 1. เฟดเข้า (Fade In)
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. รอ (Wait)
        yield return new WaitForSecondsRealtime(displayDuration);

        // 3. เฟดออก (Fade Out)
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
