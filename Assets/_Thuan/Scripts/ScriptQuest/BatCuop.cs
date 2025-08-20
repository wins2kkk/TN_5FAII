using UnityEngine;
using TMPro;

public class BatCuop : MonoBehaviour
{
    [Header("NPC Settings")]
    public GameObject[] npcCarPrefabs; // Danh sách prefab xe
    public Transform[] spawnPoints;    // Điểm spawn NPC
    public Transform[] waypoints;      // Waypoint NPC chạy

    [Header("Mission Settings")]
    public float chaseStayTime = 3f;   // Thời gian đứng gần để bắt
    public float detectDistance = 5f;  // Khoảng cách bắt đầu đếm thời gian bắt
    public float loseDistance = 100f;   // Khoảng cách tối đa để không thua
    public float maxMissionTime = 60f; // Thời gian tối đa của nhiệm vụ

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private Transform playerCar;
    private GameObject currentNPC;
    private bool isActive = false;
    private bool missionCompleted = false;
    private float timer = 0f;
    private float missionTimer = 0f;

    private void Start()
    {
        playerCar = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void StartMission()
    {
        if (npcCarPrefabs.Length == 0 || spawnPoints.Length == 0 || waypoints.Length == 0) return;

        // 🔍 Tìm spawn point gần player nhất
        Transform nearestSpawn = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Transform sp in spawnPoints)
        {
            float dist = Vector3.Distance(playerCar.position, sp.position);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestSpawn = sp;
            }
        }

        if (nearestSpawn == null) return;

        // Spawn NPC ở điểm gần player nhất
        GameObject carPrefab = npcCarPrefabs[Random.Range(0, npcCarPrefabs.Length)];
        currentNPC = Instantiate(carPrefab, nearestSpawn.position, nearestSpawn.rotation);

        // Gán waypoint cho NPC
        NPCWaypointCar npcCar = currentNPC.GetComponent<NPCWaypointCar>();
        if (npcCar != null)
        {
            npcCar.waypoints = waypoints;
        }

        isActive = true;
        missionCompleted = false;
        timer = 0f;
        missionTimer = 0f;

        Debug.Log("🚔 Nhiệm vụ bắt trộm bắt đầu!");
    }


    private void Update()
    {
        if (!isActive || missionCompleted || currentNPC == null) return;

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
                timerText.text = "";
                timerText.gameObject.SetActive(false);
            }
        }
    }

    private void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;
        if (timerText != null) timerText.text = "";
        Debug.Log("✅ Bắt trộm thành công!");
        QuestManager.instance?.CompleteQuest();
        if (currentNPC != null) Destroy(currentNPC);
    }

    private void FailMission()
    {
        missionCompleted = false;
        isActive = false;
        if (timerText != null) timerText.text = "";
        Debug.Log("❌ Thua nhiệm vụ!");
        // ❗ Không xóa NPC ngay để tránh biến mất giữa chừng
        // Nếu muốn xóa khi thua thì bỏ comment dòng dưới
         if (currentNPC != null) Destroy(currentNPC);
    }
}
