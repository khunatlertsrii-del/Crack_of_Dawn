using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private float length, startpos;
    private GameObject cam;
    
    [Tooltip("ความหนืดของการเลื่อน: \n0 = ล็อกติดกล้องเลย (เช่น ท้องฟ้าไกลสุด)\n0.5 = เลื่อนช้าๆ (เช่น ตึกเมือง)\n1 = เลื่อนเร็วเท่าผู้เล่น (เช่น ฉากหน้า)")]
    [Range(0f, 1f)]
    public float parallaxEffect = 0.5f;

    void Start()
    {
        cam = Camera.main.gameObject;
        startpos = transform.position.x;
        
        // ถ้ารูปมี SpriteRenderer จะเอาไว้ทำระบบวนลูปภาพ (Infinite Loop) ในอนาคตได้
        if (GetComponent<SpriteRenderer>() != null)
        {
            length = GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void Update()
    {
        // ระยะที่กล้องเดินไปจริงๆ
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        // ระยะที่เราต้องขยับแบคกราวด์ตาม
        float dist = (cam.transform.position.x * parallaxEffect);

        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // ระบบลูปแบคกราวด์อัตโนมัติ (ถ้าเดินทะลุกรอบรูป มันจะเอารูปมาต่อให้เอง)
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}
