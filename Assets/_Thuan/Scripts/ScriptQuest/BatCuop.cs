using UnityEngine;
using TMPro;
using System.Collections;

public class BatCuop : MonoBehaviour
{
    [Header("Player")]
    public Transform playerCar; // Kéo thả Car 3 vào đây

    [Header("NPC Settings")]
    public GameObject[] npcCarPrefabs;
    public Transform[] spawnPoints;
    public Transform[] waypoints;

    [Header("Mission Settings")]
    public float chaseStayTime = 3f;
    public float detectDistance = 5f;
    public float loseDistance = 100f;
    public float maxMissionTime = 60f;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    //private Transform playerCar;
    private GameObject currentNPC;
    private bool isActive = false;
    private bool missionCompleted = false;
    private float timer = 0f;
    private float missionTimer = 0f;

    private void Start()
    {
        StartCoroutine(FindPlayerCoroutine());
    }

    private System.Collections.IEnumerator FindPlayerCoroutine()
    {
        // Chờ 1 frame để tất cả objects được khởi tạo
        yield return null;

        while (playerCar == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerCar = playerObj.transform;
                Debug.Log("✅ Tìm thấy Player: " + playerObj.name);
                break;
            }
            else
            {
                Debug.Log("🔍 Đang tìm Player...");
                yield return new WaitForSeconds(0.1f); // Chờ 0.1s rồi tìm lại
            }
        }
    }

    public void StartMission()
    {
        Debug.Log("🚔 Bắt đầu nhiệm vụ bắt cướp");

        // Đảm bảo có player
        if (playerCar == null)
        {
            Debug.LogError("❌ Chưa tìm thấy Player! Đợi một chút...");
            return;
        }

        // Kiểm tra arrays
        if (npcCarPrefabs == null || npcCarPrefabs.Length == 0)
        {
            Debug.LogError("❌ Không có NPC prefab!");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("❌ Không có spawn point!");
            return;
        }
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("❌ Không có waypoint!");
            return;
        }

        // Tìm spawn point gần nhất
        Transform nearestSpawn = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            float dist = Vector3.Distance(playerCar.position, sp.position);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestSpawn = sp;
            }
        }

        if (nearestSpawn == null)
        {
            Debug.LogError("❌ Không tìm thấy spawn point hợp lệ!");
            return;
        }

        // Spawn NPC
        GameObject carPrefab = npcCarPrefabs[Random.Range(0, npcCarPrefabs.Length)];
        if (carPrefab == null)
        {
            Debug.LogError("❌ Car prefab null!");
            return;
        }

        if (currentNPC != null) Destroy(currentNPC);

        currentNPC = Instantiate(carPrefab, nearestSpawn.position, nearestSpawn.rotation);

        // Gán waypoint
        NPCTrom npcCar = currentNPC.GetComponent<NPCTrom>();
        if (npcCar != null)
        {
            npcCar.waypoints = waypoints;
        }
        else
        {
            Debug.LogError("❌ NPC không có script NPCTrom!");
        }

        // Reset
        timer = 0f;
        missionTimer = 0f;
        isActive = true;
        missionCompleted = false;

        Debug.Log("✅ Spawn NPC thành công tại: " + nearestSpawn.name);
    }

    private void Update()
    {
        if (!isActive || missionCompleted || currentNPC == null || playerCar == null) return;

        missionTimer += Time.deltaTime;
        if (missionTimer >= maxMissionTime)
        {
            FailMission();
            return;
        }

        float distance = Vector3.Distance(playerCar.position, currentNPC.transform.position);
        if (distance > loseDistance)
        {
            FailMission();
            return;
        }

        if (distance <= detectDistance)
        {
            timer += Time.deltaTime;
            float timeLeft = Mathf.Max(0, chaseStayTime - timer);
            if (timerText != null)
            {
                timerText.text = $"Bắt trong: {timeLeft:F1}s";
                timerText.gameObject.SetActive(true);
            }

            if (timer >= chaseStayTime)
            {
                CompleteMission();
            }
        }
        else
        {
            timer = 0f;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }

    public void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;
        if (timerText != null) timerText.gameObject.SetActive(false);
        Debug.Log("✅ Bắt trộm thành công!");
        QuestManager.instance?.CompleteQuest();
        if (currentNPC != null) Destroy(currentNPC);
    }

    public void FailMission()
    {
        missionCompleted = false;
        isActive = false;
        if (timerText != null) timerText.gameObject.SetActive(false);
        Debug.Log("❌ Thua nhiệm vụ!");
        if (currentNPC != null) Destroy(currentNPC);
    }
}