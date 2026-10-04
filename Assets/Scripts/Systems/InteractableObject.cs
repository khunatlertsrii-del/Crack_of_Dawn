using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractableObject : MonoBehaviour
{
    [Header("📝 ข้อความที่จะพูด (ใส่กี่บรรทัดก็ได้)")]
    [TextArea(3, 10)]
    public string[] dialogues;

    [Header("🎨 UI ไอคอนเตือน")]
    [Tooltip("ใส่ GameObject ที่เป็นรูปปุ่ม E หรือเครื่องหมายตกใจ (จะโชว์ตอนเดินเข้าไปใกล้)")]
    public GameObject interactPromptUI;

    [Header("⚡ คำสั่งพิเศษเมื่อกดสำรวจ (Optional)")]
    [Tooltip("ใส่คำสั่งที่จะให้ทำงานทันทีเมื่อกดสำรวจ เช่น ลบประตู, เปลี่ยนเพลง, สปอนศัตรู")]
    public UnityEngine.Events.UnityEvent OnInteract;

    [Header("✨ แอนิเมชั่นปุ่ม (ลอยขึ้นลง)")]
    public bool enableFloatingAnimation = true;
    public float floatSpeed = 4f;
    public float floatHeight = 0.2f;

    private bool isPlayerInRange = false;
    private InputAction interactAction;
    private Vector3 promptInitialLocalPos;

    private void Awake()
    {
        // สร้างปุ่มกด E สำหรับสำรวจ
        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest"); // ปุ่ม X บนจอย
        
        // ซ่อนปุ่มเตือนไว้ก่อน
        if (interactPromptUI != null) 
        {
            interactPromptUI.SetActive(false);
            promptInitialLocalPos = interactPromptUI.transform.localPosition;
        }

        // บังคับให้ Collider เป็น Trigger เสมอ
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnEnable() => interactAction.Enable();
    private void OnDisable() => interactAction.Disable();

    private void Update()
    {
        // ถ้าโชว์ปุ่มอยู่ ให้ทำแอนิเมชั่นลอยขึ้นลง
        if (isPlayerInRange && interactPromptUI != null && enableFloatingAnimation)
        {
            float offset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
            interactPromptUI.transform.localPosition = promptInitialLocalPos + new Vector3(0, offset, 0);
        }

        // ถ้าผู้เล่นอยู่ในระยะ และกดปุ่ม E และกล่องข้อความยังไม่ได้เปิดอยู่
        if (isPlayerInRange && interactAction.WasPressedThisFrame())
        {
            if (DialogueManager.Instance != null && !DialogueManager.Instance.dialogueBoxPanel.activeSelf)
            {
                // เช็คคูลดาวน์ 0.5 วินาทีหลังจากเพิ่งปิดกล่องข้อความไป เพื่อกันกดเบิ้ล
                if (Time.unscaledTime - DialogueManager.Instance.lastDialogueEndTime < 0.5f) return;

                // ส่งข้อความไปให้ DialogueManager เปิดกล่อง
                if (dialogues != null && dialogues.Length > 0)
                {
                    DialogueManager.Instance.StartDialogue(dialogues);
                }

                // เรียกคำสั่งพิเศษ (ถ้ามี)
                OnInteract?.Invoke();
            }
        }
    }

    // เมื่อผู้เล่นเดินเข้ามาในอาณาเขต (ใกล้ของสิ่งนี้)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactPromptUI != null) 
            {
                interactPromptUI.transform.localPosition = promptInitialLocalPos;
                interactPromptUI.SetActive(true); // โชว์ปุ่ม E
            }
        }
    }

    // เมื่อผู้เล่นเดินออกไป
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactPromptUI != null) interactPromptUI.SetActive(false); // ซ่อนปุ่ม E
        }
    }
}
