using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerParry : MonoBehaviour
{
    [Header("Parry Settings")]
    [Tooltip("ระยะเวลาการใช้งาน Parry")]
    public float maxParryMeter = 100f;
    [Tooltip("อัตราการลดของหลอด (ต่อวินาที)")]
    public float drainRate = 50f;
    [Tooltip("อัตราการฟื้นฟูของหลอด")]
    public float regenRate = 30f;
    [Tooltip("รัศมีการปัดกระสุนและทำดาเมจ")]
    public float parryRadius = 1.0f;
    [Tooltip("เปอร์เซ็นต์ขั้นต่ำที่ต้องชาร์จใหม่หลังหลอดหมด (เช่น 0.2 = 20%)")]
    public float overheatRecoveryThreshold = 0.2f;

    [Header("Damage Zone Settings")]
    [Tooltip("ดาเมจที่ทำได้ต่อครั้ง (เบาๆ แต่ฟันรัว)")]
    public float parryDamage = 5f;
    [Tooltip("ความถี่ในการทำดาเมจ (วินาที)")]
    public float parryDamageRate = 0.1f;
    [Tooltip("เกจที่ได้คืนเมื่อฟันโดนศัตรู (เปอร์เซ็นต์ของหลอดสูงสุดต่อการฟันโดน 1 ตัว)")]
    public float meterRegenPerHit = 5f;

    [Header("UI")]
    [Tooltip("Slider สำหรับหลอดปัดกระสุน")]
    public Slider parrySlider;

    [Header("Animation & Visuals")]
    [Tooltip("ชิ้นส่วนที่จะถูกซ่อนตอน Parry (เช่น หัว, แขน, ปืน)")]
    public GameObject[] objectsToHide;
    [Tooltip("Animator สำหรับเล่นท่าฟัน")]
    public Animator animator;
    [Tooltip("GameObject เอฟเฟกต์ฟันดาบ 360 องศา (ใส่ได้หลายอัน)")]
    public GameObject[] parryVisualEffects;

    private float currentParryMeter;
    private bool isParrying;
    private bool isOverheated; // เช็คว่าหลอดพัง (ฮีท) อยู่หรือไม่
    public bool IsParrying => isParrying;
    private CircleCollider2D parryCollider;
    private InputAction parryAction;
    private AudioSource spinAudioSource;
    private Transform parryVisualTransform;
    private float damageTimer;

    private void Awake()
    {
        currentParryMeter = maxParryMeter;

        // สร้าง AudioSource สำหรับเสียงหมุนดาบ
        spinAudioSource = gameObject.AddComponent<AudioSource>();
        spinAudioSource.loop = true;
        spinAudioSource.playOnAwake = false;
        spinAudioSource.volume = 0.5f;

        // สร้าง Collider ไว้ปัดกระสุน
        GameObject parryZone = new GameObject("ParryZone");
        parryZone.transform.SetParent(transform);
        parryZone.transform.localPosition = Vector3.zero;
        
        parryCollider = parryZone.AddComponent<CircleCollider2D>();
        parryCollider.radius = parryRadius;
        parryCollider.isTrigger = true;
        parryCollider.enabled = false;

        // ปิด VFX ทุกอันไว้ตั้งแต่เริ่มเกม
        if (parryVisualEffects != null)
        {
            foreach (var vfx in parryVisualEffects)
            {
                if (vfx != null) vfx.SetActive(false);
            }
        }

        // ใส่ Script ไว้จับ OnTrigger
        var zoneScript = parryZone.AddComponent<ParryZone>();
        zoneScript.Initialize(this);

        parryAction = new InputAction("Parry", InputActionType.Button);
        parryAction.AddBinding("<Keyboard>/w");
        parryAction.AddBinding("<Gamepad>/buttonNorth"); // Y / Triangle
    }

    private void OnEnable() => parryAction.Enable();
    private void OnDisable() => parryAction.Disable();

    private void Update()
    {
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        // ยกเลิก Overheat เมื่อหลอดชาร์จกลับมาถึง 20% ของหลอดเต็ม (หรือตามที่ตั้งค่าไว้)
        if (isOverheated && currentParryMeter >= maxParryMeter * overheatRecoveryThreshold)
        {
            isOverheated = false;
        }

        if (parryAction.IsPressed() && currentParryMeter > 0 && !isOverheated)
        {
            if (!isParrying)
            {
                // เพิ่งเริ่มหมุนดาบ
                if (AudioManager.Instance != null && AudioManager.Instance.swordSpinSound != null)
                {
                    spinAudioSource.clip = AudioManager.Instance.swordSpinSound;
                    if (!spinAudioSource.isPlaying) spinAudioSource.Play();
                }
            }

            isParrying = true;
            parryCollider.enabled = true;
            if (parryVisualEffects != null)
            {
                foreach (var vfx in parryVisualEffects)
                {
                    if (vfx != null) vfx.SetActive(true);
                }
            }

            // กวาดหาทั้งศัตรู (ทำดาเมจ) และกระสุน (ปัดทิ้ง) ในวงแหวน
            Collider2D[] allHits = Physics2D.OverlapCircleAll(transform.position, parryRadius);
            
            // 1. ปัดกระสุนชัวร์ๆ (เช็คทุกเฟรม)
            foreach (var hit in allHits)
            {
                var bullet = hit.GetComponent<EnemyBullet>();
                if (bullet != null && !bullet.IsDeflected)
                {
                    DeflectBullet(bullet);
                }
            }

            // 2. ทำดาเมจรัวๆ แบบเลื่อยไฟฟ้า!
            damageTimer += Time.deltaTime;
            if (damageTimer >= parryDamageRate)
            {
                damageTimer = 0f;
                bool hitAtLeastOne = false;

                foreach (var hit in allHits)
                {
                    // เช็คด้วย Component แทน Tag เพราะศัตรูอาจไม่ได้ตั้ง Tag "Enemy" ไว้
                    var enemy = hit.GetComponent<FriedEggEnemy>();
                    if (enemy != null)
                    {
                        hitAtLeastOne = true;
                        Vector2 hitDirection = (hit.transform.position - transform.position).normalized;
                        enemy.TakeDamage(parryDamage, hit.transform.position, false, hitDirection);

                        // เลือดสาด 360 องศา (Particle Effect รันต่อศัตรูได้ ไม่แลค)
                        if (HitEffect.Instance != null) HitEffect.Instance.PlayMeleeHitEffect(hit.transform.position);

                        // ฟื้นฟูหลอด Parry ตามที่ฟันโดน
                        currentParryMeter += meterRegenPerHit;
                    }
                }

                if (hitAtLeastOne)
                {
                    // เรียกของหนักๆ (UI เลือดแปะจอ, จอกระพริบ, เล่นเสียง) แค่ 1 ครั้งต่อการฟัน 1 รอบ (กันแลค/เสียงซ้อน)
                    if (ScreenEffectManager.Instance != null) 
                    {
                        ScreenEffectManager.Instance.PlayScreenBloodSplatter(Random.Range(1, 3));
                        ScreenEffectManager.Instance.TriggerAdrenaline();
                    }
                    if (AudioManager.Instance != null && AudioManager.Instance.swordHitEnemySound != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.swordHitEnemySound, 0.5f);
                    }
                }
            }

            currentParryMeter -= drainRate * Time.deltaTime;
            
            // ซ่อนชิ้นส่วนตอน Parry และเล่นอนิเมชั่น
            foreach(var obj in objectsToHide) if(obj != null) obj.SetActive(false);
            if (animator != null) animator.SetBool("isParrying", true);

            // ถ้าหลอดหมด ให้เข้าสู่โหมด Overheat (ใช้งานไม่ได้จนกว่าจะฟื้นฟูถึงจุดที่กำหนด)
            if (currentParryMeter <= 0)
            {
                isOverheated = true;
            }
        }
        else
        {
            if (isParrying)
            {
                // เพิ่งหยุดหมุนดาบ
                spinAudioSource.Stop();
            }

            isParrying = false;
            parryCollider.enabled = false;
            if (parryVisualEffects != null)
            {
                foreach (var vfx in parryVisualEffects)
                {
                    if (vfx != null) vfx.SetActive(false);
                }
            }
            
            // ฟื้นฟู
            currentParryMeter += regenRate * Time.deltaTime;
            
            // เปิดชิ้นส่วนกลับมาตอนเลิก Parry และหยุดเล่นอนิเมชั่น
            foreach(var obj in objectsToHide) if(obj != null) obj.SetActive(true);
            if (animator != null) animator.SetBool("isParrying", false);
        }

        currentParryMeter = Mathf.Clamp(currentParryMeter, 0, maxParryMeter);

        if (parrySlider != null)
        {
            float percent = currentParryMeter / maxParryMeter;
            parrySlider.value = percent;
            parrySlider.gameObject.SetActive(currentParryMeter < maxParryMeter || isParrying);

            // รองรับ Center Fill
            if (parrySlider.fillRect == null)
            {
                Image[] images = parrySlider.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.name == "Fill" || img.type == Image.Type.Simple || img.type == Image.Type.Sliced)
                    {
                        img.transform.localScale = new Vector3(percent, 1f, 1f);
                        break;
                    }
                }
            }
        }

        // อัปเดตขนาดให้ตรงกับค่าใน Inspector เสมอ (เผื่อคุณปรับแก้ตอนเล่น)
        if (parryCollider != null) parryCollider.radius = parryRadius;
        if (parryVisualTransform != null) parryVisualTransform.localScale = Vector3.one * parryRadius * 2f;
    }

    public void DeflectBullet(EnemyBullet bullet)
    {
        // คำนวณทิศทางสะท้อนไปยังเมาส์
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mousePos3D = new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(Camera.main.transform.position.z));
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(mousePos3D);
        Vector2 deflectDir = (mousePos - (Vector2)bullet.transform.position).normalized;
        
        bullet.Deflect(deflectDir);
        
        // เล่น Effect ปัดกระสุน!
        HitEffect.Instance?.PlayParryEffect(bullet.transform.position);

        // ระเบิดขอบจอ (Adrenaline!)
        ScreenEffectManager.Instance?.TriggerAdrenaline();

        // เสียงแพรี่
        if (AudioManager.Instance != null && AudioManager.Instance.parrySuccessSound != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.parrySuccessSound, 0.8f);
        }
    }
}

public class ParryZone : MonoBehaviour
{
    private PlayerParry parrySystem;

    public void Initialize(PlayerParry parrySystem)
    {
        this.parrySystem = parrySystem;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ปัญหา: กระสุนศัตรูที่สร้างใหม่ไม่ได้ตั้ง Tag เป็น EnemyBullet 
        // วิธีแก้: ดึง Component ตรงๆ เลย ไม่ต้องเช็ค Tag
        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            parrySystem.DeflectBullet(bullet);
        }
    }
}
