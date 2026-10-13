// ============================================================
// PlayerHealth.cs — ระบบ HP ผู้เล่น
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ระบบ HP ของ "จอห์น ไข่เจียว":
/// - HP พร้อมตาย
/// - I-Frames (ช่วงอมตะหลังโดนยิง)
/// - กระพริบตัวตอนอยู่ใน I-Frame
/// - แจ้ง GameManager เมื่อตาย
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Hit-Based Health (3 ครั้งตาย สไตล์ Mario)")]
    [Tooltip("เปิดใช้ระบบเลือดแบบนับจำนวนครั้ง (3 ครั้งตาย)")]
    [SerializeField] private bool useHitBasedHealth = true;
    [Tooltip("จำนวนครั้งที่โดนยิงได้ก่อนตาย")]
    [SerializeField] private int maxHits = 3;
    [Tooltip("โดนโจมตีแต่ละครั้ง ลดกี่ขีด")]
    [SerializeField] private float damagePerHit = 1f;

    [Header("Health (Classic Mode)")]
    [Tooltip("HP สูงสุด (ใช้เมื่อปิด useHitBasedHealth)")]
    [SerializeField] private float maxHealth = 3f;

    [Header("I-Frame (Invincibility)")]
    [Tooltip("ระยะเวลาอมตะหลังโดนยิง (วินาที)")]
    [SerializeField] private float invincibilityDuration = 1.0f;

    [Tooltip("ความเร็วกระพริบ (ยิ่งเยอะยิ่งกระพริบเร็ว)")]
    [SerializeField] private float blinkRate = 15f;

    [Tooltip("แรง Knockback เมื่อโดนยิง")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Auto Regeneration (ปิดไว้เพื่อความดุเดือด)")]
    [Tooltip("เปิดใช้รีเจนเลือดอัตโนมัติ (แนะนำให้ปิดเพื่อให้เกมกดดัน)")]
    [SerializeField] private bool enableAutoRegen = false;
    [Tooltip("ต้องไม่โดนโจมตีกี่วินาที เลือดถึงจะเริ่มเด้ง")]
    [SerializeField] private float regenDelay = 4.0f;
    [Tooltip("ความเร็วในการรีเจน (HP ต่อวินาที)")]
    [SerializeField] private float regenRate = 20f;

    // ===================== State =====================
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float HealthPercent => CurrentHealth / maxHealth;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvincible => _isInvincible || _manualInvincible;
    
    private bool _isInvincible;
    private bool _manualInvincible;

    // ===================== Events =====================
    /// <summary>เรียกเมื่อ HP เปลี่ยน (ส่ง % 0-1)</summary>
    public event Action<float> OnHealthChanged;
    /// <summary>เรียกเมื่อโดนยิง</summary>
    public event Action<float, Vector2> OnDamaged;
    /// <summary>เรียกเมื่อตาย</summary>
    public event Action OnDied;

    // ===================== References =====================
    private SpriteRenderer[] spriteRenderers;
    private Rigidbody2D rb;
    private Coroutine blinkCoroutine;
    private float timeSinceLastHit;
    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // โหมด 3 ครั้งตาย (Mario Style)
        if (useHitBasedHealth)
        {
            maxHealth = maxHits;
            enableAutoRegen = false; // บังคับปิด Auto Regen ทันที
        }

        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(HealthPercent);
    }

    private void Update()
    {
        if (!IsAlive) return;

        if (enableAutoRegen && CurrentHealth < maxHealth)
        {
            timeSinceLastHit += Time.deltaTime;
            
            // ถ้ารอจนครบเวลาที่กำหนดแล้ว ให้ค่อยๆ รีเจนเลือด
            if (timeSinceLastHit >= regenDelay)
            {
                CurrentHealth += regenRate * Time.deltaTime;
                CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
                OnHealthChanged?.Invoke(HealthPercent);
                ScreenEffectManager.Instance?.UpdateHealthPercent(HealthPercent);
            }
        }
    }

    // =========================================================
    // IDamageable IMPLEMENTATION
    // =========================================================

    public void TakeDamage(float damage, Vector2 hitPoint, bool isCritical, Vector2 hitDirection)
    {
        if (!IsAlive) return;
        if (IsInvincible) return;



        // คำนวณดาเมจ: ถ้าใช้โหมด 3 ครั้งตาย จะลดทีละ 1 ขีด
        float damageToApply = useHitBasedHealth ? damagePerHit : damage;
        CurrentHealth -= damageToApply;
        CurrentHealth = Mathf.Max(0, CurrentHealth);
        timeSinceLastHit = 0f; // รีเซ็ตเวลารีเจนเมื่อโดนโจมตี

        OnHealthChanged?.Invoke(HealthPercent);
        ScreenEffectManager.Instance?.UpdateHealthPercent(HealthPercent);
        OnDamaged?.Invoke(damageToApply, hitPoint);

        // เรียกจอแดง!
        ScreenEffectManager.Instance?.PlayDamageEffect();
        ScreenEffectManager.Instance?.TriggerAdrenaline();

        // หักคะแนน และหักเวลาจำกัด (Katana Zero Time Penalty)
        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.ApplyPenalty("Damage Taken", LevelRatingManager.Instance.damagePenalty);
            LevelRatingManager.Instance.ApplyTimePenalty();
        }

        Debug.Log($"[Player] โดนยิง! เสียเลือด {damageToApply} | HP: {CurrentHealth}/{maxHealth}");

        // Knockback
        if (rb != null)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - hitPoint).normalized;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            // เริ่ม I-Frame
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    public void Die()
    {
        Debug.Log("[Player] ไข่เจียวตาย! GAME OVER!");
        OnDied?.Invoke();

        // แจ้ง LevelRatingManager
        if (LevelRatingManager.Instance != null)
        {
            LevelRatingManager.Instance.PlayerDied();
        }

        // ปิดการควบคุม
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.HideAmmoUI();
            shooting.enabled = false;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameplayHUD();
        }

        // ปิดเสียงหัวใจเต้นและเอฟเฟคเลือดน้อยทันทีเมื่อตาย
        ScreenEffectManager.Instance?.UpdateHealthPercent(0f);
        AudioManager.Instance?.StopHeartbeat();

        // เล่น Animation ตาย (ถ้ามี Animator)
        // ตอนนี้แค่เปลี่ยนสีเป็นแดง
        SetSpriteColor(new Color(0.5f, 0f, 0f, 0.7f));
    }

    // =========================================================
    // I-FRAME (INVINCIBILITY)
    // =========================================================

    private IEnumerator InvincibilityCoroutine()
    {
        _isInvincible = true;

        // กระพริบ
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkCoroutine());

        yield return new WaitForSeconds(invincibilityDuration);

        _isInvincible = false;

        // หยุดกระพริบ + แสดงตัวปกติ
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        SetSpriteAlpha(1f);
    }

    private IEnumerator BlinkCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            float alpha = Mathf.PingPong(elapsed * blinkRate, 1f) > 0.5f ? 0.3f : 1f;
            SetSpriteAlpha(alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetSpriteAlpha(1f);
    }

    // =========================================================
    // MANUAL OVERRIDES (For Shadow Dash etc.)
    // =========================================================
    public void SetManualInvincible(bool val)
    {
        _manualInvincible = val;
    }

    // =========================================================
    // HEALING / RESET
    // =========================================================

    /// <summary>รักษา HP</summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        float actualHeal = useHitBasedHealth ? 1f : amount;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + actualHeal);
        OnHealthChanged?.Invoke(HealthPercent);
        ScreenEffectManager.Instance?.UpdateHealthPercent(HealthPercent);
    }

    /// <summary>รีเซ็ต HP เต็ม</summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        _isInvincible = false;
        _manualInvincible = false;
        SetSpriteAlpha(1f);
        OnHealthChanged?.Invoke(HealthPercent);
        ScreenEffectManager.Instance?.UpdateHealthPercent(HealthPercent);
    }

    // =========================================================
    // VISUAL HELPERS
    // =========================================================

    private void SetSpriteAlpha(float alpha)
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    private void SetSpriteColor(Color color)
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                sr.color = color;
            }
        }
    }
}
