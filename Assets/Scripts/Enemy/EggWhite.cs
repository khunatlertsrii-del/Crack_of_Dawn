// ============================================================
// EggWhite.cs — เกราะไข่ขาว (Visual + Effect Component)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// Component สำหรับ Visual ของ "ไข่ขาว" (Body Zone)
/// - ใส่ไว้บน Child Object ที่แสดง Sprite ไข่ขาว
/// - จัดการ Visual Effects ตอนไข่ขาวโดนยิง
/// 
/// เอฟเฟกต์:
/// - Flash สีแดงตอนโดนยิง
/// - Wobble (สั่น) ตอนโดนยิง
/// </summary>
public class EggWhite : MonoBehaviour
{
    [Header("Hit Feedback")]
    [Tooltip("สีตอนโดนยิง (Flash)")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.5f, 0.5f);

    [Tooltip("ระยะเวลา Flash")]
    [SerializeField] private float flashDuration = 0.1f;

    [Tooltip("ความแรง Wobble")]
    [SerializeField] private float wobbleIntensity = 5f;

    [Tooltip("ระยะเวลา Wobble")]
    [SerializeField] private float wobbleDuration = 0.2f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Color originalColor;
    private Vector3 originalPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// เรียกเมื่อไข่ขาวโดนยิง — แสดง Visual Feedback
    /// </summary>
    public void OnBodyHit(Vector2 hitPoint)
    {
        StartCoroutine(FlashCoroutine());
        StartCoroutine(WobbleCoroutine());
    }

    /// <summary>Flash สีแดงแล้วกลับสีเดิม</summary>
    private IEnumerator FlashCoroutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    /// <summary>สั่นตัวเบาๆ ตอนโดนยิง</summary>
    private IEnumerator WobbleCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < wobbleDuration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / wobbleDuration);
            float offsetX = Mathf.Sin(elapsed * wobbleIntensity * 50f) * 0.02f * decay;
            float offsetY = Mathf.Cos(elapsed * wobbleIntensity * 40f) * 0.01f * decay;

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
