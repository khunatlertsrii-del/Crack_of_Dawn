using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// คัตซีนเปิดด่าน Level 1 (V3):
/// 1. NPC คุยกัน (กล้องจับที่ NPC ไม่ตาม Player)
/// 2. Player พุ่งเข้ามาทะลุหน้าต่าง + เล่นท่าสไลด์
/// 3. ★ กลางสไลด์ → เวลาหยุด → สอนกด Shift
/// 4. ★ กด Shift → สโลว์โม (มีขอบฟิล์ม!) → Player สไลด์ต่อแบบช้าๆ
/// 5. สไลด์ถึงจุดจอด → สอนยิง → ยิง → จบคัตซีน
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
    public float playerEntryDuration = 1.2f;
    public float delayBeforeEntry = 0.5f;
    [Tooltip("ความสูงของวิถีโค้ง (ปรับเป็น 0 ถ้าอยากให้สไลด์มาบนพื้นตรงๆ)")]
    public float arcHeight = 0f;
    [Tooltip("จุดที่สไลด์ถึงแล้วจะหยุดเวลา (0.0 = เริ่มสไลด์ ถึง 1.0 = ถึงจุดจอด ปกติใช้ 0.4)")]
    [Range(0.1f, 0.9f)]
    public float freezeAtProgress = 0.4f;

    [Header("=== แอนิเมชันและ UI ===")]
    [Tooltip("ชื่อ State ของแอนิเมชันสไลด์ที่คุณวาดไว้ใน Animator")]
    public string slideAnimationState = "Slide";
    [Tooltip("ชื่อ State ท่ายืนปกติ")]
    public string idleAnimationState = "Idle";
    
    [Tooltip("หน้า UI ที่ต้องการซ่อนชั่วคราวระหว่างคัตซีน (ลาก Canvas หลอดเลือด/สโลว์/กระสุน มาใส่)")]
    public GameObject[] gameplayUI;

    // State
    private bool cutsceneActive = false;
    private bool waitingForSlowMo = false;
    private bool waitingForShoot = false;
    private bool slideResumed = false;
    private InputAction slowMoAction;
    private InputAction shootAction;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private PlayerShooting playerShooting;
    private PlayerParry playerParry;
    private PlayerAnimator playerAnimatorScript;
    private Animator playerAnimator;
    private CameraFollow cameraFollow;
    private SlowMotionManager slowMotionManager;
    private Camera mainCam;
    private AnimatorUpdateMode originalAnimUpdateMode;

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
            playerAnimatorScript = player.GetComponent<PlayerAnimator>();
            // ถ้าไม่ได้ลาก Animator ใส่มาเอง ให้ลองหาดูในลูกๆ
            if (playerAnimator == null) playerAnimator = player.GetComponentInChildren<Animator>();
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

        // ไม่ได้กดค้าง = false, กดค้าง = true
        bool isHoldingSlowMo = slowMoAction.IsPressed();

        // ให้อัปเดตสถานะ slideResumed ตามการกดค้าง
        if (waitingForSlowMo)
        {
            slideResumed = isHoldingSlowMo;
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

        // รอ 1 เฟรม ให้สคริปต์อื่นๆ (เช่น UI) ตั้งค่าตัวเองใน Start ให้เสร็จก่อน ค่อยสั่งปิด
        yield return null;

        // ซ่อน UI ระหว่างคัตซีน
        SetGameplayUI(false);

        // === 1. ปิดทุกอย่าง (เดิน ยิง สโลว์ ดาบ) ===
        LockAllPlayerSystems();

        // ซ่อน Player นอกจอ
        if (playerStartPoint != null) player.transform.position = playerStartPoint.position;
        player.SetActive(false);

        // === 2. ย้ายกล้องไปที่ NPC (ไม่ตาม Player!) ===
        if (cameraFollow != null) cameraFollow.enabled = false;
        if (cameraFocusNPC != null && mainCam != null)
        {
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

        // === 4. ย้ายกล้องไปจุด Action ===
        if (cameraFocusAction != null && mainCam != null)
        {
            yield return StartCoroutine(PanCameraTo(cameraFocusAction.position, 0.6f));
        }

        // === 5. Player พุ่งเข้ามา + สไลด์ (จะหยุดกลางทาง!) ===
        player.SetActive(true);

        // เอฟเฟกต์กระจกแตก
        if (windowBreakEffect != null) windowBreakEffect.SetActive(true);
        if (windowBreakSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(windowBreakSound, 1f);

        // Screen Shake ตอนทะลุหน้าต่าง!
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.3f, 0.4f);

        // สไลด์เข้ามา → จะหยุดกลางทาง → รอกดสโลว์ → สไลด์ต่อ → ถึงจุดจอด
        yield return StartCoroutine(SlideEntryWithFreeze());

        // === ถึงจุดจอดแล้ว ===
        
        // เสียงลงพื้น + Screen Shake
        if (landingSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(landingSound, 0.8f);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.4f, 0.3f);

        // หยุดท่าสไลด์ กลับมาท่า Idle
        if (playerAnimator != null)
        {
            playerAnimator.Play(idleAnimationState);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // === สอนยิง ===
        // เปิด PlayerShooting ชั่วคราว
        if (playerShooting != null) playerShooting.enabled = true;

        if (shootPrompt != null)
        {
            shootPrompt.SetActive(true);
            StartCoroutine(PulseEffect(shootPrompt.transform));
        }
        waitingForShoot = true;
    }

    // =========================================================
    // สไลด์ + หยุดกลางทาง + สโลว์โม + สไลด์ต่อ
    // =========================================================

    private IEnumerator SlideEntryWithFreeze()
    {
        if (playerLandPoint == null) yield break;

        Vector3 start = player.transform.position;
        Vector3 end = playerLandPoint.position;

        // หันหน้าให้ถูก
        if (player.transform.localScale.x < 0)
        {
            Vector3 scale = player.transform.localScale;
            scale.x *= -1;
            player.transform.localScale = scale;
        }

        // ปิด PlayerAnimator script ไม่ให้มันแย่งควบคุม Animator
        if (playerAnimatorScript != null) playerAnimatorScript.enabled = false;

        // ตั้ง Animator ให้อัปเดตแม้ TimeScale = 0
        if (playerAnimator != null)
        {
            originalAnimUpdateMode = playerAnimator.updateMode;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            playerAnimator.Play(slideAnimationState);
        }

        // === ช่วงที่ 1: สไลด์เข้ามาจนถึงจุดหยุด (freezeAtProgress) ===
        float elapsed = 0f;
        float phase1Duration = playerEntryDuration * freezeAtProgress;

        while (elapsed < phase1Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / playerEntryDuration); // t ตาม duration ทั้งหมด
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 pos = Vector3.Lerp(start, end, easedT);
            float arc = arcHeight * 4f * t * (1f - t);
            pos.y += arc;
            player.transform.position = pos;
            yield return null;
        }

        // === ★ หยุดเวลา! (Freeze Frame กลางอากาศ/กลางสไลด์) ===
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(0.3f);

        // โชว์ "กด Shift ค้างไว้"
        if (slowMoPrompt != null)
        {
            slowMoPrompt.SetActive(true);
            StartCoroutine(PulseEffect(slowMoPrompt.transform));
        }
        waitingForSlowMo = true;
        slideResumed = false;

        // รอจนกว่าจะเริ่มกด Shift (hold)
        yield return new WaitUntil(() => slideResumed);

        // ปิด Prompt "กด Shift"
        if (slowMoPrompt != null) slowMoPrompt.SetActive(false);
        waitingForSlowMo = false; // ไม่ต้องเช็ค prompt แล้ว

        // เปิดเอฟเฟกต์ขอบฟิล์ม/Adrenaline
        if (ScreenEffectManager.Instance != null)
            ScreenEffectManager.Instance.TriggerAdrenaline();

        if (AudioManager.Instance != null && AudioManager.Instance.slowMoEnterSound != null)
            AudioManager.Instance.PlayUISound(AudioManager.Instance.slowMoEnterSound);

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.15f, 0.2f);

        // === ช่วงที่ 2: สไลด์ต่อแบบสโลว์โม (ระยะที่เหลือ) ===
        // ถ้าปล่อยปุ่ม Shift เวลาจะหยุดนิ่ง!
        while (elapsed < playerEntryDuration)
        {
            // เช็คว่ากดค้างอยู่ไหม (ในขณะอยู่ใน Phase 2)
            bool isHolding = slowMoAction.IsPressed();

            if (isHolding)
            {
                Time.timeScale = 0.15f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                float dt = Time.unscaledDeltaTime * Time.timeScale;
                elapsed += dt;
                
                float t = Mathf.Clamp01(elapsed / playerEntryDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 pos = Vector3.Lerp(start, end, easedT);
                float arc = arcHeight * 4f * t * (1f - t);
                pos.y += arc;
                player.transform.position = pos;
            }
            else
            {
                // ถ้าปล่อยปุ่ม ให้หยุดนิ่ง!
                Time.timeScale = 0f;
                Time.fixedDeltaTime = 0f;
            }

            yield return null;
        }

        // ลงจุดพอดี
        player.transform.position = end;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
        // คืนค่า timeScale
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }



    // =========================================================
    // เมื่อกดยิง
    // =========================================================

    private IEnumerator OnShootPressed()
    {
        if (shootPrompt != null) shootPrompt.SetActive(false);

        // เวลากลับมาปกติ
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // เสียงออกสโลว์
        if (AudioManager.Instance != null && AudioManager.Instance.slowMoExitSound != null)
            AudioManager.Instance.PlayUISound(AudioManager.Instance.slowMoExitSound);

        // Screen Shake ตอนกระสุนออก
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeCamera(0.15f, 0.2f);

        // รอให้กระสุนบินไปโดน NPC
        yield return new WaitForSeconds(0.8f);

        // === จบคัตซีน ===
        UnlockAllPlayerSystems();
        cutsceneActive = false;
        
        // เปิด UI กลับมา
        SetGameplayUI(true);

        // เปิดกล้องตาม Player กลับ
        if (cameraFollow != null) cameraFollow.enabled = true;

        Destroy(gameObject, 1f);
    }

    // =========================================================
    // CAMERA PAN
    // =========================================================

    private IEnumerator PanCameraTo(Vector3 target, float duration)
    {
        if (mainCam == null) yield break;

        Vector3 start = mainCam.transform.position;
        target.z = start.z;
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
    // PULSE EFFECT
    // =========================================================

    private IEnumerator PulseEffect(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float timer = 0f;

        while (timer < 0.4f)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / 0.4f;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            target.localScale = originalScale * scale;
            yield return null;
        }
        target.localScale = originalScale;

        while (target.gameObject.activeSelf)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.08f;
            target.localScale = originalScale * pulse;
            yield return null;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void SetGameplayUI(bool show)
    {
        if (gameplayUI == null) return;
        foreach (GameObject ui in gameplayUI)
        {
            if (ui != null) ui.SetActive(show);
        }
    }

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
        if (playerShooting != null)
        {
            playerShooting.enabled = true;
            playerShooting.ShowAmmoUI();
        }
        if (playerParry != null) playerParry.enabled = true;
        if (slowMotionManager != null) slowMotionManager.enabled = true;
        if (playerAnimatorScript != null) playerAnimatorScript.enabled = true;
        if (playerAnimator != null) playerAnimator.updateMode = originalAnimUpdateMode;
        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
