// ============================================================
// BloodParticle.cs — เศษไข่กระจาย และระบบ Decal แหมะติดกำแพง
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// แปะสคริปต์นี้ไว้ที่ Particle เศษไข่ตอนกระเด็น
/// เมื่อมันชนกำแพง จะเปลี่ยนตัวเองเป็นคราบเลือด (Decal)
/// </summary>
public class BloodParticle : MonoBehaviour
{
    private bool hasSplat = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (hasSplat) return;

        // ไม่ให้ติดกับผู้เล่นหรือศัตรู หรือเลือดด้วยกันเอง
        if (collider.CompareTag("Player") || collider.CompareTag("Enemy") || collider.GetComponent<BloodParticle>() != null)
        {
            return;
        }

        // ชนกำแพง/พื้น! → หยุดการเคลื่อนไหว
        hasSplat = true;
        
        // หมุนคราบเลือดตามทิศทางที่พุ่งมาก่อนหยุด
        float angle = 0f;
        if (rb != null)
        {
            angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            rb.simulated = false; // ปิดระบบฟิสิกส์
            rb.linearVelocity = Vector2.zero;
        }

        // ทำให้บี้แบนนิดนึง เหมือนของเหลวกระแทกกำแพง
        transform.localScale = new Vector3(
            transform.localScale.x * Random.Range(1.2f, 1.8f),
            transform.localScale.y * Random.Range(0.6f, 0.9f),
            1f
        );

        transform.rotation = Quaternion.Euler(0, 0, angle);

        // ย้ายไปอยู่เลเยอร์หลังสุด (ฉากหลังขาวดำ) เพื่อไม่ให้บังตัวละคร
        if (sr != null)
        {
            sr.sortingOrder = -5;
        }

        // ยกเลิกการ Destroy อัตโนมัติที่เคยเซ็ตไว้ตอนแรก 
        // ให้ซากติดอยู่นานๆ ตามที่ตั้งค่าไว้ใน Inspector (ค่าเริ่มต้น 20 วิ)
        CancelInvoke();
        float duration = 20f;
        if (HitEffect.Instance != null)
        {
            duration = HitEffect.Instance.bloodDecalDuration;
        }
        Destroy(gameObject, duration);
    }
}
