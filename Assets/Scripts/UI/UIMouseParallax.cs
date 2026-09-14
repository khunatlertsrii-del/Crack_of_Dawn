using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class UIMouseParallax : MonoBehaviour
{
    [Tooltip("ความแรงของการขยับ (ตัวเลขยิ่งเยอะ ยิ่งขยับกว้าง) เช่น ตึกหน้าใส่ 30 ตึกหลังใส่ 10")]
    public Vector2 parallaxStrength = new Vector2(20f, 5f);
    
    [Tooltip("ความสมูทในการเลื่อนตามเมาส์ (ค่าน้อย = หนืดๆ นุ่มๆ, ค่ามาก = ตามเมาส์ทันที)")]
    public float smoothSpeed = 5f;

    [Tooltip("กลับด้านการขยับหรือไม่? (ปกติถ้าเมาส์ไปขวา ฉากหลังมักจะขยับไปซ้าย)")]
    public bool invertDirection = true;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        // 1. หาตำแหน่งเมาส์ปัจจุบัน
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }

        // 2. คำนวณว่าเมาส์อยู่ห่างจากตรงกลางจอแค่ไหน (ค่าตั้งแต่ -1 ถึง 1)
        float normalizedX = (mousePos.x - (Screen.width / 2f)) / (Screen.width / 2f);
        float normalizedY = (mousePos.y - (Screen.height / 2f)) / (Screen.height / 2f);

        // กลับทิศทางถ้าจำเป็น
        float dir = invertDirection ? -1f : 1f;

        // 3. คำนวณตำแหน่งเป้าหมาย
        float offsetX = normalizedX * parallaxStrength.x * dir;
        float offsetY = normalizedY * parallaxStrength.y * dir;
        
        targetPosition = startPosition + new Vector2(offsetX, offsetY);

        // 4. ค่อยๆ เลื่อนไปหาเป้าหมายอย่างสมูท (Lerp)
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            targetPosition, 
            Time.unscaledDeltaTime * smoothSpeed
        );
    }
}
