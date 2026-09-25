using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Effect (เอฟเฟกต์เด้งดึ๋ง)")]
    public float hoverScaleMultiplier = 1.1f; // คูณจากสเกลเดิม
    public float clickScaleMultiplier = 0.95f; 
    public float scaleSpeed = 15f; 

    [Header("Color Effect (เอฟเฟกต์เปลี่ยนสี)")]
    public bool useColorChange = true;
    [Tooltip("ถลาก Image หรือ Text มาใส่ตรงนี้เพื่อบังคับให้เปลี่ยนสีตัวนี้ (ถ้าเว้นว่างไว้ จะสุ่มเปลี่ยนตัวแรกที่เจอ)")]
    public Graphic specificGraphicToColor;
    public Color hoverColor = new Color(1f, 0.9f, 0.5f); // สีเหลืองอ่อนๆ ตอนชี้
    public Color clickColor = new Color(0.8f, 0.7f, 0.4f); // สีเข้มตอนกด
    public float colorSpeed = 10f;

    // Internal state
    private Vector3 originalScale;
    private Color originalColor;
    private Vector3 targetScale;
    private Color targetColor;
    private Graphic targetGraphic;
    
    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // ถ้าใส่แบบเฉพาะเจาะจงมา ให้ใช้ตัวนั้นเลย
        if (specificGraphicToColor != null)
        {
            targetGraphic = specificGraphicToColor;
        }
        else
        {
            // ดึง Image หรือ Text มาเปลี่ยนสี (เอาจากตัวมันเอง หรือตัวลูกก็ได้)
            targetGraphic = GetComponent<Graphic>();
            if (targetGraphic == null) targetGraphic = GetComponentInChildren<Graphic>();
        }

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
            targetColor = originalColor;
        }
    }

    private void OnEnable()
    {
        // รีเซ็ตตอนเปิดขึ้นมาใหม่ด้วย
        targetScale = originalScale;
        if (useColorChange) targetColor = originalColor;

        transform.localScale = originalScale;
        if (targetGraphic != null && useColorChange) targetGraphic.color = originalColor;
    }

    private void OnDisable()
    {
        // รีเซ็ตค่ากลับเป็นค่าตั้งต้นเวลาปิดหน้าจอ (ป้องกันบั๊กสีค้าง/สเกลค้าง)
        targetScale = originalScale;
        if (useColorChange) targetColor = originalColor;

        transform.localScale = originalScale;
        if (targetGraphic != null && useColorChange) targetGraphic.color = originalColor;
    }

    private void Update()
    {
        // Smoothly animate scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);

        // Smoothly animate color
        if (targetGraphic != null && useColorChange)
        {
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.unscaledDeltaTime * colorSpeed);
        }
    }

    // เอาเมาส์เข้า
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
        if (useColorChange) targetColor = hoverColor;
        
        if (AudioManager.Instance != null && AudioManager.Instance.uiHoverSound != null)
        {
            AudioManager.Instance.PlayUISound(AudioManager.Instance.uiHoverSound);
        }
    }

    // เอาเมาส์ออก
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        if (useColorChange) targetColor = originalColor;
    }

    // กดเมาส์ลง
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScaleMultiplier;
        if (useColorChange) targetColor = clickColor;

        if (AudioManager.Instance != null && AudioManager.Instance.uiClickSound != null)
        {
            AudioManager.Instance.PlayUISound(AudioManager.Instance.uiClickSound);
        }
    }

    // ปล่อยเมาส์
    public void OnPointerUp(PointerEventData eventData)
    {
        // เช็คว่าเมาส์ยังอยู่บนปุ่มหรือเปล่าตอนปล่อย
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            targetScale = originalScale * hoverScaleMultiplier;
            if (useColorChange) targetColor = hoverColor;
        }
        else
        {
            targetScale = originalScale;
            if (useColorChange) targetColor = originalColor;
        }
    }
}
