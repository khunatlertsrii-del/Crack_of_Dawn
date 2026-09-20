// ============================================================
// SlowMotionManager.cs — ระบบ Bullet Time (Slow-Motion)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// ระบบ Slow-Motion แบบ My Friend Pedro:
/// - กดค้าง Right Mouse → เวลาช้าลง
/// - มี Meter ที่หมดเรื่อยๆ ต้องรอ Recharge
/// - Time.fixedDeltaTime ปรับตามเพื่อ Physics ไม่พัง
/// 
/// Singleton → SlowMotionManager.Instance
/// </summary>
public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance { get; private set; }

    [Header("Slow-Motion Settings")]
    [Tooltip("timeScale ตอน Slow-Mo (0.3 = ช้าลง 70%)")]
    [SerializeField] private float slowMotionScale = 0.3f;

    [Tooltip("ความเร็วในการเปลี่ยน timeScale (Smooth transition)")]
    [SerializeField] private float transitionSpeed = 10f;

    [Tooltip("ดีเลย์ระหว่างการกดเปิด/ปิดสโลว์ (ป้องกันการกดรัว)")]
    [SerializeField] private float toggleCooldown = 0.5f;

    [Header("Meter / Energy")]
    [Tooltip("พลัง Slow-Mo สูงสุด")]
    [SerializeField] private float maxMeter = 100f;

    [Tooltip("อัตราการใช้พลัง Slow-Mo (ต่อวินาที)")]
    [SerializeField] private float drainRate = 30f;

    [Tooltip("อัตราการชาร์จพลังคืน (ต่อวินาที) ตอนไม่ได้กด")]
    [SerializeField] private float rechargeRate = 20f;

    [Tooltip("ต้องมีพลังเหลือขั้นต่ำเท่าไหร่ถึงจะเปิด Slow-Mo ได้")]
    [SerializeField] private float minimumMeterToActivate = 10f;

    [Header("Combat Reward")]
    [Tooltip("เปิดระบบเพิ่มหลอด Slow-Mo เมื่อโจมตีโดนศัตรู (เฉพาะตอนกำลังเปิด Slow-Mo อยู่)")]
    [SerializeField] private bool rewardMeterOnHit = true;

    [Tooltip("จำนวนหลอดที่เพิ่มขึ้นเมื่อโจมตีโดน")]
    [SerializeField] private float meterRewardPerHit = 5f;

    // ===================== State =====================
    public float CurrentMeter { get; private set; }
    public float MeterPercent => CurrentMeter / maxMeter;
    public bool IsSlowMotionActive { get; private set; } = false;

    /// <summary>เรียกเมื่อ Meter เปลี่ยน (ส่ง % 0-1)</summary>
    public event Action<float> OnMeterChanged;

    // ===================== Internal =====================
    private InputAction slowMoAction;
    private float defaultFixedDeltaTime;
    private float targetTimeScale = 1f;
    private float lastToggleTime = -100f;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup input — Right Mouse Button
        slowMoAction = new InputAction("SlowMotion", InputActionType.Button);
        slowMoAction.AddBinding("<Mouse>/rightButton");
        slowMoAction.AddBinding("<Gamepad>/leftTrigger");

        defaultFixedDeltaTime = Time.fixedDeltaTime;
        CurrentMeter = maxMeter;
    }

    private void Start()
    {
        OnMeterChanged?.Invoke(MeterPercent);
    }

    private void OnEnable()
    {
        slowMoAction.Enable();
    }

    private void OnDisable()
    {
        slowMoAction.Disable();
        // Reset time scale
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    private void Update()
    {
        // ข้ามถ้าจบเกมแล้ว หรือเกมหยุดอยู่
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        HandleSlowMotionInput();
        UpdateTimeScale();
        UpdateMeter();
    }

    // =========================================================
    // SLOW-MOTION LOGIC
    // =========================================================

    private void HandleSlowMotionInput()
    {
        bool pressedThisFrame = slowMoAction.WasPressedThisFrame();

        // สลับเปิด/ปิดเมื่อกดปุ่ม (เช็คคูลดาวน์ด้วย)
        if (pressedThisFrame && Time.unscaledTime - lastToggleTime >= toggleCooldown)
        {
            if (IsSlowMotionActive)
            {
                DeactivateSlowMotion();
                lastToggleTime = Time.unscaledTime;
            }
            else if (CurrentMeter > minimumMeterToActivate)
            {
                ActivateSlowMotion();
                lastToggleTime = Time.unscaledTime;
            }
        }

        // ถ้า Meter หมดขณะกำลังใช้ → ปิดอัตโนมัติ
        if (IsSlowMotionActive && CurrentMeter <= 0)
        {
            DeactivateSlowMotion();
        }
    }

    private void ActivateSlowMotion()
    {
        if (!IsSlowMotionActive)
        {
            IsSlowMotionActive = true;
            targetTimeScale = slowMotionScale;
            
            if (AudioManager.Instance != null && AudioManager.Instance.slowMoEnterSound != null)
                AudioManager.Instance.PlayUISound(AudioManager.Instance.slowMoEnterSound);
        }
    }

    private void DeactivateSlowMotion()
    {
        if (IsSlowMotionActive)
        {
            IsSlowMotionActive = false;
            targetTimeScale = 1f;
            
            if (AudioManager.Instance != null && AudioManager.Instance.slowMoExitSound != null)
                AudioManager.Instance.PlayUISound(AudioManager.Instance.slowMoExitSound);
        }
    }

    private void UpdateTimeScale()
    {
        // ถ้าเกมถูก Pause อยู่ หรือ จบด่านแล้ว หรือ คุยอยู่ ห้ามให้ SlowMotionManager ไปยุ่งกับ Time.timeScale!
        if ((PauseManager.Instance != null && PauseManager.Instance.IsPaused) ||
            (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) ||
            (DialogueManager.Instance != null && DialogueManager.Instance.dialogueBoxPanel != null && DialogueManager.Instance.dialogueBoxPanel.activeSelf))
        {
            return; 
        }

        // Smooth transition ไปยัง target timeScale
        Time.timeScale = Mathf.MoveTowards(
            Time.timeScale,
            targetTimeScale,
            transitionSpeed * Time.unscaledDeltaTime
        );

        // ปรับ fixedDeltaTime ตาม timeScale เพื่อ Physics ทำงานถูก
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private void UpdateMeter()
    {
        if (IsSlowMotionActive)
        {
            // ใช้พลัง (ใช้ unscaledDeltaTime เพราะ timeScale เปลี่ยนแล้ว)
            CurrentMeter -= drainRate * Time.unscaledDeltaTime;
            CurrentMeter = Mathf.Max(0, CurrentMeter);
        }
        else
        {
            // ชาร์จพลังคืน
            CurrentMeter += rechargeRate * Time.unscaledDeltaTime;
            CurrentMeter = Mathf.Min(maxMeter, CurrentMeter);
        }

        OnMeterChanged?.Invoke(MeterPercent);
    }

    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    /// <summary>เพิ่ม Meter (เช่น เก็บไอเท็ม)</summary>
    public void AddMeter(float amount)
    {
        CurrentMeter = Mathf.Min(maxMeter, CurrentMeter + amount);
        OnMeterChanged?.Invoke(MeterPercent);
    }

    /// <summary>รีเซ็ต Meter เต็ม</summary>
    public void ResetMeter()
    {
        CurrentMeter = maxMeter;
        OnMeterChanged?.Invoke(MeterPercent);
    }

    // =========================================================
    // COMBAT REWARD
    // =========================================================

    /// <summary>
    /// เรียกจากศัตรูเมื่อมันโดนโจมตี (Bullet หรือ Melee)
    /// </summary>
    public void AddMeterFromHit()
    {
        if (rewardMeterOnHit && IsSlowMotionActive)
        {
            CurrentMeter += meterRewardPerHit;
            CurrentMeter = Mathf.Min(CurrentMeter, maxMeter); // ไม่ให้เกิน max
            OnMeterChanged?.Invoke(MeterPercent);
        }
    }
}
