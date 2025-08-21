using UnityEngine;
using TMPro;

public class SurvialCheckPoint : MonoBehaviour
{
    [Header("References")]
    public Transform[] checkpoints; // danh sách checkpoint
    public Transform startPoint;

    [Header("Settings")]
    public string carTag = "Player";
    public float startTime = 30f;   // thời gian ban đầu
    public float extraTime = 10f;   // cộng thêm mỗi khi qua checkpoint

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI checkpointText;

    private float timer;
    private int currentCheckpoint = 0;
    private bool isActive = false;
    private bool missionCompleted = false;

    private void Start()
    {
        UpdateUI();
    }

    public void StartMission()
    {
        isActive = true;
        missionCompleted = false;
        timer = startTime;
        currentCheckpoint = 0;

        if (checkpoints.Length > 0)
            WaypointManager.Instance?.CreatePointer(checkpoints[0].position, null);

        Debug.Log("🚀 SurvivalRaceMission started!");
    }

    private void Update()
    {
        if (!isActive || missionCompleted) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            FailMission();
            return;
        }
        UpdateUI();
    }

    // Hàm này sẽ được gọi bởi Checkpoint.cs khi Player đi qua
    public void PassCheckpoint(int index)
    {
        if (!isActive || missionCompleted) return;

        if (index == currentCheckpoint)
        {
            Debug.Log($"✅ Qua checkpoint {index + 1}");

            // Reset lại timer thay vì cộng thêm
            timer = startTime;

            currentCheckpoint++;

            if (currentCheckpoint >= checkpoints.Length)
            {
                CompleteMission();
            }
            else
            {
                WaypointManager.Instance?.CreatePointer(checkpoints[currentCheckpoint].position, null);
            }
            UpdateUI();
        }
    }


    private void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timer:F1}s"; // không dùng icon đặc biệt để tránh lỗi font
            timerText.gameObject.SetActive(isActive);
        }
        if (checkpointText != null)
        {
            checkpointText.text = $"Checkpoint: {currentCheckpoint}/{checkpoints.Length}";
            checkpointText.gameObject.SetActive(isActive);
        }
    }

    private void CompleteMission()
    {
        isActive = false;
        missionCompleted = true;
        WaypointManager.Instance?.RemoveWaypoint();
        Debug.Log("✅ Survival Race Completed!");
        QuestManager.instance?.CompleteQuest();
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (checkpointText != null) checkpointText.gameObject.SetActive(false);
    }

    private void FailMission()
    {
        isActive = false;
        missionCompleted = true;
        WaypointManager.Instance?.RemoveWaypoint();
        Debug.Log("❌ Survival Race Failed!");
        QuestManager.instance?.FailQuest("Hết giờ");
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (checkpointText != null) checkpointText.gameObject.SetActive(false);
    }
}
