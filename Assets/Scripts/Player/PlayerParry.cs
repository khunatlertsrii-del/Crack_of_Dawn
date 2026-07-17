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

    [Header("Damage Zone Settings")]
    [Tooltip("ดาเมจที่ทำได้ต่อครั้ง (เบาๆ แต่ฟันรัว)")]
    public float parryDamage = 5f;
    [Tooltip("ความถี่ในการทำดาเมจ (วินาที)")]
    public float parryDamageRate = 0.1f;

    [Header("UI")]
    [Tooltip("Slider สำหรับหลอดปัดกระสุน")]
    public Slider parrySlider;

    private float currentParryMeter;
    private bool isParrying;
    public bool IsParrying => isParrying;
    private CircleCollider2D parryCollider;
    private InputAction parryAction;
    private Transform parryVisualTransform;
    private float damageTimer;

    private void Awake()
    {
        currentParryMeter = maxParryMeter;

        // สร้าง Collider ไว้ปัดกระสุน
        GameObject parryZone = new GameObject("ParryZone");
        parryZone.transform.SetParent(transform);
        parryZone.transform.localPosition = Vector3.zero;
        
        parryCollider = parryZone.AddComponent<CircleCollider2D>();
        parryCollider.radius = parryRadius;
        parryCollider.isTrigger = true;
        parryCollider.enabled = false;

        // สร้าง Effect ฟันรอบตัว (Placeholder)
        GameObject visual = new GameObject("ParryVisual");
        visual.transform.SetParent(parryZone.transform);
        visual.transform.localPosition = Vector3.zero;
        var sr = visual.AddComponent<SpriteRenderer>();
        
        // สร้างรูปวงกลมง่ายๆ สีฟ้าใสๆ
        Texture2D tex = new Texture2D(64, 64);
        Color[] px = new Color[64 * 64];
        Vector2 center = new Vector2(32, 32);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // ทำเป็นวงแหวน (ขอบเข้ม ตรงกลางใส)
                if (dist < 32f && dist > 26f) px[y * 64 + x] = Color.white;
                else px[y * 64 + x] = Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
        sr.color = new Color(0f, 1f, 1f, 0.5f); // สีฟ้าสว่าง
        sr.sortingOrder = 10;
        
        parryVisualTransform = visual.transform;
        parryVisualTransform.localScale = Vector3.one * parryRadius * 2f;
        visual.SetActive(false);

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
        if (parryAction.IsPressed() && currentParryMeter > 0)
        {
            isParrying = true;
            parryCollider.enabled = true;
            if (parryVisualTransform != null)
            {
                parryVisualTransform.gameObject.SetActive(true);
                // หมุนเอฟเฟคฟันติ้วๆ
                parryVisualTransform.Rotate(0, 0, 1000f * Time.deltaTime);
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
                foreach (var hit in allHits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        var enemy = hit.GetComponent<FriedEggEnemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(parryDamage, hit.transform.position, false);
                        }
                    }
                }
            }

            currentParryMeter -= drainRate * Time.deltaTime;
        }
        else
        {
            isParrying = false;
            parryCollider.enabled = false;
            if (parryVisualTransform != null) parryVisualTransform.gameObject.SetActive(false);
            
            // ฟื้นฟู
            currentParryMeter += regenRate * Time.deltaTime;
        }

        currentParryMeter = Mathf.Clamp(currentParryMeter, 0, maxParryMeter);

        if (parrySlider != null)
        {
            parrySlider.value = currentParryMeter / maxParryMeter;
            parrySlider.gameObject.SetActive(currentParryMeter < maxParryMeter || isParrying);
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
