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

            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้โชว์ข้อความทั้งหมดทันที (ข้ามแอนิเมชันพิมพ์)
                StopAllCoroutines();
                dialogueText.maxVisibleCharacters = currentSentence.Length;
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
        skipNextInput = true;
        dialogueBoxPanel.SetActive(true);
        sentences.Clear();

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
        StartCoroutine(TypeSentence(currentSentence));
    }

    // ระบบพิมพ์ดีด (โผล่มาทีละตัวอักษร)
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        
        // ใส่ข้อความไปให้หมดก่อน เพื่อให้มันจัดบรรทัด (Word Wrap) ล่วงหน้า จะได้ไม่กระโดด
        dialogueText.text = sentence;
        dialogueText.maxVisibleCharacters = 0; // ซ่อนไว้ก่อน
        
        // ใช้ regular for loop
        for (int i = 0; i <= sentence.Length; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typingSpeed); // ใช้ Realtime เพราะเราหยุดเวลา (TimeScale=0)
        }
        
        isTyping = false;
    }

    public void EndDialogue()
    {
        dialogueBoxPanel.SetActive(false);
        lastDialogueEndTime = Time.unscaledTime; // เก็บเวลาตอนคุยจบ
    }
}
