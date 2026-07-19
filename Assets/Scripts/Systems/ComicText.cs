using UnityEngine;
using System.Collections;

public class ComicText : MonoBehaviour
{
    private TextMesh textMesh;
    private float floatSpeed = 2f;
    private float fadeDuration = 0.8f;
    private float scaleDuration = 0.2f;

    public void Initialize(string text, Color color, float size = 1f)
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.2f;
        }

        textMesh.text = text;
        textMesh.color = color;
        
        // เพิ่มเงาดำ (Outline ปลอมๆ) ให้ดูเป็น Comic
        var outline = new GameObject("Outline");
        outline.transform.SetParent(transform);
        outline.transform.localPosition = new Vector3(0.05f, -0.05f, 0.1f);
        var outlineText = outline.AddComponent<TextMesh>();
        outlineText.font = textMesh.font;
        outlineText.anchor = textMesh.anchor;
        outlineText.alignment = textMesh.alignment;
        outlineText.characterSize = textMesh.characterSize;
        outlineText.text = text;
        outlineText.color = Color.black;

        transform.localScale = Vector3.zero;
        
        StartCoroutine(AnimateText(size, outlineText));
    }

    private IEnumerator AnimateText(float targetSize, TextMesh outlineText)
    {
        // 1. POP UP (เด้งขึ้นมา)
        float t = 0;
        Vector3 finalScale = Vector3.one * targetSize;
        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float normalizedTime = t / scaleDuration;
            // ใส่ Overshoot เล็กน้อย (Ease Out Back แบบบ้านๆ)
            float scaleValue = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f) * 1.2f;
            if (normalizedTime > 0.8f) scaleValue = Mathf.Lerp(1.2f, 1f, (normalizedTime - 0.8f) * 5f);
            
            transform.localScale = finalScale * scaleValue;
            yield return null;
        }
        transform.localScale = finalScale;

        // 2. FLOAT AND FADE (ลอยและจางหายไป)
        t = 0;
        Color startColor = textMesh.color;
        Color outlineStartColor = outlineText.color;
        Vector3 startPos = transform.position;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalizedTime = t / fadeDuration;
            
            // ลอยขึ้น
            transform.position = startPos + Vector3.up * (normalizedTime * floatSpeed);

            // จางหาย
            Color c = startColor;
            c.a = 1f - normalizedTime;
            textMesh.color = c;

            Color oc = outlineStartColor;
            oc.a = 1f - normalizedTime;
            outlineText.color = oc;

            yield return null;
        }

        Destroy(gameObject);
    }
}
