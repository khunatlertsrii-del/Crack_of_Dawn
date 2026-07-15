using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ระบบความกรอบ (Crispy Mode)
/// แทนที่จะได้คะแนนเฉยๆ ผู้เล่นจะได้ "ความร้อน" (Heat) เมื่อฆ่าศัตรู
/// เมื่อ Heat เต็ม 100 สามารถกดระเบิดพลัง "ร่างกรอบทอง" ได้
/// </summary>
public class CrispyModeManager : MonoBehaviour
{
    public static CrispyModeManager Instance { get; private set; }

    [Header("Heat Settings")]
    public float maxHeat = 100f;
    public float heatPerKill = 20f;
    public float heatPerCriticalKill = 40f;
    public float heatDecayRate = 5f; // ลดลงกี่หน่วยต่อวินาที

    [Header("Crispy Mode Settings")]
    public float crispyDuration = 8f;
    public float speedMultiplier = 1.5f;
    public float fireRateMultiplier = 2f;

    [Header("UI (Optional)")]
    [Tooltip("หลอดความร้อน (Slider)")]
    public Slider heatSlider;
    [Tooltip("ข้อความแสดงสถานะร่างกรอบทอง (TextMeshPro)")]
    public TextMeshProUGUI crispyStatusText;

    // ===================== State =====================
    public float CurrentHeat { get; private set; } = 0f;
    public bool IsCrispyActive { get; private set; } = false;
    
    // Properties ให้ Script อื่นดึงไปคูณความเก่ง
    public float CurrentSpeedMultiplier => IsCrispyActive ? speedMultiplier : 1f;
    public float CurrentFireRateMultiplier => IsCrispyActive ? fireRateMultiplier : 1f;

    // ===================== Events =====================
    public event Action<float> OnHeatChanged;
    public event Action<bool> OnCrispyModeChanged;

    // ===================== Internal =====================
    private float crispyTimer = 0f;
    private InputAction activateAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // สร้าง Input สำหรับกดแปลงร่าง (ปุ่ม Q หรือ D-Pad Up)
        activateAction = new InputAction("ActivateCrispy", InputActionType.Button);
        activateAction.AddBinding("<Keyboard>/q");
        activateAction.AddBinding("<Gamepad>/dpad/up");
    }

    private void OnEnable()
    {
        activateAction.Enable();
    }

    private void OnDisable()
    {
        activateAction.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (IsCrispyActive)
        {
            // กำลังอยู่ในโหมด Crispy
            crispyTimer -= Time.deltaTime;
            
            // ค่อยๆ ลด Heat ให้เห็นว่าหลอดกำลังหมด
            CurrentHeat = (crispyTimer / crispyDuration) * maxHeat;
            OnHeatChanged?.Invoke(CurrentHeat / maxHeat);

            if (crispyTimer <= 0)
            {
                EndCrispyMode();
            }
        }
        else
        {
            // โหมดปกติ: ความร้อนค่อยๆ ลดลงถ้าไม่ฆ่าใคร (และหลอดยังไม่เต็ม)
            if (CurrentHeat > 0 && CurrentHeat < maxHeat)
            {
                CurrentHeat -= heatDecayRate * Time.deltaTime;
                CurrentHeat = Mathf.Max(0, CurrentHeat);
                OnHeatChanged?.Invoke(CurrentHeat / maxHeat);
            }

            // เช็คกดแปลงร่าง
            if (CurrentHeat >= maxHeat && activateAction.WasPressedThisFrame())
            {
                ActivateCrispyMode();
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (heatSlider != null)
        {
            heatSlider.value = CurrentHeat / maxHeat;
        }

        if (crispyStatusText != null)
        {
            if (IsCrispyActive)
            {
                crispyStatusText.gameObject.SetActive(true);
                crispyStatusText.text = "CRISPY MODE!";
                // กระพริบสีแดง-ส้ม
                float alpha = Mathf.PingPong(Time.time * 5f, 1f);
                crispyStatusText.color = new Color(1f, 0.2f, 0f, 0.5f + (alpha * 0.5f));
            }
            else if (CurrentHeat >= maxHeat)
            {
                crispyStatusText.gameObject.SetActive(true);
                crispyStatusText.text = "PRESS Q TO BE CRISPY!";
                // กระพริบสีเหลือง-ทอง
                float alpha = Mathf.PingPong(Time.time * 4f, 1f);
                crispyStatusText.color = new Color(1f, 0.8f, 0f, alpha);
            }
            else
            {
                // ถ้ายังไม่เต็ม ให้ซ่อนข้อความไปเลย
                crispyStatusText.gameObject.SetActive(false);
            }
        }
    }

    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    /// <summary>เพิ่มความร้อน (เรียกเมื่อฆ่าศัตรู)</summary>
    public void AddHeat(bool isCritical)
    {
        if (IsCrispyActive) return; // ถ้าแปลงร่างอยู่ ไม่ต้องเพิ่ม (ให้หลอดมันลดไปจนหมด)

        float heatToAdd = isCritical ? heatPerCriticalKill : heatPerKill;
        CurrentHeat = Mathf.Min(CurrentHeat + heatToAdd, maxHeat);
        
        OnHeatChanged?.Invoke(CurrentHeat / maxHeat);

        if (CurrentHeat >= maxHeat)
        {
            Debug.Log("[CrispyMode] Heat is FULL! Press Q to activate!");
        }
    }

    private void ActivateCrispyMode()
    {
        IsCrispyActive = true;
        crispyTimer = crispyDuration;
        
        Debug.Log("[CrispyMode] ACTIVATED! 🍳🔥");
        OnCrispyModeChanged?.Invoke(true);

        // หา Player เพื่อใส่ Visual Effect แบบง่ายๆ
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.6f, 0f); // สีส้มทองกรอบๆ
        }
    }

    private void EndCrispyMode()
    {
        IsCrispyActive = false;
        CurrentHeat = 0f;
        
        Debug.Log("[CrispyMode] Ended. Back to normal.");
        OnCrispyModeChanged?.Invoke(false);

        // คืนสี Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }
    }
}
