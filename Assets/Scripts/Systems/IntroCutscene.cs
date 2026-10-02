using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// คัตซีนเปิดด่าน Level 1:
/// 1. ไข่ดาว 2 ตัวยืนคุยกัน (ใช้ DialogueManager)
/// 2. ผู้เล่น (ไข่เจียว) พุ่งทะลุหน้าต่างเข้ามาจากซ้าย
/// 3. เวลาหยุด → โชว์ TutorialPrompt สอนกด Shift (สโลว์โม)
/// 4. ผู้เล่นกด Shift → โชว์สอนคลิกซ้ายยิง
/// 5. ผู้เล่นคลิกยิง → เวลากลับมาเดิน กระสุนพุ่งฆ่าไข่ดาว 2 ตัว
/// 
/// วิธีใช้: สร้าง Empty GameObject ชื่อ "IntroCutscene" แล้วลากสคริปต์นี้ใส่
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("=== ตัวละคร ===")]
    [Tooltip("ตัวผู้เล่น (ไข่เจียว) ที่จะพุ่งเข้ามา")]
    public GameObject player;
    
    [Tooltip("จุดเริ่มต้นของผู้เล่น (อยู่นอกจอด้านซ้าย ตรงหน้าต่าง)")]
    public Transform playerStartPoint;
    
    [Tooltip("จุดที่ผู้เล่นลงจอด (ในห้อง)")]
    public Transform playerLandPoint;

    [Header("=== ไข่ดาว NPC ===")]
    [Tooltip("ไข่ดาว NPC ตัวที่ 1 (ไม่ใช่ FriedEggEnemy ธรรมดา ใช้แค่ Sprite + Collider)")]
    public GameObject eggNPC1;
    [Tooltip("ไข่ดาว NPC ตัวที่ 2")]
    public GameObject eggNPC2;

    [Header("=== บทพูด (ก่อน Player เข้ามา) ===")]
    [TextArea(2, 5)]
    public string[] npcDialogues = new string[]
    {
        "เฮ้ แกได้ยินข่าวมั้ย?",
        "ช่วงนี้มีไข่เจียวมือสังหารไปจัดการไข่ดาวไปทั่วเมืองเลย...",
        "บ้า... ไม่มีทางมาถึงที่นี่หรอก มั้ง..."
    };

    [Header("=== Tutorial Prompts ===")]
    [Tooltip("UI ที่แสดงปุ่ม Shift (โชว์ตอนสอนสโลว์โม)")]
    public GameObject slowMoPrompt;
    [Tooltip("UI ที่แสดงปุ่มคลิกซ้าย (โชว์ตอนสอนยิง)")]
    public GameObject shootPrompt;

    [Header("=== แก้วหน้าต่าง (Optional) ===")]
    [Tooltip("เอฟเฟกต์กระจกแตก หรือ Particle System (เล่นตอนผู้เล่นพุ่งเข้ามา)")]
    public GameObject windowBreakEffect;

    [Header("=== Settings ===")]
    public float playerEntrySpeed = 15f;
    public float delayBeforeEntry = 0.5f;

    // State
    private bool cutsceneActive = false;
    private bool waitingForSlowMo = false;
    private bool waitingForShoot = false;
    private InputAction slowMoAction;
    private InputAction shootAction;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private PlayerShooting playerShooting;

    private void Awake()
    {
        // สร้าง input actions สำหรับเช็คว่าผู้เล่นกดยัง
        slowMoAction = new InputAction("CutsceneSlowMo", InputActionType.Button);
        slowMoAction.AddBinding("<Keyboard>/leftShift");
        
        shootAction = new InputAction("CutsceneShoot", InputActionType.Button);
        shootAction.AddBinding("<Mouse>/leftButton");
    }

    private void Start()
    {
        // ซ่อน prompts ไว้ก่อน
        if (slowMoPrompt != null) slowMoPrompt.SetActive(false);
        if (shootPrompt != null) shootPrompt.SetActive(false);

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerController = player.GetComponent<PlayerController>();
            playerShooting = player.GetComponent<PlayerShooting>();
        }

        // เริ่มคัตซีน!
        StartCoroutine(PlayCutscene());
    }

    private void OnEnable()
    {
        slowMoAction.Enable();
        shootAction.Enable();
    }

    private void OnDisable()
    {
        slowMoAction.Disable();
        shootAction.Disable();
    }

    private void Update()
    {
        if (!cutsceneActive) return;

        // ขั้นตอน: รอผู้เล่นกดสโลว์โม
        if (waitingForSlowMo && slowMoAction.WasPressedThisFrame())
        {
            waitingForSlowMo = false;
            StartCoroutine(OnSlowMoPressed());
        }

        // ขั้นตอน: รอผู้เล่นกดยิง
        if (waitingForShoot && shootAction.WasPressedThisFrame())
        {
            waitingForShoot = false;
            StartCoroutine(OnShootPressed());
        }
    }

    // =========================================================
    // CUTSCENE SEQUENCE
    // =========================================================

    private IEnumerator PlayCutscene()
    {
        cutsceneActive = true;

        // === 1. ปิดการควบคุมผู้เล่น ===
        DisablePlayerControl();

        // ย้ายผู้เล่นไปซ่อนนอกจอ
        if (playerStartPoint != null)
        {
            player.transform.position = playerStartPoint.position;
        }
        // ซ่อนผู้เล่นไว้ก่อน
        player.SetActive(false);

        // === 2. ไข่ดาว NPC คุยกัน ===
        if (DialogueManager.Instance != null && npcDialogues.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(npcDialogues);

            // รอจนกว่ากล่องข้อความจะปิด
            yield return new WaitUntil(() => 
                !DialogueManager.Instance.dialogueBoxPanel.activeSelf);
        }

        yield return new WaitForSecondsRealtime(delayBeforeEntry);

        // === 3. ผู้เล่นพุ่งเข้ามาทะลุหน้าต่าง! ===
        player.SetActive(true);

        // เล่นเอฟเฟกต์กระจกแตก
        if (windowBreakEffect != null)
        {
            windowBreakEffect.SetActive(true);
        }

        // พุ่งผู้เล่นเข้ามาในฉาก
        yield return StartCoroutine(MovePlayerToLandPoint());

        // === 4. หยุดเวลา! (Freeze Frame) ===
        Time.timeScale = 0f;

        // === 5. โชว์ TutorialPrompt สอน Shift (สโลว์โม) ===
        if (slowMoPrompt != null) slowMoPrompt.SetActive(true);
        waitingForSlowMo = true;

        // รอผู้เล่นกดใน Update()...
    }

    private IEnumerator OnSlowMoPressed()
    {
        // ซ่อน prompt สโลว์โม
        if (slowMoPrompt != null) slowMoPrompt.SetActive(false);

        // เปิดสโลว์โม (ค่อยๆ ให้เวลาวิ่งช้าๆ)
        Time.timeScale = 0.1f;

        // รอสักครู่ให้ผู้เล่นรู้สึกถึงสโลว์โม
        yield return new WaitForSecondsRealtime(0.5f);

        // === 6. โชว์ TutorialPrompt สอนคลิกซ้ายยิง ===
        if (shootPrompt != null) shootPrompt.SetActive(true);
        waitingForShoot = true;

        // รอผู้เล่นกดใน Update()...
    }

    private IEnumerator OnShootPressed()
    {
        // ซ่อน prompt ยิง
        if (shootPrompt != null) shootPrompt.SetActive(false);

        // === 7. เวลากลับมาเดินปกติ! ===
        Time.timeScale = 1f;

        // รอสักนิดให้กระสุนบินไป
        yield return new WaitForSeconds(0.5f);

        // === 8. จบคัตซีน — คืนการควบคุมให้ผู้เล่น ===
        EnablePlayerControl();
        cutsceneActive = false;

        // ทำลายสคริปต์นี้ทิ้งเพื่อไม่ให้กินทรัพยากร
        Destroy(gameObject, 1f);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private IEnumerator MovePlayerToLandPoint()
    {
        if (playerLandPoint == null) yield break;

        Vector3 target = playerLandPoint.position;
        
        // พุ่งเข้ามาแบบเป็นเส้นตรง (แนว physics จำลอง)
        while (Vector2.Distance(player.transform.position, target) > 0.3f)
        {
            Vector2 dir = (target - player.transform.position).normalized;
            player.transform.position += (Vector3)(dir * playerEntrySpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        player.transform.position = target;

        // หยุดตัวผู้เล่น
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    private void DisablePlayerControl()
    {
        if (playerController != null) playerController.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    private void EnablePlayerControl()
    {
        if (playerController != null) playerController.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;
    }
}
