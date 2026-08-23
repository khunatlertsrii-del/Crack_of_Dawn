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

    private Queue<string> sentences;
    private bool isTyping = false;
    private string currentSentence = "";
    
    // ตัวแปรกันกดเบิ้ลรัวๆ
    public float lastDialogueEndTime = -100f;

    // ปุมสำหรับกด Next (ใช้ปุ่ม E หรือ Spacebar หรือเมาส์คลิกซ้ายก็ได้)
    private InputAction nextAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sentences = new Queue<string>();

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

    private void Update()
    {
        // ถ้ากล่องข้อความเปิดอยู่ และผู้เล่นกด E
        if (dialogueBoxPanel.activeSelf && nextAction.WasPressedThisFrame())
        {
            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้โชว์ข้อความทั้งหมดทันที (ข้ามแอนิเมชันพิมพ์)
                StopAllCoroutines();
                dialogueText.text = currentSentence;
                isTyping = false;
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
        dialogueBoxPanel.SetActive(true);
        sentences.Clear();

        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        // หยุดเวลาในเกมเวลาคุย (ให้ไข่เจียวหยุดเดิน)
        Time.timeScale = 0f; 

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
        StartCoroutine(TypeSentence(currentSentence));
    }

    // ระบบพิมพ์ดีด (โผล่มาทีละตัวอักษร)
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed); // ใช้ Realtime เพราะเราหยุดเวลา (TimeScale=0)
        }
        isTyping = false;
    }

    public void EndDialogue()
    {
        dialogueBoxPanel.SetActive(false);
        Time.timeScale = 1f; // ให้เวลาเดินต่อ
        lastDialogueEndTime = Time.unscaledTime; // เก็บเวลาตอนคุยจบ
    }
}
