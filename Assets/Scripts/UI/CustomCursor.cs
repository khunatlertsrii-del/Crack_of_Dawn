using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [Tooltip("ลากรูปลูกศรหรือเป้ายิง (Crosshair) มาใส่ตรงนี้ (ต้องปรับ Texture Type เป็น Cursor หรือ Sprite ก่อน)")]
    public Texture2D cursorTexture;
    
    [Tooltip("จุดกึ่งกลางของเมาส์ (ถ้าเป็นเป้ายิงตรงกลาง ให้ใช้ X=16, Y=16 สำหรับรูปขนาด 32x32)")]
    public Vector2 hotSpot = Vector2.zero;
    
    [Tooltip("เปิดเกมมาให้ซ่อนเมาส์มาตรฐานเลยมั้ย?")]
    public bool lockCursor = false;

    private void Start()
    {
        if (cursorTexture != null)
        {
            // เปลี่ยนรูปร่างเมาส์
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }

        if (lockCursor)
        {
            // ล็อคเมาส์ให้อยู่ในหน้าจอเกม (เหมาะสำหรับเกมยิง)
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}
