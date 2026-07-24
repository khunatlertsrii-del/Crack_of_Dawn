using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ระบบห้องปิดตาย (Arena Lockdown)
/// ควบคุมเลเซอร์กั้นทางเข้า-ออก และเรียกสปอนศัตรู
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaManager : MonoBehaviour
{
    [Header("Arena Setup")]
    [Tooltip("รายชื่อประตูเกิดศัตรูที่จะทำงานใน Arena นี้")]
    [SerializeField] private List<DoorSpawner> spawners;
    
    [Tooltip("เกมออบเจ็กต์ที่เป็นประตูกั้น (เช่น เลเซอร์ หรือกรงเหล็ก) จะถูกเปิดใช้งานตอนเริ่มต่อสู้")]
    [SerializeField] private List<GameObject> blockers;

    private bool arenaStarted = false;
    private bool arenaCleared = false;
    private BoxCollider2D triggerCol;
    
    private void Awake()
    {
        triggerCol = GetComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        
        // ปิดเลเซอร์กั้นทางไว้ก่อน
        foreach (var blocker in blockers)
        {
            if (blocker != null) blocker.SetActive(false);
        }
        
        // บังคับให้ Spawner ต้องใช้ Manual Trigger
        foreach(var spawner in spawners)
        {
            if(spawner != null) spawner.requireManualTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (arenaStarted || arenaCleared) return;

        if (collision.CompareTag("Player"))
        {
            StartArena();
        }
    }

    private void StartArena()
    {
        arenaStarted = true;
        Debug.Log("[Arena] Lockdown เริ่มต้น!");
        
        // เปิดเลเซอร์ขังผู้เล่น
        foreach (var blocker in blockers)
        {
            if (blocker != null) blocker.SetActive(true);
        }

        // สั่งเปิดประตูสปอนศัตรูทั้งหมด
        foreach (var spawner in spawners)
        {
            if (spawner != null) spawner.TriggerSpawner();
        }
    }

    private void Update()
    {
        if (arenaStarted && !arenaCleared)
        {
            CheckArenaClear();
        }
    }

    private void CheckArenaClear()
    {
        // 1. เช็คก่อนว่า Spawner ทั้งหมดสปอนศัตรูครบหรือยัง
        foreach (var spawner in spawners)
        {
            if (spawner != null && !spawner.IsSpawningComplete)
            {
                return; // ยังสปอนไม่เสร็จ ห้ามจบ Arena
            }
        }

        // 2. เช็คว่ามีศัตรูหลงเหลืออยู่ในกรอบ Arena ไหม
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(triggerCol.bounds.center, triggerCol.bounds.size, 0f);
        
        bool enemyFound = false;
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                enemyFound = true;
                break;
            }
        }
        
        // ถ้าไม่เจอศัตรูเลย แปลว่าเคลียร์แล้ว!
        if (!enemyFound)
        {
            arenaCleared = true;
            Debug.Log("[Arena] ปลดล็อกห้อง เคลียร์ศัตรูหมดแล้ว!");

            // ปิดเลเซอร์
            foreach (var blocker in blockers)
            {
                if (blocker != null) blocker.SetActive(false);
            }
            
            // สามารถเพิ่ม UI คำว่า CLEARED ได้ในอนาคต
        }
    }
}
