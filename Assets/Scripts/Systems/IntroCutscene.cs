using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// คัตซีนเปิดด่าน Level 1 (ปรับปรุงใหม่ V2):
/// - ควบคุมกล้องแยกจาก Player (ตั้งมุมกล้องเองได้)
/// - Player พุ่งเข้ามาแบบมีแอนิเมชัน (วิถีโค้ง + หมุน + ลงพื้นเท่ๆ)
/// - ล็อคทุกอย่างระหว่างคัตซีน (เดินไม่ได้ ยิงไม่ได้)
/// - มี Screen Shake + เสียง + Flash ตอน Player พุ่งเข้ามา
/// - เปิด PlayerShooting ชั่วคราวตอนสอนยิง
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("=== ตัวละคร ===")]
    public GameObject player;
    
    [Tooltip("จุดเริ่มต้นที่ Player จะพุ่งมา (นอกจอด้านซ้าย)")]
    public Transform playerStartPoint;
    
    [Tooltip("จุดที่ Player ลงจอด (ในห้อง)")]
    public Transform playerLandPoint;

    [Header("=== กล้อง ===")]
    [Tooltip("จุดที่กล้องจะจับตอน NPC คุยกัน (ลาก Empty มาตั้งตรงกลางระหว่าง NPC)")]
    public Transform cameraFocusNPC;
    
    [Tooltip("จุดที่กล้องจะย้ายไปตอน Player พุ่งเข้ามา")]
    public Transform cameraFocusAction;

    [Header("=== ไข่ดาว NPC ===")]
    public GameObject eggNPC1;
    public GameObject eggNPC2;

    [Header("=== บทพูด ===")]
    [TextArea(2, 5)]
    public string[] npcDialogues = new string[]
    {
        "เฮ้ แกได้ยินข่าวมั้ย?",
        "ช่วงนี้มีไข่เจียวมือสังหารไปจัดการไข่ดาวไปทั่วเมืองเลย...",
        "บ้า... ไม่มีทางมาถึงที่นี่หรอก มั้ง..."
    };

    [Header("=== Tutorial Prompts ===")]
    public GameObject slowMoPrompt;
    public GameObject shootPrompt;

    [Header("=== เอฟเฟกต์ ===")]
    [Tooltip("เอฟเฟกต์กระจกแตก / Particle (Optional)")]
    public GameObject windowBreakEffect;
    
    [Tooltip("เสียงกระจกแตกตอน Player พุ่งเข้ามา")]
    public AudioClip windowBreakSound;
    
    [Tooltip("เสียงลงพื้น (ตูม!)")]
    public AudioClip landingSound;

    [Header("=== ตั้งค่า ===")]
    public float playerEntryDuration = 0.8f;
    public float delayBeforeEntry = 0.5f;
    public float cameraFollowSpeed = 5f;
    [Tooltip("ความสูงสูงสุดของวิถีโค้ง (ยิ่งเยอะ Player พุ่งสูงยิ่งเท่)")]
    public float arcHeight = 3f;

    // State
    private bool cutsceneActive = false;
    private bool waitingForSlowMo = false;
    private bool waitingForShoot = false;
    private InputAction slowMoAction;
    private InputAction shootAction;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private PlayerShooting playerShooting;
    private PlayerParry playerParry;
    private CameraFollow cameraFollow;
    private SlowMotionManager slowMotionManager;
    private Camera mainCam;

    private void Awake()
    {
        slowMoAction = new InputAction("CutsceneSlowMo", InputActionType.Button);
        slowMoAction.AddBinding("<Keyboard>/leftShift");
        
        shootAction = new InputAction("CutsceneShoot", InputActionType.Button);
        shootAction.AddBinding("<Mouse>/leftButton");
    }

    private void Start()
    {
        if (slowMoPrompt != null) slowMoPrompt.SetActive(false);
        if (shootPrompt != null) shootPrompt.SetActive(false);

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerController = player.GetComponent<PlayerController>();
            playerShooting = player.GetComponent<PlayerShooting>();
            playerParry = player.GetComponent<PlayerParry>();
        }

        mainCam = Camera.main;
        cameraFollow = CameraFollow.Instance;
        slowMotionManager = SlowMotionManager.Instance;

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

        if (waitingForSlowMo && slowMoAction.WasPressedThisFrame())
        {
            waitingForSlowMo = false;
            StartCoroutine(OnSlowMoPressed());
        }

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

        // === 1. ปิดทุกอย่าง (เดิน ยิง สโลว์ ดาบ) ===
        LockAllPlayerSystems();

        // ซ่อน Player นอกจอ
        if (playerStartPoint != null) player.transform.position = playerStartPoint.position;
        player.SetActive(false);

        // === 2. ย้ายกล้องไปที่ NPC (ไม่ตาม Player!) ===
        if (cameraFollow != null) cameraFollow.enabled = false;
        if (cameraFocusNPC != null && mainCam != null)
        {
            // Snap กล้องไปที่จุด NPC ทันที
            Vector3 camPos = cameraFocusNPC.position;
            camPos.z = mainCam.transform.position.z;
            mainCam.transform.position = camPos;
        }

        // === 3. NPC คุยกัน ===
        if (DialogueManager.Instance != null && npcDialogues.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(npcDialogues);
            yield return new WaitUntil(() =>
                !DialogueManager.Instance.dialogueBoxPanel.activeSelf);
        }

        yield return new WaitForSecondsRealtime(delayBeforeEntry);

        // === 4. ย้ายกล้องไปจุด Action (ค่อยๆ แพน) ===
        if (cameraFocusAction != null && mainCam != null)
        {
            yield return StartCoroutine(PanCameraTo(cameraFocusAction.position, 0.6f));
        }

        // === 5. Player พุ่งเข้ามาทะลุหน้าต่าง! (แบบมีวิถีโค้ง + หมุน) ===
        player.SetActive(true);
        
        // เอฟเฟกต์กระจกแตก
        if (windowBreakEffect != null) windowBreakEffect.SetActive(true);
        if (windowBreakSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(windowBreakSound, 1f);

        // Screen Shake ตอนทะลุหน้าต่าง!
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.3f, 0.4f);

        // Player พุ่งเข้ามาแบบวิถีโค้ง + หมุนตัว
        yield return StartCoroutine(ArcEntryAnimation());

        // เสียงลงพื้น + Screen Shake ตอนลงจอด!
        if (landingSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(landingSound, 0.8f);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.4f, 0.3f);

        // === 6. Freeze Frame! (หยุดเวลา) ===
        Time.timeScale = 0f;

        // แฟลชขาวจอ (ถ้ามี ScreenEffectManager)
        yield return new WaitForSecondsRealtime(0.3f);

        // === 7. สอน Shift (สโลว์โม) ===
        if (slowMoPrompt != null)
        {
            slowMoPrompt.SetActive(true);
            // เด้งเข้ามาเล็กน้อย
            StartCoroutine(PulseEffect(slowMoPrompt.transform));
        }
        waitingForSlowMo = true;
    }

    private IEnumerator OnSlowMoPressed()
    {
        if (slowMoPrompt != null) slowMoPrompt.SetActive(false);

        // เปิดสโลว์โม (ไม่ใช้ SlowMotionManager เพราะมันถูกล็อค)
        Time.timeScale = 0.15f;

        // เปิดเอฟเฟกต์ขอบฟิล์มสโลว์
        if (ScreenEffectManager.Instance != null)
            ScreenEffectManager.Instance.TriggerAdrenaline();

        yield return new WaitForSecondsRealtime(0.6f);

        // === 8. สอนคลิกซ้ายยิง ===
        // **เปิด PlayerShooting ชั่วคราวเพื่อให้ยิงได้!**
        if (playerShooting != null) playerShooting.enabled = true;

        if (shootPrompt != null)
        {
            shootPrompt.SetActive(true);
            StartCoroutine(PulseEffect(shootPrompt.transform));
        }
        waitingForShoot = true;
    }

    private IEnumerator OnShootPressed()
    {
        if (shootPrompt != null) shootPrompt.SetActive(false);

        // ปิด shooting กลับ (จะเปิดอีกทีตอนจบคัตซีน)
        // ไม่ปิดเพราะกระสุนต้องบินไปก่อน

        // เวลากลับมาเดินปกติ!
        Time.timeScale = 1f;

        // Screen Shake เบาๆ ตอนกระสุนออก
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.15f, 0.2f);

        // รอให้กระสุนบินไปโดน NPC
        yield return new WaitForSeconds(0.8f);

        // === 9. จบคัตซีน — คืนการควบคุมทุกอย่าง ===
        UnlockAllPlayerSystems();
        cutsceneActive = false;

        // เปิดกล้องตาม Player กลับ
        if (cameraFollow != null) cameraFollow.enabled = true;

        Destroy(gameObject, 1f);
    }

    // =========================================================
    // ANIMATION — วิถีโค้ง + หมุนตัว
    // =========================================================

    private IEnumerator ArcEntryAnimation()
    {
        if (playerLandPoint == null) yield break;

        Vector3 start = player.transform.position;
        Vector3 end = playerLandPoint.position;
        float elapsed = 0f;

        // เก็บ rotation เดิม
        Quaternion originalRotation = player.transform.rotation;

        while (elapsed < playerEntryDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / playerEntryDuration);

            // Ease out (เริ่มเร็ว ชะลอตอนท้าย)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // ตำแหน่ง Lerp ตรงๆ
            Vector3 pos = Vector3.Lerp(start, end, easedT);

            // เพิ่มวิถีโค้ง (พาราโบลา)
            float arc = arcHeight * 4f * t * (1f - t); // สูงสุดตรงกลาง
            pos.y += arc;

            player.transform.position = pos;

            // หมุนตัว Player (Flip + Spin) 
            float spin = t * 360f; // หมุนครบ 1 รอบ
            player.transform.rotation = Quaternion.Euler(0, 0, -spin);

            yield return null;
        }

        // ลงจุดพอดี + รีเซ็ต rotation
        player.transform.position = end;
        player.transform.rotation = originalRotation;

        // หยุด Rigidbody
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    // =========================================================
    // CAMERA PAN — แพนกล้องไปจุดที่ต้องการ
    // =========================================================

    private IEnumerator PanCameraTo(Vector3 target, float duration)
    {
        if (mainCam == null) yield break;

        Vector3 start = mainCam.transform.position;
        target.z = start.z; // คง Z ไว้
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCam.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        mainCam.transform.position = target;
    }

    // =========================================================
    // PULSE EFFECT — ปุ่มเด้งตอนขึ้น
    // =========================================================

    private IEnumerator PulseEffect(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float timer = 0f;

        // เด้งใหญ่ขึ้นแล้วหดกลับ
        while (timer < 0.4f)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / 0.4f;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f; // เด้ง 30%
            target.localScale = originalScale * scale;
            yield return null;
        }
        target.localScale = originalScale;

        // ยังค้างอยู่ = เต้นเบาๆ ไปเรื่อยๆ
        while (target.gameObject.activeSelf)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.08f;
            target.localScale = originalScale * pulse;
            yield return null;
        }
    }

    // =========================================================
    // LOCK / UNLOCK ทุกระบบ
    // =========================================================

    private void LockAllPlayerSystems()
    {
        if (playerController != null) playerController.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
        if (playerParry != null) playerParry.enabled = false;
        if (slowMotionManager != null) slowMotionManager.enabled = false;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void UnlockAllPlayerSystems()
    {
        if (playerController != null) playerController.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;
        if (playerParry != null) playerParry.enabled = true;
        if (slowMotionManager != null) slowMotionManager.enabled = true;
        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
