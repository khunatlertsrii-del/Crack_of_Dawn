using UnityEngine;
using System.Collections;

public class SmokeParticle : MonoBehaviour
{
    private SpriteRenderer sr;
    private float fadeDuration;
    private float expandSpeed;
    private Vector3 initialScale;
    private Vector2 velocity;

    public void Initialize(float duration, float expandAmount, Vector2 initialVelocity)
    {
        sr = GetComponent<SpriteRenderer>();
        fadeDuration = duration;
        expandSpeed = expandAmount;
        initialScale = transform.localScale;
        velocity = initialVelocity;

        StartCoroutine(SmokeRoutine());
    }

    private IEnumerator SmokeRoutine()
    {
        float timer = 0f;
        Color initialColor = sr.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            // Fade out
            Color c = initialColor;
            c.a = Mathf.Lerp(initialColor.a, 0f, t);
            if (sr != null) sr.color = c;

            // Move manually (No Rigidbody)
            transform.position += (Vector3)velocity * Time.deltaTime;
            // ลอยขึ้นนิดๆ (จำลอง Gravity ขาขึ้น)
            velocity.y += 0.5f * Time.deltaTime;
            // ค่อยๆ ชะลอความเร็ว (Air Friction)
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 2f);

            // Expand
            transform.localScale = initialScale * (1f + t * expandSpeed);

            yield return null;
        }

        Destroy(gameObject);
    }
}
