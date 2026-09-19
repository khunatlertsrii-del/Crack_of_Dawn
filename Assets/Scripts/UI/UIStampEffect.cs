using UnityEngine;
using System.Collections;
using TMPro; // รองรับ TextMeshPro

/// <summary>
/// เอฟเฟกต์ประทับตรา (Stamp/Sticker) 
/// ค่อยๆ โผล่ขึ้นมาแบบกระแทกหน้าจอ (Scale ใหญ่ -> เล็ก)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIStampEffect : MonoBehaviour
{
    [Header("Stamp Settings")]
    [Tooltip("เวลาดีเลย์ก่อนที่จะเริ่มแอนิเมชัน (วินาที)")]
    public float startDelay = 0f;
    
    [Tooltip("ความเร็วในการกระแทก (ยิ่งน้อยยิ่งเร็ว)")]
    public float duration = 0.3f;

    [Tooltip("ตัวคูณขนาดเริ่มต้น (เช่น 5 เท่าของขนาดเดิม)")]
    public float startScaleMultiplier = 5f;
    
    [Tooltip("เปิดเสียงตุบตอนแสตมป์ไหม?")]
    public bool playSoundOnStamp = false;

    private RectTransform rectTransform;
    private AudioSource audioSource;
    private CanvasGroup canvasGroup;
    private UIWobble wobbleScript;
    private UIButtonEffect buttonEffectScript;
    private Vector3 originalScale; // เก็บขนาดดั้งเดิมจาก Editor

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // เก็บขนาดดั้งเดิมที่ตั้งไว้ใน Editor
        originalScale = rectTransform.localScale;

        // ลองหา AudioSource เผื่ออยากใส่เสียง
        audioSource = GetComponent<AudioSource>();

        // ลองหา UIWobble และ UIButtonEffect
        wobbleScript = GetComponent<UIWobble>();
        buttonEffectScript = GetComponent<UIButtonEffect>();

        // ซ่อนไว้ก่อน
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false; // ปิดการกดปุ่มไปก่อน
        // ลบการย่อขยายตรงนี้ออก เพื่อไม่ให้ชนกับ Awake ของ UIWobble
    }

    private void OnEnable()
    {
        // ปิดสคริปต์สเกลอื่นๆ ไว้ก่อนช่วงที่กำลัง Stamp เพื่อไม่ให้แย่งกันปรับขนาด
        if (wobbleScript != null) wobbleScript.enabled = false;
        if (buttonEffectScript != null) buttonEffectScript.enabled = false;
        
        // มั่นใจว่าปุ่มกดไม่ได้
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        
        // เริ่มแอนิเมชันทุกครั้งที่เปิด UI นี้
        StopAllCoroutines();
        StartCoroutine(StampRoutine());
    }

    private IEnumerator StampRoutine()
    {
        // ซ่อนไว้ก่อน
        canvasGroup.alpha = 0f;
        
        Vector3 startScale = originalScale * startScaleMultiplier;
        rectTransform.localScale = startScale;

        // รอดีเลย์
        if (startDelay > 0)
        {
            yield return new WaitForSecondsRealtime(startDelay); // ใช้ Realtime เพราะเกมอาจจะ Pause อยู่
        }

        // เริ่มแสดง
        canvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // ใช้สมการ Ease Out Back (เด้งนิดนึงตอนจบ)
            float easeT = EaseOutBack(t);

            rectTransform.localScale = Vector3.LerpUnclamped(startScale, originalScale, easeT);
            
            // เฟดความชัด (แค่ช่วงสั้นๆ ตอนเริ่ม)
            canvasGroup.alpha = Mathf.Clamp01(t * 2f);

            yield return null;
        }

        // บังคับจบให้เป๊ะตามขนาดดั้งเดิม
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true; // เปิดให้กดปุ่มได้แล้ว!

        // เปิด Wobble และ ButtonEffect กลับมาทำงาน
        if (wobbleScript != null)
        {
            wobbleScript.enabled = true;
        }
        if (buttonEffectScript != null)
        {
            buttonEffectScript.enabled = true;
        }

        // เล่นเสียงกระแทก (ถ้ามี)
        if (playSoundOnStamp && audioSource != null)
        {
            audioSource.Play();
        }
        else if (playSoundOnStamp)
        {
            // หรือถ้ามี ScreenEffectManager ก็ให้จอสั่นนิดๆ
            ScreenEffectManager.Instance?.TriggerAdrenaline(); // ยืมเอฟเฟกต์สั่นมาใช้
        }
    }

    // สมการคณิตศาสตร์สำหรับทำให้เด้งดึ๋งตอนจบ
    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
