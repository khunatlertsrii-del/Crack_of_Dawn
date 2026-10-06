using UnityEngine;

/// <summary>
/// ระบบแสดงปุ่ม Tutorial ลอยบนหัวผู้เล่นแบบ Katana Zero
/// เอาสคริปต์นี้ไปแปะที่ Trigger (BoxCollider2D - Is Trigger) ตามทางเดิน
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialPrompt : MonoBehaviour
{
    [Header("UI รูปปุ่ม (เช่น ปุ่ม WASD, ปุ่มเมาส์, ปุ่ม Shift)")]
    [Tooltip("ลาก GameObject ที่มี SpriteRenderer หรือ Canvas UI มาใส่ตรงนี้ (มันจะโผล่เมื่อเดินชน)")]
    public GameObject promptVisual;

    [Header("ตั้งค่าการแสดงผล")]
    [Tooltip("ให้ปุ่มลอยตามหัวผู้เล่นไหม? (ถ้าไม่ มันจะลอยอยู่กับที่ตรงจุดนี้)")]
    public bool followPlayer = true;
    [Tooltip("ความสูงของปุ่มเวลาลอยบนหัว")]
    public float heightOffset = 1.5f;
    [Tooltip("แสดงแค่ครั้งเดียวแล้วปิดทิ้งเลยใช่ไหม?")]
    public bool showOnlyOnce = true;

    private bool hasShown = false;
    private bool isPlayerInside = false;
    private Transform playerTransform;

    private void Awake()
    {
        // ซ่อนไอคอนไว้ตั้งแต่เริ่มเกม
        if (promptVisual != null)
        {
            promptVisual.SetActive(false);
        }

        // บังคับให้เป็น Trigger
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void LateUpdate()
    {
        // ถ้าผู้เล่นอยู่ในเขต และตั้งให้ไอคอนลอยตามหัว
        if (isPlayerInside && followPlayer && playerTransform != null && promptVisual != null && promptVisual.activeSelf)
        {
            // ย้ายไอคอนไปลอยบนหัวผู้เล่นแบบติดเป๊ะๆ เหมือน Katana Zero
            promptVisual.transform.position = playerTransform.position + Vector3.up * heightOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (showOnlyOnce && hasShown) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerTransform = collision.transform;

            if (promptVisual != null)
            {
                promptVisual.SetActive(true);
                // ดึงมาเป็นลูกชั่วคราว หรือถ้า FollowPlayer ก็ปล่อยแยกไว้แต่อัปเดต Position แทน (แบบ Update ดีกว่าเผื่อ Player กลับด้าน)
                if (followPlayer)
                {
                    promptVisual.transform.position = playerTransform.position + Vector3.up * heightOffset;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            
            if (promptVisual != null)
            {
                promptVisual.SetActive(false);
            }

            if (showOnlyOnce)
            {
                hasShown = true;
            }
        }
    }
}
