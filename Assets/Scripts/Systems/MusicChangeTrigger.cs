using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MusicChangeTrigger : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("เพลงที่ต้องการให้เปลี่ยนเมื่อผู้เล่นเดินผ่าน")]
    public AudioManager.MusicTrack newMusic;

    [Tooltip("ให้เล่นเพลงแบบ Fade หรือไม่? (ถ้าตั้ง AudioManager ไว้รองรับ)")]
    public bool fadeMusic = false;
    
    [Tooltip("ให้เปลี่ยนเพลงแค่ครั้งเดียวใช่ไหม?")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        // บังคับให้เป็น Trigger เสมอ
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnce && hasTriggered) return;

        // เช็คว่าคนที่เดินผ่านคือ Player หรือไม่
        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;
            
            if (newMusic != null && newMusic.clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(newMusic);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}
