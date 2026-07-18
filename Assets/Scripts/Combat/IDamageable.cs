// ============================================================
// IDamageable.cs — Interface สำหรับ Object ที่รับ Damage ได้
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// ใช้กับ Object ที่สามารถรับ Damage ได้ (Player, Enemy, etc.)
/// ทุก Object ที่โดนยิงได้ต้อง implement interface นี้
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// เรียกเมื่อโดนยิง
    /// </summary>
    /// <param name="damage">จำนวน Damage ที่ได้รับ</param>
    /// <param name="hitPoint">ตำแหน่งที่โดนยิง (World Space)</param>
    /// <param name="isCritical">true = ยิงโดนจุดอ่อน (เช่น ไข่แดง)</param>
    /// <param name="hitDirection">ทิศทางที่กระสุนพุ่งมาชน</param>
    void TakeDamage(float damage, Vector2 hitPoint, bool isCritical, Vector2 hitDirection);

    /// <summary>
    /// ตรวจว่ายังมีชีวิตอยู่ไหม
    /// </summary>
    bool IsAlive { get; }
}
