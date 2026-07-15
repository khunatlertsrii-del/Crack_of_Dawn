// ============================================================
// EggYolk.cs — จุดอ่อนไข่แดง (Visual + Effect Component)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// Component สำหรับ Visual ของ "ไข่แดง" (จุดอ่อน)
/// - ใส่ไว้บน Child Object ที่แสดง Sprite ไข่แดง
/// - จัดการ Visual Effects ตอนไข่แดงโดนยิง
/// - ข้อมูลรัศมีไข่แดงอยู่ที่ FriedEggEnemy (parent)
/// 
/// เอฟเฟกต์พิเศษ:
/// - Pulse Animation (เต้นเป็นจังหวะ เหมือนไข่แดงมีชีวิต)
/// - Explosion Effect ตอนถูกยิง
/// </summary>
public class EggYolk : MonoBehaviour
{
    [Header("Pulse Animation")]
    [Tooltip("ความเร็วการเต้น")]
    [SerializeField] private float pulseSpeed = 2f;

    [Tooltip("ขนาดที่เต้น (0.1 = เต้น 10%)")]
    [SerializeField] private float pulseAmount = 0.08f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 originalScale;
    private FriedEggEnemy parentEnemy;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        parentEnemy = GetComponentInParent<FriedEggEnemy>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        // Pulse — ไข่แดงเต้นเป็นจังหวะ (มีชีวิต!)
        if (parentEnemy != null && parentEnemy.IsAlive)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }

    /// <summary>
    /// เรียกเมื่อไข่แดงถูก Hit — สามารถเพิ่ม VFX ได้ที่นี่
    /// </summary>
    public void OnCriticalHit()
    {
        // เปลี่ยนสีเป็นสีระเบิด
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.2f, 0f); // แดงส้ม
        }

        // สเกลพอง
        transform.localScale = originalScale * 2f;
    }
}
