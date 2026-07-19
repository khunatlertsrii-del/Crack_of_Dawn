using UnityEngine;

public class ComicTextSpawner : MonoBehaviour
{
    public static ComicTextSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnText(Vector2 position, string text, Color color, float size = 1f)
    {
        GameObject textObj = new GameObject("ComicText");
        // สุ่มให้มันกระจายออกจากจุดตรงกลางนิดหน่อย
        textObj.transform.position = position + Random.insideUnitCircle * 0.5f;
        
        // สุ่มเอียงเล็กน้อยสไตล์คอมมิค
        textObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        
        // เพิ่ม Sorting Group เพื่อให้อยู่เหนือ Sprite อื่นๆ
        var sg = textObj.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sg.sortingOrder = 500;

        ComicText comicText = textObj.AddComponent<ComicText>();
        comicText.Initialize(text, color, size);
    }

    public void SpawnRandomHitText(Vector2 position, bool isCritical)
    {
        string[] normalWords = { "BAM!", "POW!", "WHACK!" };
        string[] critWords = { "SPLAT!", "CRACK!", "BOOM!" };
        
        string word = isCritical ? critWords[Random.Range(0, critWords.Length)] : normalWords[Random.Range(0, normalWords.Length)];
        Color color = isCritical ? Color.red : Color.white;
        float size = isCritical ? 2.0f : 1.2f;

        SpawnText(position, word, color, size);
    }

    public void SpawnParryText(Vector2 position)
    {
        SpawnText(position, "CLANG!", Color.yellow, 1.8f);
    }
}
