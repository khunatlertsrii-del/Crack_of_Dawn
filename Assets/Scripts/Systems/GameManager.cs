// ============================================================
// GameManager.cs — จัดการสถานะเกม (Singleton)
// Omelet Rush © 2026
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Singleton จัดการสถานะเกมทั้งหมด:
/// Playing → GameOver / Victory
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ===================== Game State =====================
    public enum GameState { Playing, Paused, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ===================== Events =====================
    /// <summary>เรียกเมื่อสถานะเกมเปลี่ยน</summary>
    public event Action<GameState> OnGameStateChanged;
    /// <summary>เรียกเมื่อศัตรูถูกฆ่า</summary>
    public event Action<int> OnEnemyKilled;
    /// <summary>เรียกเมื่อ Score เปลี่ยน</summary>
    public event Action<int> OnScoreChanged;

    // ===================== Stats =====================
    [Header("Game Stats")]
    public int TotalKills { get; private set; } = 0;
    public int Score { get; private set; } = 0;
    public int CriticalKills { get; private set; } = 0;

    [Header("Settings")]
    [SerializeField] private int scorePerKill = 100;
    [SerializeField] private int scorePerCriticalKill = 500;

    // ===================== Enemy Tracking =====================
    private int activeEnemyCount = 0;
    private int totalEnemiesInLevel = 0;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // ป้องกันไม่ให้โค้ดส่วนล่างทำงาน
        }

        // สร้าง ScreenEffectManager ถ้ายังไม่มี
        if (ScreenEffectManager.Instance == null)
        {
            GameObject screenFX = new GameObject("ScreenEffectManager");
            screenFX.AddComponent<ScreenEffectManager>();
        }
        Instance = this;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    private void Update()
    {
        // Pause toggle
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    /// <summary>เปลี่ยนสถานะเกม</summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
            case GameState.Victory:
                Time.timeScale = 0f;
                break;
        }

        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] State → {newState}");
    }

    /// <summary>สลับ Pause/Playing</summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    /// <summary>เรียกเมื่อ Player ตาย</summary>
    public void PlayerDied()
    {
        if (CurrentState != GameState.Playing) return;
        Debug.Log("[GameManager] Player Died! GAME OVER");
        SetState(GameState.GameOver);
    }

    /// <summary>เรียกเมื่อศัตรูถูกฆ่า</summary>
    public void EnemyKilled(bool wasCritical)
    {
        TotalKills++;
        if (wasCritical)
        {
            CriticalKills++;
            Score += scorePerCriticalKill;
        }
        else
        {
            Score += scorePerKill;
        }

        activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
        OnEnemyKilled?.Invoke(TotalKills);
        OnScoreChanged?.Invoke(Score);

        Debug.Log($"[GameManager] Enemy killed! Total: {TotalKills} | Score: {Score} | Critical: {CriticalKills}");

        // Check victory (ถ้าศัตรูหมด)
        if (activeEnemyCount <= 0 && totalEnemiesInLevel > 0)
        {
            SetState(GameState.Victory);
        }
    }

    /// <summary>ลงทะเบียนศัตรูที่ spawn เข้ามา</summary>
    public void RegisterEnemy()
    {
        activeEnemyCount++;
        totalEnemiesInLevel++;
    }

    /// <summary>รีสตาร์ทด่าน</summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>รีเซ็ตสถิติ</summary>
    public void ResetStats()
    {
        TotalKills = 0;
        Score = 0;
        CriticalKills = 0;
        activeEnemyCount = 0;
        totalEnemiesInLevel = 0;
    }
}
