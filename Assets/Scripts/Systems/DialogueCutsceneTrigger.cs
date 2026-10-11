using System.Collections;
using UnityEngine;

/// <summary>
/// สคริปต์สำหรับจุดคัตซีนย่อย (เช่น ก่อนโดดหลุม)
/// - เดินชน Trigger -> ล็อคผู้เล่น -> แพนกล้องไปที่เป้าหมาย -> คุย -> คุยจบแพนกล้องกลับ -> คืนการควบคุม
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueCutsceneTrigger : MonoBehaviour
{
    [Header("=== ตั้งค่ากล้อง ===")]
    [Tooltip("จุดที่อยากให้กล้องแพนไปดู (เช่น ตรงกลางระหว่างศัตรู 2 ตัว)")]
    public Transform cameraFocusTarget;
    public float cameraPanSpeed = 2f;

    [Header("=== บทสนทนา ===")]
    [TextArea(2, 5)]
    public string[] dialogues;

    [Header("=== หลังจากคุยจบ ===")]
    [Tooltip("ปุ่มที่อยากให้โผล่มาสอนหลังจากคุยจบ (เช่น ปุ่มฟันดาบ) Optional")]
    public GameObject promptToShowAfter;

    private bool hasTriggered = false;
    private Camera mainCam;
    private CameraFollow cameraFollow;
    private PlayerController playerController;
    private PlayerShooting playerShooting;
    private Rigidbody2D playerRb;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        mainCam = Camera.main;
        cameraFollow = CameraFollow.Instance;
        if (promptToShowAfter != null) promptToShowAfter.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            
            // ดึงคอมโพเนนต์ของผู้เล่น
            playerController = collision.GetComponent<PlayerController>();
            playerShooting = collision.GetComponent<PlayerShooting>();
            playerRb = collision.GetComponent<Rigidbody2D>();

            StartCoroutine(PlayCutscene(collision.transform));
        }
    }

    private IEnumerator PlayCutscene(Transform playerTransform)
    {
        // 1. ล็อคผู้เล่นให้อยู่นิ่งๆ
        if (playerController != null) playerController.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        // 2. ถอดกล้องออกจากผู้เล่น
        if (cameraFollow != null) cameraFollow.enabled = false;

        // 3. แพนกล้องไปที่เป้าหมาย (NPC)
        if (cameraFocusTarget != null)
        {
            yield return StartCoroutine(PanCamera(cameraFocusTarget.position));
        }

        // 4. เริ่มบทสนทนา
        if (DialogueManager.Instance != null && dialogues.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogues);
            // รอจนกว่าหน้าต่างคุยจะหายไป
            yield return new WaitUntil(() => !DialogueManager.Instance.dialogueBoxPanel.activeSelf);
        }

        // รอจังหวะนิดนึงให้คนอ่านพักหายใจ
        yield return new WaitForSeconds(0.5f);

        // 5. แพนกล้องกลับมาที่ผู้เล่น
        yield return StartCoroutine(PanCamera(playerTransform.position));

        // 6. เปิดปุ่มสอน (ถ้ามี)
        if (promptToShowAfter != null)
        {
            promptToShowAfter.SetActive(true);
        }

        // 7. คืนการควบคุมให้ผู้เล่น
        if (cameraFollow != null) cameraFollow.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (playerShooting != null)
        {
            playerShooting.enabled = true;
            playerShooting.ShowAmmoUI();
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }

        // ปิด Trigger นี้ไปเลย
        Destroy(gameObject, 2f);
    }

    private IEnumerator PanCamera(Vector3 targetPos)
    {
        Vector3 startPos = mainCam.transform.position;
        targetPos.z = startPos.z; // รักษาระยะ Z ของกล้องไว้
        
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * cameraPanSpeed;
            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }
}
