using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ระบบแสดงป้ายชื่อเพลงแบบ Katana Zero (NOW PLAYING)
/// ให้เอาไปแปะไว้ที่ GameObject ใน Canvas ที่มี TextMeshPro และ Panel พื้นหลัง
/// </summary>
public class NowPlayingUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text trackNameText;
    public RectTransform uiPanel;

    [Header("Settings")]
    public float displayDuration = 4f; // โชว์กี่วินาที
    public float slideSpeed = 5f; // ความเร็วสไลด์เข้า/ออก

    private Vector2 hiddenPos;
    private Vector2 shownPos;
    private Coroutine slideCoroutine;

    private void Start()
    {
        if (uiPanel == null)
            uiPanel = GetComponent<RectTransform>();

        // จำตำแหน่งตอนเปิด (โชว์)
        shownPos = uiPanel.anchoredPosition;

        // คำนวณตำแหน่งซ่อน (สไลด์ลงไปล่างจอ หรือขวาจอ)
        // สมมติว่าสไลด์ลงไปข้างล่าง
        hiddenPos = new Vector2(shownPos.x, shownPos.y - uiPanel.rect.height - 200f);

        // ตั้งค่าเริ่มต้นให้ซ่อนอยู่
        uiPanel.anchoredPosition = hiddenPos;

        // สมัครรับ Event เมื่อเปลี่ยนเพลง
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicChanged += ShowNowPlaying;
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicChanged -= ShowNowPlaying;
        }
    }

    /// <summary>
    /// ถูกเรียกอัตโนมัติเมื่อเพลงใน AudioManager เปลี่ยน
    /// </summary>
    public void ShowNowPlaying(string trackName)
    {
        // ไม่ต้องแสดงชื่อเพลงตอน Game Over หรือถ้าเป็นเพลง Game Over
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver) return;
        if (LevelRatingManager.Instance != null && !LevelRatingManager.Instance.isLevelActive && LevelRatingManager.Instance.RemainingTime <= 0f) return;
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverBGM != null && trackName == AudioManager.Instance.gameOverBGM.trackName) return;
        if (string.IsNullOrEmpty(trackName) || trackName.ToLower().Contains("game over") || trackName.ToLower().Contains("gameover")) return;

        if (trackNameText != null)
        {
            // ทำตัวหนังสือให้เหมือน Katana Zero (มีสีๆ หน่อย)
            trackNameText.text = $"<color=#00FFFF>NOW PLAYING</color>\n{trackName}";
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideRoutine());
    }

    /// <summary>
    /// สั่งซ่อนทันทีเมื่อจบด่านหรือ Game Over
    /// </summary>
    public void HideImmediately()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        if (uiPanel != null)
        {
            uiPanel.anchoredPosition = hiddenPos;
        }
    }

    private IEnumerator SlideRoutine()
    {
        // สไลด์เข้า (เด้งขึ้นมา)
        while (Vector2.Distance(uiPanel.anchoredPosition, shownPos) > 1f)
        {
            uiPanel.anchoredPosition = Vector2.Lerp(uiPanel.anchoredPosition, shownPos, slideSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        uiPanel.anchoredPosition = shownPos;

        // รอเวลา
        yield return new WaitForSecondsRealtime(displayDuration);

        // สไลด์ออก (มุดลงไป)
        while (Vector2.Distance(uiPanel.anchoredPosition, hiddenPos) > 1f)
        {
            uiPanel.anchoredPosition = Vector2.Lerp(uiPanel.anchoredPosition, hiddenPos, slideSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        uiPanel.anchoredPosition = hiddenPos;
    }
}
