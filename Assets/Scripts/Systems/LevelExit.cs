using UnityEngine;

/// <summary>
/// จุดจบด่าน เมื่อผู้เล่นเดินมาถึง จะตัดจบด่านและแสดงคะแนนสรุป
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelExit : MonoBehaviour
{
    private bool hasTriggered = false;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        
        // ถ้ายังไม่มีขนาด ให้ขยายใหญ่ๆ หน่อย
        if (col.size == Vector2.one)
        {
            col.size = new Vector2(2f, 4f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered) return;

        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("[LevelExit] ผู้เล่นเข้าเส้นชัย! จบด่าน!");

            // 1. บอก GameManager ว่าจบเกม (ชนะ)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Victory);
            }

            // 2. สั่งคำนวณและโชว์เกรด
            if (LevelRatingManager.Instance != null)
            {
                LevelRatingManager.Instance.ShowVictorySummary();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // สีเขียวโปร่งแสง
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
        }
    }
}
