using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // สำคัญมาก! สำหรับใช้ TextMeshPro (ฟอนต์คมชัด)

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Elements")]
    public GameObject dialogueBoxPanel; // แผงพื้นหลังสีดำ
    public TextMeshProUGUI dialogueText; // กล่องตัวหนังสือ

    [Header("Settings")]
    public float typingSpeed = 0.05f; // ความเร็วในการพิมพ์

    [Header("Audio Settings")]
    public AudioClip openSound; // เสียงตอนกล่องข้อความเด้งขึ้นมา
    public AudioClip clickSound; // เสียงตอนกดข้าม/ไปประโยคถัดไป
    public AudioClip typeSound; // เสียงพิมพ์ดีดตัวอักษร
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    private Queue<string> sentences;
    private bool isTyping = false;
    private string currentSentence = "";
    
    // ตัวแปรกันกดเบิ้ลรัวๆ
    public float lastDialogueEndTime = -100f;

    // ปุมสำหรับกด Next (ใช้ปุ่ม E หรือ Spacebar หรือเมาส์คลิกซ้ายก็ได้)
    private InputAction nextAction;

    private AudioSource typeAudioSource; // เพิ่ม AudioSource ถาวร

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sentences = new Queue<string>();

        // สร้าง AudioSource ถาวรเพื่อเล่นเสียงพิมพ์ (จะได้ตัดเสียงเก่าทิ้งทันที)
        typeAudioSource = gameObject.AddComponent<AudioSource>();
        typeAudioSource.playOnAwake = false;

        // สร้างปุ่มกด Next 
        nextAction = new InputAction("NextDialogue", InputActionType.Button);
        nextAction.AddBinding("<Keyboard>/e");
        nextAction.AddBinding("<Keyboard>/space");
        nextAction.AddBinding("<Mouse>/leftButton");
        nextAction.Enable();

        // ซ่อนกล่องข้อความไว้ก่อนตอนเริ่มเกม
        if (dialogueBoxPanel != null)
        {
            dialogueBoxPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        nextAction.Disable();
    }

    private bool skipNextInput = false;

    private void Update()
    {
        // ถ้ากล่องข้อความเปิดอยู่ และผู้เล่นกด E
        if (dialogueBoxPanel.activeSelf && nextAction.WasPressedThisFrame())
        {
            if (skipNextInput)
            {
                skipNextInput = false;
                return;
            }

            PlaySound(clickSound); // เล่นเสียงตอนกดปุ่ม

            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้โชว์ข้อความทั้งหมดทันที (ข้ามแอนิเมชันพิมพ์)
                StopAllCoroutines();
                dialogueText.ForceMeshUpdate();
                dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
                isTyping = false;
                if (typeAudioSource != null) typeAudioSource.Stop(); // หยุดเสียงพิมพ์ทันทีเมื่อกดข้าม
            }
            else
            {
                // ถ้าพิมพ์เสร็จแล้ว ให้ไปประโยคถัดไป
                DisplayNextSentence();
            }
        }
    }

    // ฟังก์ชันสำหรับรับข้อความจากสิ่งของ (Interactable Object)
    public void StartDialogue(string[] lines)
    {
        skipNextInput = true;
        dialogueBoxPanel.SetActive(true);
        sentences.Clear();

        PlaySound(openSound); // เล่นเสียงเปิดกล่องข้อความ

        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        StopAllCoroutines();
        if (typeAudioSource != null) typeAudioSource.Stop(); // หยุดเสียงค้าง
        StartCoroutine(TypeSentence(currentSentence));
    }

    // ระบบพิมพ์ดีด (โผล่มาทีละตัวอักษร) — ใช้ TMP characterCount แทน string.Length
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        
        // ใส่ข้อความไปให้หมดก่อน เพื่อให้มันจัดบรรทัด (Word Wrap) ล่วงหน้า
        dialogueText.text = sentence;
        dialogueText.ForceMeshUpdate(); // บังคับให้ TMP คำนวณก่อน
        
        // นับจำนวนตัวอักษรที่ "มองเห็นจริง" (ไม่นับแท็ก <color>, <link> ฯลฯ)
        int totalVisibleChars = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0; // ซ่อนไว้ก่อน
        
        for (int i = 0; i <= totalVisibleChars; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            // ตรวจว่าตัวอักษรปัจจุบันเป็น space หรือไม่
            bool isSpace = false;
            if (i > 0 && i <= totalVisibleChars)
            {
                TMP_CharacterInfo charInfo = dialogueText.textInfo.characterInfo[i - 1];
                isSpace = charInfo.character == ' ';
            }

            // เล่นเสียงพิมพ์ดีด (ไม่เล่นตอน space)
            if (i % 2 == 0 && totalVisibleChars > 0 && i < totalVisibleChars && !isSpace)
            {
                PlaySoundWithPitch(typeSound, Random.Range(0.9f, 1.1f));
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        
        isTyping = false;
        if (typeAudioSource != null) typeAudioSource.Stop();
    }

    public void EndDialogue()
    {
        dialogueBoxPanel.SetActive(false);
        lastDialogueEndTime = Time.unscaledTime; // เก็บเวลาตอนคุยจบ
        if (typeAudioSource != null) typeAudioSource.Stop();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(clip);
        }
    }

    private void PlaySoundWithPitch(AudioClip clip, float pitch)
    {
        if (clip == null || typeAudioSource == null) return;
        
        // ใช้ AudioSource ถาวรเพื่อเล่นเสียงพิมพ์ (จะตัดเสียงเก่าทิ้งทันทีที่มีเสียงใหม่)
        typeAudioSource.clip = clip;
        typeAudioSource.volume = soundVolume;
        typeAudioSource.pitch = pitch;
        typeAudioSource.Play();
    }
}
