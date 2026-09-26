using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// สคริปต์ปิด-เปิดแสง 2D อัตโนมัติ (Culling)
/// ช่วยลดความแลคจาก 2D Lights จำนวนมากในฉาก
/// โดยจะปิดแสงที่อยู่นอกจอ และเปิดเฉพาะแสงที่อยู่ในจอ
/// </summary>
public class LightCuller2D : MonoBehaviour
{
    [Tooltip("ระยะเผื่อขอบจอ (ถ้าตั้ง 5 แสงจะเปิดก่อนเข้าจอ 5 หน่วย)")]
    public float cullMargin = 5f;

    [Tooltip("อัปเดตทุกๆ กี่วินาที (0.2 คืออัปเดต 5 ครั้งต่อวินาที ช่วยประหยัด CPU)")]
    public float updateInterval = 0.2f;

    private Light2D[] allLights;
    private Camera mainCam;
    private float timer;

    private void Start()
    {
        mainCam = Camera.main;
        // หาแสง 2D ทั้งหมดในฉากตอนเริ่มเกม
        allLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (mainCam == null || allLights == null || allLights.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            CullLights();
        }
    }

    private void CullLights()
    {
        // หาขอบเขตของกล้อง
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        
        Vector3 camPos = mainCam.transform.position;
        
        // ขอบเขตที่แสงควรจะทำงาน (รวม margin แล้ว)
        float minX = camPos.x - (camWidth / 2f) - cullMargin;
        float maxX = camPos.x + (camWidth / 2f) + cullMargin;
        float minY = camPos.y - (camHeight / 2f) - cullMargin;
        float maxY = camPos.y + (camHeight / 2f) + cullMargin;

        foreach (var light in allLights)
        {
            if (light == null) continue;

            // ไม่ต้องยุ่งกับ Global Light (แสงสว่างทั้งฉาก) เพราะถ้าปิดฉากจะมืดสนิท
            if (light.lightType == Light2D.LightType.Global) continue;

            // เช็คว่าตำแหน่งของแสงอยู่ในกรอบที่กำหนดไหม
            Vector3 lightPos = light.transform.position;
            bool isInside = lightPos.x >= minX && lightPos.x <= maxX && 
                            lightPos.y >= minY && lightPos.y <= maxY;

            // ปิด/เปิด Component Light2D (ดีกว่าปิด GameObject เพราะไม่กระทบ Script อื่น)
            if (light.enabled != isInside)
            {
                light.enabled = isInside;
            }
        }
    }
}
