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
    }

    [Header("BGM (เสียงเพลง)")]
    public MusicTrack mainMenuBGM;
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
        DontDestroyOnLoad(gameObject); // ให้ AudioManager อยู่ข้ามฉากได้

        // สร้าง AudioSource สำหรับเพลงหลัก
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
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
        else
        {
            // ฉากอื่นที่ไม่ใช่เมนู = ฉากเลเวล → เล่นเพลงด่าน
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

        // จัดการลบ AudioSource ที่เล่นจบแล้ว และปรับ Pitch ของเสียงที่กำลังเล่นอยู่
        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            if (sfxSources[i] == null)
            {
                sfxSources.RemoveAt(i);
                continue;
            }

            if (!sfxSources[i].isPlaying)
            {
                Destroy(sfxSources[i].gameObject);
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
    public void PlayMusic(MusicTrack track)
    {
        if (track == null || track.clip == null) return;
        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

        musicSource.clip = track.clip;
        // ปริมาณเสียง = global music volume * individual track volume
        musicSource.volume = musicVolume * track.volume;
        musicSource.Play();
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
    // HELPER
    // ==========================================
    private void CreateAudioSource(AudioClip clip, float volume, bool isSFX)
    {
        GameObject soundObj = new GameObject("Sound_" + clip.name);
        soundObj.transform.SetParent(transform);
        AudioSource source = soundObj.AddComponent<AudioSource>();
        
        source.clip = clip;
        source.volume = volume;
        source.playOnAwake = false;
        
        // ถ้าเป็น SFX ปกติ ให้เซ็ต Pitch เริ่มต้นตามสถานะปัจจุบัน
        if (isSFX && SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive)
        {
            source.pitch = slowMoPitch;
        }

        source.Play();

        if (isSFX)
        {
            sfxSources.Add(source); // เก็บลง List เพื่ออัปเดต Pitch เรียลไทม์
        }
        else
        {
            // ถ้าเป็นเสียง UI ให้ลบทำลายทิ้งหลังเล่นจบแบบไม่ต้องสนใจ Pitch
            Destroy(soundObj, clip.length + 0.1f);
        }
    }
}
