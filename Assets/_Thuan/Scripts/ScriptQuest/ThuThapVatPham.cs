using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ThuThapVatPham : MonoBehaviour
{
    [Header("Cài đặt nhiệm vụ thu thập")]
    public string playerTag = "Player";
    public GameObject collectablePrefab;
    public int numberOfItems = 5;
    public TextMeshProUGUI collectText; // 👈 Text hiển thị số lượng vật phẩm thu thập

    //  public int reward = 100;
    public Transform[] itemSpawnPoints;

    private Transform player;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private int itemsCollected = 0;
    private bool questActive = false;

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;
        else
        {

        }
           // Debug.LogError("Không tìm thấy Player với tag: " + playerTag//);
    }

    public void StartQuest()
    {
        FindPlayer();
        if (player == null || collectablePrefab == null || itemSpawnPoints.Length == 0)
        {
            Debug.LogError("Không thể bắt đầu nhiệm vụ: thiếu thông tin.");
            return;
        }

        ClearItems();
        itemsCollected = 0;
        if (collectText != null)
        {
            collectText.text = $"Đã thu thập: {itemsCollected}/{numberOfItems}";
            collectText.gameObject.SetActive(true); // 👈 Hiện text khi bắt đầu
        }

        questActive = true;

        for (int i = 0; i < numberOfItems; i++)
        {
            Transform spawnPoint = itemSpawnPoints[Random.Range(0, itemSpawnPoints.Length)];
            GameObject item = Instantiate(collectablePrefab, spawnPoint.position, Quaternion.identity);
            spawnedItems.Add(item);
        }

        Debug.Log("🧺 Bắt đầu nhiệm vụ thu thập!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!questActive) return;

        if (spawnedItems.Contains(other.gameObject))
        {
            Destroy(other.gameObject);
            spawnedItems.Remove(other.gameObject);
            itemsCollected++;

            Debug.Log($"✅ Đã thu thập {itemsCollected}/{numberOfItems}");
            if (collectText != null)
                collectText.text = $"Đã thu thập: {itemsCollected}/{numberOfItems}";

            if (itemsCollected == numberOfItems)
            {
                CompleteQuest();
            }
        }
    }
    public void CollectItem(GameObject item)
    {
        if (!questActive || !spawnedItems.Contains(item)) return;

        spawnedItems.Remove(item);
        Destroy(item);
        itemsCollected++;

        Debug.Log($"✅ Đã thu thập {itemsCollected}/{numberOfItems}");
        if (collectText != null)
            collectText.text = $"Đã thu thập: {itemsCollected}/{numberOfItems}";

        if (itemsCollected >= numberOfItems)
        {
            CompleteQuest();
        }
    }

    void CompleteQuest()
    {
        questActive = false;
        ClearItems();
        //Debug.Log($"🎉 Nhiệm vụ hoàn thành! Nhận {reward} xu.");

        // Gọi hệ thống quản lý nhiệm vụ nếu có
        QuestManager.instance?.CompleteQuest();
        if (collectText != null)
        {
            collectText.text = "";
            collectText.gameObject.SetActive(false); // 👈 Ẩn text khi xong
        }

    }

    void ClearItems()
    {
        foreach (var item in spawnedItems)
            if (item) Destroy(item);
        spawnedItems.Clear();
    }
}
