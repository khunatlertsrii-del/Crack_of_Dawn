using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// สคริปต์ปิด-เปิดแสง 2D อัตโนมัติ (Culling) + ควบคุมเงาฉลาด (Smart Shadows)
/// ช่วยลดความแลคจาก 2D Lights จำนวนมากในฉากได้อย่างมหาศาล
/// </summary>
public class LightCuller2D : MonoBehaviour
{
    [Tooltip("ระยะเผื่อขอบจอ (ถ้าตั้ง 5 แสงจะเปิดก่อนเข้าจอ 5 หน่วย)")]
    public float cullMargin = 5f;

    [Tooltip("อัปเดตทุกๆ กี่วินาที (0.2 คืออัปเดต 5 ครั้งต่อวินาที ช่วยประหยัด CPU)")]
    public float updateInterval = 0.2f;

    [Header("Shadow Optimization")]
    [Tooltip("เปิดระบบควบคุมเงาอัจฉริยะ (ให้เฉพาะไฟใกล้ๆ ผู้เล่นที่สร้างเงาได้)")]
    public bool optimizeShadows = true;
    
    [Tooltip("จำนวนไฟสูงสุดบนจอที่จะให้มีเงาดำ (แนะนำ 2-4 จะลื่นมาก)")]
    public int maxShadowCastingLights = 4;

    private struct LightData
    {
        public Light2D light;
        public bool originalShadows;
    }

    private List<LightData> allLights = new List<LightData>();
    private Camera mainCam;
    private float timer;
    private Transform playerTransform;

    private void Start()
    {
        mainCam = Camera.main;
        
        // หา Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // หาแสง 2D ทั้งหมดในฉากตอนเริ่มเกม แล้วจำค่าเดิมของมันไว้
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global) continue; // ข้าม Global
            allLights.Add(new LightData { light = l, originalShadows = l.shadowsEnabled });
        }
    }

    private void Update()
    {
        if (mainCam == null || allLights.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            CullLights();
        }
    }

    private void CullLights()
    {
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;
        
        float minX = camPos.x - (camWidth / 2f) - cullMargin;
        float maxX = camPos.x + (camWidth / 2f) + cullMargin;
        float minY = camPos.y - (camHeight / 2f) - cullMargin;
        float maxY = camPos.y + (camHeight / 2f) + cullMargin;

        List<LightData> activeLightsOnScreen = new List<LightData>();

        // 1. ปิด/เปิด ไฟตามขอบจอ
        for (int i = 0; i < allLights.Count; i++)
        {
            LightData data = allLights[i];
            if (data.light == null) continue;

            Vector3 lightPos = data.light.transform.position;
            bool isInside = lightPos.x >= minX && lightPos.x <= maxX && 
                            lightPos.y >= minY && lightPos.y <= maxY;

            if (data.light.enabled != isInside)
            {
                data.light.enabled = isInside;
            }

            if (isInside && data.originalShadows)
            {
                activeLightsOnScreen.Add(data);
            }
        }

        // 2. จัดการเงา (เก็บเงาให้ไฟที่ใกล้ผู้เล่นที่สุดเท่านั้น)
        if (optimizeShadows && playerTransform != null && activeLightsOnScreen.Count > maxShadowCastingLights)
        {
            // เรียงลำดับไฟตามระยะห่างจากผู้เล่น (ใกล้ไปไกล)
            activeLightsOnScreen.Sort((a, b) => 
                Vector2.Distance(playerTransform.position, a.light.transform.position)
                .CompareTo(Vector2.Distance(playerTransform.position, b.light.transform.position))
            );

            // เปิดเงาให้แค่ N ตัวแรก ที่เหลือปิดเงาชั่วคราว
            for (int i = 0; i < activeLightsOnScreen.Count; i++)
            {
                bool shouldHaveShadow = (i < maxShadowCastingLights);
                if (activeLightsOnScreen[i].light.shadowsEnabled != shouldHaveShadow)
                {
                    activeLightsOnScreen[i].light.shadowsEnabled = shouldHaveShadow;
                }
            }
        }
        else if (optimizeShadows && activeLightsOnScreen.Count <= maxShadowCastingLights)
        {
            // ถ้าไฟน้อยกว่าลิมิต ก็ให้เปิดเงาตามปกติให้ครบทุกดวง
            foreach (var data in activeLightsOnScreen)
            {
                if (!data.light.shadowsEnabled) data.light.shadowsEnabled = true;
            }
        }
    }
}
