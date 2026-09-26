using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบ Object Pool แบบครอบจักรวาล (ใช้งานง่าย)
/// ช่วยลดความแลคจากการ Instantiate และ Destroy กระสุนหรือเอฟเฟกต์บ่อยๆ
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // เก็บ Pool แยกตามชนิดของ Prefab (ใช้ GameObject เป็น Key)
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ขอเบิก Object จากกะบะ (ถ้าหมดจะสร้างใหม่)
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        // ถ้ายังไม่มีกะบะสำหรับ Prefab นี้ ให้สร้างใหม่
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        GameObject objToSpawn = null;
        Queue<GameObject> queue = poolDictionary[prefab];

        // ค้นหาของที่ว่างในกะบะ
        while (queue.Count > 0)
        {
            objToSpawn = queue.Dequeue();
            // เช็คกันเหนียวเผื่อมันโดนลบไปโดยไม่ตั้งใจ
            if (objToSpawn != null)
            {
                break;
            }
        }

        // ถ้ากะบะหมด (ไม่มีของว่าง) → จำเป็นต้องเสกใหม่
        if (objToSpawn == null)
        {
            objToSpawn = Instantiate(prefab);
            // เพิ่ม Component พิเศษเพื่อให้มันรู้ว่ามันมาจาก Prefab ไหน เวลาคืนจะได้คืนถูกที่
            PooledObject po = objToSpawn.AddComponent<PooledObject>();
            po.originalPrefab = prefab;
        }

        // รีเซ็ตค่าและเตรียมใช้งาน
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        return objToSpawn;
    }

    /// <summary>
    /// คืนของลงกะบะ (แทนการ Destroy)
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        PooledObject po = obj.GetComponent<PooledObject>();
        if (po != null && po.originalPrefab != null)
        {
            obj.SetActive(false); // ซ่อนไว้เฉยๆ ไม่ลบทิ้ง
            
            if (poolDictionary.ContainsKey(po.originalPrefab))
            {
                poolDictionary[po.originalPrefab].Enqueue(obj);
            }
            else
            {
                Destroy(obj); // ถ้าไม่มีกะบะให้เก็บ (แปลกๆ) ก็ลบทิ้งซะ
            }
        }
        else
        {
            Destroy(obj); // ถ้าไม่ใช่ของจากระบบ Pool ให้ลบทิ้งไปเลย
        }
    }
}

/// <summary>
/// แปะป้ายบอกว่า Object นี้มาจาก Prefab ต้นฉบับอันไหน
/// </summary>
public class PooledObject : MonoBehaviour
{
    public GameObject originalPrefab;
}
