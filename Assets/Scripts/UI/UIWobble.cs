using UnityEngine;

/// <summary>
/// สคริปต์สำหรับทำให้ UI ขยับไปมา หมุน ยืดหด ได้แบบมีชีวิตชีวา (Wobble Effect)
/// เอาไปแปะที่ Text Grade (ตัวอักษร S, A, B, C) หรือ UI อื่นๆ ได้เลย
/// </summary>
public class UIWobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    [Tooltip("ความเร็วในการขยับ")]
    public float speed = 5f;

    [Header("Scale (การยืดหด)")]
    [Tooltip("เปิด/ปิด การยืดหด")]
    public bool enableScale = true;
    [Tooltip("ขนาดที่ยืดหดมากสุด")]
    public float scaleAmount = 0.1f;

    [Header("Rotation (การหมุน)")]
    [Tooltip("เปิด/ปิด การหมุน")]
    public bool enableRotation = true;
    [Tooltip("องศาที่หมุนซ้ายขวามากสุด")]
    public float rotationAmount = 5f;

    [Header("Position (การลอยขึ้นลง)")]
    [Tooltip("เปิด/ปิด การลอย")]
    public bool enableFloat = true;
    [Tooltip("ระยะลอยขึ้นลงมากสุด (พิกเซล)")]
    public float floatAmount = 10f;

    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialScale = rectTransform.localScale;
            initialRotation = rectTransform.localRotation;
            initialPosition = rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        // รีเซ็ตตำแหน่งตอนเปิด UI ขึ้นมาใหม่
        if (rectTransform != null)
        {
            rectTransform.localScale = initialScale;
            rectTransform.localRotation = initialRotation;
            rectTransform.anchoredPosition = initialPosition;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        float time = Time.unscaledTime * speed; // ใช้ unscaledTime เพื่อให้ขยับได้แม้เวลาเกมหยุด (Time.timeScale = 0)

        // 1. การยืดหด (Scale) เต้นตุ้บๆ
        if (enableScale)
        {
            float scaleWobble = Mathf.Sin(time) * scaleAmount;
            rectTransform.localScale = initialScale + new Vector3(scaleWobble, scaleWobble, 0f);
        }

        // 2. การหมุน (Rotation) ดุ๊กดิ๊กซ้ายขวา
        if (enableRotation)
        {
            float rotWobble = Mathf.Cos(time * 0.8f) * rotationAmount;
            rectTransform.localRotation = initialRotation * Quaternion.Euler(0, 0, rotWobble);
        }

        // 3. การลอย (Position) เด้งขึ้นลง
        if (enableFloat)
        {
            float posWobble = Mathf.Sin(time * 1.2f) * floatAmount;
            rectTransform.anchoredPosition = initialPosition + new Vector3(0, posWobble, 0);
        }
    }
}
