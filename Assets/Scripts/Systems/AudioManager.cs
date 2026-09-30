using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class MusicTrack
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public string trackName = "Unknown Track"; // ชื่อเพลงสำหรับแสดงใน UI Katana Zero
    }

    [Header("BGM (เสียงเพลง)")]
    public MusicTrack mainMenuBGM;
    public MusicTrack tutorialBGM;
    public MusicTrack levelBGM;
    public MusicTrack victoryBGM;
    public MusicTrack gameOverBGM;

    [Header("Player (ไข่เจียว)")]
    public AudioClip[] shootSounds;
    public AudioClip reloadSound;
    public AudioClip jumpSound;
    public AudioClip wallJumpSound;
    public AudioClip dashSlideSound;
    public AudioClip playerHitSound;

    [Header("Melee & Parry (ดาบและปัดป้อง)")]
    public AudioClip swordSpinSound;
    public AudioClip swordHitEnemySound;
    public AudioClip parrySuccessSound;

    [Header("Enemies (ไข่ดาว)")]
    public AudioClip enemyDeathSound;
    public AudioClip enemyShootSound;

    [Header("System & UI (ระบบและเมนู)")]
    public AudioClip slowMoEnterSound;
    public AudioClip slowMoExitSound;
    public AudioClip pauseOpenSound;
    public AudioClip uiHoverSound;
    public AudioClip uiClickSound;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float uiVolume = 1.0f;

    [Header("Slow-Mo Pitch Settings")]
    [Tooltip("ความเบี้ยวของเสียงเวลาสโลว์ (เช่น 0.5 คือเสียงทุ้มและยืดยาว)")]
    public float slowMoPitch = 0.6f;
    
    private AudioSource musicSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // สร้าง AudioSource สำหรับเพลงหลัก
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // ถอดตัวเองออกจาก Parent เพื่อป้องกันไม่ให้เผลอทำให้ Manager ตัวอื่นเป็นอมตะไปด้วย
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // ตรวจสอบฉากเริ่มต้น (กรณีเปิดเกมจากหน้าเมนู)
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name.Contains("Menu") || scene.buildIndex == 0)
        {
            PlayMusic(mainMenuBGM);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Safety net: รีเซ็ต TimeScale ทุกครั้งที่โหลดฉากใหม่
        // (ป้องกัน Victory/GameOver ค้าง Time.timeScale = 0 ข้ามฉาก)
        Time.timeScale = 1f;

        // ถ้าเป็นฉากเมนู ให้เล่นเพลงเมนู
        if (scene.name.Contains("Menu") || scene.buildIndex == 0)
        {
            if (mainMenuBGM != null && mainMenuBGM.clip != null) PlayMusic(mainMenuBGM);
        }
        else if (scene.name.ToLower().Contains("tutorial"))
        {
            // ถ้าเป็นฉากฝึกสอน
            if (tutorialBGM != null && tutorialBGM.clip != null) PlayMusic(tutorialBGM);
        }
        else
        {
            // ฉากอื่นที่ไม่ใช่เมนูและฝึกสอน = ฉากเลเวล → เล่นเพลงด่าน
            if (levelBGM != null && levelBGM.clip != null)
            {
                PlayMusic(levelBGM);
            }
        }
    }

    private void Update()
    {
        // ปรับ Pitch ของเพลงและเสียงประกอบตาม TimeScale ถ้าอยู่ในฉากเลเวล
        float currentPitch = 1f;
        
        if (SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive)
        {
            currentPitch = slowMoPitch;
        }

        // ทำให้ดนตรีค่อยๆ บิดเบี้ยวตามสโลว์โมชั่น
        musicSource.pitch = Mathf.Lerp(musicSource.pitch, currentPitch, Time.unscaledDeltaTime * 10f);

        // จัดการ list ของ SFX ที่กำลังเล่น และปรับ Pitch
        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            if (sfxSources[i] == null)
            {
                sfxSources.RemoveAt(i);
                continue;
            }

            if (!sfxSources[i].isPlaying)
            {
                // เล่นจบแล้ว ลบออกจาก list (ไม่ต้อง Destroy เพราะเป็น pool)
                sfxSources.RemoveAt(i);
            }
            else
            {
                // อัปเดต Pitch ของเสียง SFX (เสียง UI จะไม่ปรับตามนี้)
                sfxSources[i].pitch = Mathf.Lerp(sfxSources[i].pitch, currentPitch, Time.unscaledDeltaTime * 15f);
            }
        }
    }

    // ==========================================
    // MUSIC
    // ==========================================
    // Event สำหรับส่งชื่อเพลงไปให้ UI แสดง
    public event System.Action<string> OnMusicChanged;

    public void PlayMusic(MusicTrack track)
    {
        if (track == null || track.clip == null) return;
        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

        musicSource.clip = track.clip;
        // ปริมาณเสียง = global music volume * individual track volume
        musicSource.volume = musicVolume * track.volume;
        musicSource.Play();
        
        // ส่ง Event ไปบอก UI ว่าเปลี่ยนเพลงแล้ว
        OnMusicChanged?.Invoke(track.trackName);
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }

    public void SwitchToLevelBGM()
    {
        if (levelBGM != null && levelBGM.clip != null)
        {
            PlayMusic(levelBGM);
        }
    }

    // ==========================================
    // SOUND EFFECTS (SFX)
    // ==========================================
    
    // เล่นเสียง SFX ทั่วไป (จะได้รับผลกระทบจาก Slow-Mo)
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;
        CreateAudioSource(clip, sfxVolume * volumeMultiplier, true);
    }

    // เล่นเสียงแบบสุ่มจาก Array (เช่น เสียงยิงปืน)
    public void PlayRandomSFX(AudioClip[] clips, float volumeMultiplier = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip randomClip = clips[Random.Range(0, clips.Length)];
        PlaySFX(randomClip, volumeMultiplier);
    }

    // ==========================================
    // UI SOUNDS
    // ==========================================
    
    // เล่นเสียง UI (จะไม่โดนดึง Pitch ตอน Slow-Mo)
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null) return;
        CreateAudioSource(clip, uiVolume, false);
    }

    // ==========================================
    // HELPER — ใช้ Pool แทนการสร้าง/ลบ GameObject ทุกครั้ง
    // ==========================================
    private const int AUDIO_POOL_SIZE = 16;
    private AudioSource[] audioPool;
    private int audioPoolIndex = 0;

    private void InitAudioPool()
    {
        audioPool = new AudioSource[AUDIO_POOL_SIZE];
        for (int i = 0; i < AUDIO_POOL_SIZE; i++)
        {
            GameObject soundObj = new GameObject("SFXPool_" + i);
            soundObj.transform.SetParent(transform);
            AudioSource src = soundObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            audioPool[i] = src;
        }
    }

    private void CreateAudioSource(AudioClip clip, float volume, bool isSFX)
    {
        if (audioPool == null) InitAudioPool();

        // หา AudioSource ที่ว่างอยู่จากกะบะ (Round-Robin)
        AudioSource source = audioPool[audioPoolIndex];
        audioPoolIndex = (audioPoolIndex + 1) % AUDIO_POOL_SIZE;

        source.Stop(); // หยุดเสียงเก่าถ้ากำลังเล่นอยู่
        source.clip = clip;
        source.volume = volume;
        
        // ถ้าเป็น SFX ปกติ ให้เซ็ต Pitch เริ่มต้นตามสถานะปัจจุบัน
        if (isSFX && SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive)
        {
            source.pitch = slowMoPitch;
        }
        else
        {
            source.pitch = 1f;
        }

        source.Play();

        if (isSFX && !sfxSources.Contains(source))
        {
            sfxSources.Add(source); // เก็บลง List เพื่ออัปเดต Pitch เรียลไทม์
        }
    }
}
