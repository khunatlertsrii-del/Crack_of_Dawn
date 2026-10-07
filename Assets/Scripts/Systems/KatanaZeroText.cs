using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class KatanaZeroText : MonoBehaviour
{
    [Header("Wave Animation")]
    public float waveSpeed = 15f;
    public float waveAmount = 4f;
    
    [Header("Shake Animation")]
    public float shakeAmount = 2.5f;

    private TMP_Text textComponent;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        // บังคับให้อัปเดต Mesh (ข้อมูล Vertex ของตัวอักษร) ก่อนแก้ไข
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        // ค้นหาคำที่ถูกครอบด้วยแท็ก <link>
        for (int i = 0; i < textInfo.linkCount; i++)
        {
            TMP_LinkInfo linkInfo = textInfo.linkInfo[i];
            string linkID = linkInfo.GetLinkID(); // ดึงชื่อ link เช่น "wave" หรือ "shake"

            for (int j = 0; j < linkInfo.linkTextLength; j++)
            {
                int charIndex = linkInfo.linkTextfirstCharacterIndex + j;
                if (!textInfo.characterInfo[charIndex].isVisible) continue;

                // หาตำแหน่ง Vertex ของตัวอักษรนี้
                int vertexIndex = textInfo.characterInfo[charIndex].vertexIndex;
                int materialIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = Vector3.zero;

                // เอฟเฟกต์ตัวหนังสือโยกไปมา (คลื่น)
                if (linkID == "wave")
                {
                    offset = new Vector3(0, Mathf.Sin(Time.unscaledTime * waveSpeed + charIndex) * waveAmount, 0);
                }
                // เอฟเฟกต์ตัวหนังสือสั่น (ความโกรธ/ตกใจ)
                else if (linkID == "shake")
                {
                    offset = new Vector3(Random.Range(-shakeAmount, shakeAmount), Random.Range(-shakeAmount, shakeAmount), 0);
                }

                // ขยับจุดทั้ง 4 จุดของตัวอักษร 1 ตัว
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }
        }

        // นำค่าที่ขยับแล้วไปใช้กับ Mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
