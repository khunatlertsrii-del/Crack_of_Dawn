// ============================================================
// TimerStartTrigger.cs — กล่องเขตเริ่มนับเวลาถอยหลัง (Katana Zero Style)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;

/// <summary>
/// กล่อง Trigger สำหรับเริ่มนับเวลาถอยหลัง:
/// วางไว้ในฉาก เมื่อ Player เดินผ่านเขตนี้ ระบบจับเวลาจะเริ่มนับถอยหลังทันที
/// ก่อนเดินผ่านเขตนี้ หลอดเวลาจะไม่ทำงาน (ไม่นับถอยหลัง)
/// มี Gizmos สีทอง-ส้ม แสดงกรอบในหน้า Scene View ชัดเจนเหมือน MusicChangeTrigger
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("Omelet Rush/Systems/Timer Start Trigger")]
public class TimerStartTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("ให้เริ่มนับเวลาแค่ครั้งเดียว")]
    public bool triggerOnce = true;

    [Tooltip("ซ่อนหลอดเวลาบนหน้าจอไว้ก่อน จนกว่าผู้เล่นจะเดินเข้าเขตนี้หรือไม่")]
    public bool hideTimerUntilTriggered = false;

    [Header("Optional Enhancements")]
    [Tooltip("เปลี่ยนเพลงประกอบเมื่อเริ่มจับเวลาด้วยหรือไม่ (เว้นว่างไว้ได้)")]
    public AudioManager.MusicTrack musicOnStart;

    [Tooltip("เสียงเอฟเฟกต์เมื่อเริ่มจับเวลา (Optional เช่น เสียงสัญญาณเตือนภัย/หวูด)")]
    public AudioClip sfxOnStart;

    private bool hasTriggered = false;

    private void Awake()
    {
        // บังคับให้ BoxCollider2D เป็น Trigger เสมอ
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (hideTimerUntilTriggered && UIManager.Instance != null)
        {
            UIManager.Instance.SetTimerUIVisible(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnce && hasTriggered) return;

        // เช็คว่าผู้เล่นเดินเข้ามาในเขตหรือไม่
        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // สั่งเริ่มนับเวลาถอยหลังใน LevelRatingManager
            if (LevelRatingManager.Instance != null)
            {
                LevelRatingManager.Instance.StartCountdownTimer();
            }

            // แสดง UI เวลา (กรณีเลือกซ่อนไว้ก่อนหน้า)
            if (hideTimerUntilTriggered && UIManager.Instance != null)
            {
                UIManager.Instance.SetTimerUIVisible(true);
            }

            // เปลี่ยนเพลงถ้ามีการตั้งไว้
            if (musicOnStart != null && musicOnStart.clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(musicOnStart);
            }

            // เล่น SFX ถ้ามี
            if (sfxOnStart != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(sfxOnStart);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // วาดกรอบสีทอง/ส้ม โปร่งแสงใน Scene View เพื่อให้มองเห็นและปรับขนาดง่าย
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.35f);
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}
