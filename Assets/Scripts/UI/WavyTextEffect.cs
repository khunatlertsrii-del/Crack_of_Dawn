using UnityEngine;
using TMPro;

/// <summary>
/// เอฟเฟกต์ตัวอักษรเลื้อยขึ้น-ลงไปมาแบบคลื่น (Wavy Text)
/// แปะสคริปต์นี้ที่ TextMeshPro หรือ TextMeshProUGUI ได้เลย!
/// </summary>
public class WavyTextEffect : MonoBehaviour
{
    [Header("Wave Settings (ตั้งค่าคลื่น)")]
    [Tooltip("ความสูงของคลื่น (ยิ่งเยอะตัวอักษรยิ่งขยับขึ้น-ลงเยอะ)")]
    [SerializeField] private float waveHeight = 8f;

    [Tooltip("ความเร็วของคลื่น (ยิ่งเยอะยิ่งเลื้อยเร็ว)")]
    [SerializeField] private float waveSpeed = 3f;

    [Tooltip("ความถี่ของคลื่น (ยิ่งเยอะคลื่นยิ่งถี่ ตัวอักษรแต่ละตัวจะห่างเฟสกันมาก)")]
    [SerializeField] private float waveFrequency = 0.5f;

    [Header("Advanced (ตั้งค่าเพิ่มเติม)")]
    [Tooltip("ใช้ UnscaledTime เพื่อให้เลื้อยต่อแม้เกม Pause หรือ Slow-Mo")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("เปิด/ปิด เอฟเฟกต์")]
    [SerializeField] private bool isActive = true;

    private TMP_Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (!isActive || textComponent == null) return;

        // บังคับให้ TMP อัปเดต mesh ก่อน
        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // ข้ามตัวอักษรที่มองไม่เห็น (เช่น space)
            if (!charInfo.isVisible) continue;

            // ดึง vertices ของตัวอักษรนี้
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // คำนวณ offset แบบ sine wave
            // ตัวอักษรแต่ละตัวจะมี phase ต่างกันตาม index ทำให้เกิดเอฟเฟกต์เลื้อย
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float offsetY = Mathf.Sin(time * waveSpeed + i * waveFrequency) * waveHeight;

            // ขยับ vertex ทั้ง 4 มุมของตัวอักษรขึ้น-ลง
            vertices[vertexIndex + 0] += new Vector3(0, offsetY, 0);
            vertices[vertexIndex + 1] += new Vector3(0, offsetY, 0);
            vertices[vertexIndex + 2] += new Vector3(0, offsetY, 0);
            vertices[vertexIndex + 3] += new Vector3(0, offsetY, 0);
        }

        // อัปเดต mesh กลับไปที่ TMP
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
