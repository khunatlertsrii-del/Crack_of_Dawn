using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จัดการ Custom Cursor / Crosshair
/// รองรับทั้งโหมด Software Cursor (UI Image ตามเมาส์ ซึ่งเสถียร 100% บน WebGL / itch.io)
/// และโหมด Hardware Cursor ปกติ
/// </summary>
public class CustomCursor : MonoBehaviour
{
    [Header("=== ตั้งค่ารูปเป้าเล็ง ===")]
    [Tooltip("ลากรูปลูกศรหรือเป้ายิง (Crosshair) มาใส่ตรงนี้")]
    public Texture2D cursorTexture;

    [Tooltip("หรือลากรูป Sprite เป้าเล็งมาใส่ตรงนี้ (ถ้าใส่จะใช้ระบบ UI Crosshair ที่คมชัดและขนาดเป๊ะทุกแพลตฟอร์ม)")]
    public Sprite cursorSprite;

    [Header("=== การแสดงผลบน WebGL / หน้าจอ ===")]
    [Tooltip("ใช้ Software Cursor (สร้าง UI วิ่งตามเมาส์) แก้ปัญหาเมาส์ขยายใหญ่ผิดปกติบน WebGL / itch.io 100%")]
    public bool useSoftwareCursorOnWebGL = true;

    [Tooltip("ขนาดของเป้าเล็งบนหน้าจอ (เช่น 32x32 หรือ 40x40 พิกเซล)")]
    public Vector2 cursorSize = new Vector2(36f, 36f);

    [Tooltip("จุดกึ่งกลางของเมาส์ (ถ้าเป็น Hardware Cursor)")]
    public Vector2 hotSpot = Vector2.zero;

    [Tooltip("ล็อคเมาส์ให้อยู่ในกรอบหน้าต่างเกม")]
    public bool lockCursor = false;

    // Private UI references
    private RectTransform cursorRectTransform;
    private Image cursorImage;
    private Canvas cursorCanvas;
    private bool isSoftwareCursorActive = false;

    private void Awake()
    {
        // ถ้ามี Sprite หรือตั้งให้ใช้ Software Cursor บน WebGL (หรือทุกที่ถ้าไม่มี Hardware Cursor)
        bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
        if (isWebGL && useSoftwareCursorOnWebGL)
        {
            SetupSoftwareCursor();
        }
        else
        {
            ApplyHardwareCursor();
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    private void Start()
    {
        // ย้ำอีกครั้งตอน Start เพื่อความชัวร์
        if (isSoftwareCursorActive)
        {
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (isSoftwareCursorActive && cursorRectTransform != null)
        {
            // ซ่อนเมาส์มาตรฐานของเบราว์เซอร์/วินโดวส์
            if (Cursor.visible)
            {
                Cursor.visible = false;
            }

            // ขยับ UI เป้าเล็งตามตำแหน่งเมาส์แบบเรียลไทม์
            Vector2 mousePos = Input.mousePosition;
            cursorRectTransform.position = mousePos;
        }
    }

    private void OnDisable()
    {
        // คืนค่าเมาส์ปกติเมื่อปิดสคริปต์
        Cursor.visible = true;
        if (cursorCanvas != null)
        {
            cursorCanvas.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (isSoftwareCursorActive)
        {
            Cursor.visible = false;
            if (cursorCanvas != null)
            {
                cursorCanvas.gameObject.SetActive(true);
            }
        }
    }

    private void ApplyHardwareCursor()
    {
        if (cursorTexture != null)
        {
            Vector2 actualHotspot = hotSpot;
            if (actualHotspot == Vector2.zero)
            {
                actualHotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
            }
            Cursor.SetCursor(cursorTexture, actualHotspot, CursorMode.Auto);
        }
        Cursor.visible = true;
    }

    private void SetupSoftwareCursor()
    {
        isSoftwareCursorActive = true;
        Cursor.visible = false;

        // ดึง Sprite ถ้ามีแต่ Texture2D ให้สร้าง Sprite อัตโนมัติจาก Texture2D
        Sprite displaySprite = cursorSprite;
        if (displaySprite == null && cursorTexture != null)
        {
            displaySprite = Sprite.Create(
                cursorTexture, 
                new Rect(0, 0, cursorTexture.width, cursorTexture.height), 
                new Vector2(0.5f, 0.5f), 
                100f
            );
        }

        if (displaySprite == null)
        {
            ApplyHardwareCursor();
            return;
        }

        // สร้าง Canvas แยกเฉพาะสำหรับ Cursor ที่อยู่บนสุดเสมอ (Sort Order 9999)
        GameObject canvasObj = new GameObject("CustomCursor_Canvas");
        cursorCanvas = canvasObj.AddComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.sortingOrder = 9999; // บนสุดของทุกอย่าง
        canvasObj.AddComponent<CanvasScaler>();

        // สร้าง Image สำหรับ Cursor
        GameObject cursorObj = new GameObject("Crosshair_Pointer");
        cursorObj.transform.SetParent(canvasObj.transform, false);

        cursorRectTransform = cursorObj.AddComponent<RectTransform>();
        cursorRectTransform.sizeDelta = cursorSize;
        cursorRectTransform.pivot = new Vector2(0.5f, 0.5f); // กึ่งกลางเป้าเป๊ะๆ

        cursorImage = cursorObj.AddComponent<Image>();
        cursorImage.sprite = displaySprite;
        cursorImage.raycastTarget = false; // ไม่บล็อกการคลิกปุ่ม UI ใดๆ ทั้งสิ้น!
        cursorImage.color = Color.white;
    }
}
