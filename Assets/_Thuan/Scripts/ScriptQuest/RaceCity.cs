using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RaceCity : MonoBehaviour
{
    [Header("Mission Settings")]
    public int requiredLaps = 3;
    public float timeLimit = 90f;
    public string carTag = "Player";

    [Header("UI Elements")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;

    [Header("Checkpoint Order")]
    public List<string> checkpointOrder = new List<string> { "Check1", "Check2", "Check3" };

    private int currentLap = 0;
    private int currentCheckpointIndex = 0;
    private float timer = 0f;
    private bool isActive = false;
    private bool missionCompleted = false;

    private Transform carTransform;
    private Transform lastCheckpointTransform;

    private void Start()
    {
        FindActiveCar();
        UpdateLapDisplay();
    }

    private void FindActiveCar()
    {
        // Tìm xe active có tag "Player"
        GameObject carObject = GameObject.FindGameObjectWithTag(carTag);
        if (carObject != null)
        {
            carTransform = carObject.transform;
            Debug.Log($"Found active car: {carTransform.name}");
        }
        else
        {
            Debug.LogError("No active car found with tag: " + carTag);
        }
    }

    public void StartMission()
    {
        // Tìm lại xe trước khi bắt đầu mission (đề phòng xe đã thay đổi)
        FindActiveCar();

        if (carTransform == null)
        {
            Debug.LogError("Cannot start mission: No car found!");
            return;
        }

        currentLap = 0;
        currentCheckpointIndex = 0;
        timer = 0f;
        isActive = true;
        missionCompleted = false;

        // 🔄 Chỉ hiện checkpoint đầu tiên thay vì tất cả
        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideAllCheckpoints();
            // Hiện checkpoint đầu tiên
            if (checkpointOrder.Count > 0)
            {
                CheckpointPool.Instance.ShowCheckpoint(checkpointOrder[0]);
            }
        }

        UpdateLapDisplay();
        Debug.Log("🏁 Bắt đầu nhiệm vụ đua!");
    }

    void Update()
    {
        if (!isActive || missionCompleted || carTransform == null) return;

        timer += Time.deltaTime;

        // Hiển thị thời gian
        if (timerText != null)
        {
            float timeLeft = Mathf.Max(0f, timeLimit - timer);
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            timerText.text = $"Thời gian còn lại: {minutes:00}:{seconds:00}";
            timerText.gameObject.SetActive(true);
        }

        if (timer > timeLimit)
        {
            FailMission();
        }

        // ⚠️ Kiểm tra rơi khỏi map
        if (carTransform.position.y < -100f)
        {
            Debug.Log("⚠️ Người chơi rơi khỏi map");
            ReturnToLastCheckpoint();
        }
    }

    public void OnCheckpointHit(string checkpointName)
    {
        if (!isActive || missionCompleted) return;

        string expected = checkpointOrder[currentCheckpointIndex];
        if (checkpointName != expected)
        {
            Debug.Log($"❌ Sai checkpoint. Cần: {expected}, Nhận: {checkpointName}");
            return;
        }

        Debug.Log($"✅ Checkpoint đúng: {checkpointName}");

        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideCheckpoint(checkpointName);

            // 🔁 Ghi nhớ checkpoint cuối cùng
            var checkpoint = CheckpointPool.Instance.checkpoints.Find(c => c.name == checkpointName);
            if (checkpoint != null)
                lastCheckpointTransform = checkpoint.transform;
        }

        currentCheckpointIndex++;

        // 🔄 Hiện checkpoint tiếp theo nếu chưa hết lap
        if (currentCheckpointIndex < checkpointOrder.Count)
        {
            if (CheckpointPool.Instance != null)
            {
                CheckpointPool.Instance.ShowCheckpoint(checkpointOrder[currentCheckpointIndex]);
            }
        }
        else
        {
            CompleteLap();
        }
    }

    void CompleteLap()
    {
        currentLap++;
        currentCheckpointIndex = 0;
        UpdateLapDisplay();

        Debug.Log($"🏁 Lap {currentLap}/{requiredLaps} hoàn thành");

        if (currentLap >= requiredLaps)
            CompleteMission();
        else
            StartCoroutine(PrepareNextLap());
    }

    IEnumerator PrepareNextLap()
    {
        yield return new WaitForSeconds(0.5f);

        // 🔄 Chỉ hiện checkpoint đầu tiên của lap mới
        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideAllCheckpoints();
            if (checkpointOrder.Count > 0)
            {
                CheckpointPool.Instance.ShowCheckpoint(checkpointOrder[0]);
            }
        }

        Debug.Log($"🔄 Chuẩn bị lap {currentLap + 1}");
    }

    void UpdateLapDisplay()
    {
        if (lapText != null)
            lapText.text = $"Lap: {currentLap}/{requiredLaps}";
    }

    void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;
        Debug.Log("🎉 Đua hoàn thành!");

        // Ẩn timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        // Ẩn tất cả checkpoint
        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideAllCheckpoints();
        }

        // Thưởng coin
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(500);
        }

        // Hoàn thành quest thay vì chuyển scene
        QuestManager.instance?.CompleteQuest();
    }

    void FailMission()
    {
        missionCompleted = true;
        isActive = false;
        Debug.Log("❌ Thất bại - Hết thời gian!");

        // Ẩn timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        // Ẩn tất cả checkpoint
        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideAllCheckpoints();
        }

        // Có thể thêm logic fail quest nếu QuestManager hỗ trợ
        Debug.Log("Mission failed!");
    }

    public void ReturnToLastCheckpoint()
    {
        if (lastCheckpointTransform != null && carTransform != null)
        {
            carTransform.position = lastCheckpointTransform.position + Vector3.up * 2f;
            carTransform.rotation = lastCheckpointTransform.rotation;

            Rigidbody rb = carTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("🔁 Quay lại checkpoint gần nhất");
        }
    }
}