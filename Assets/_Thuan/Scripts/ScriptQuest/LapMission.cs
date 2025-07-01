using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LapMission : MonoBehaviour
{
    [Header("Mission Settings")]
    public int requiredLaps = 3;
    public float timeLimit = 90f;

    [Header("UI Elements")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI countdownText;
    public GameObject countdownPanel;

    [Header("Checkpoint Order")]
    public List<string> checkpointOrder = new List<string> { "Check1", "Check2", "Check3" };

    [Header("Objects")]
    public GameObject startBarrier; // 👈 Gắn Cube cần ẩn sau countdown

    private int currentLap = 0;
    private int currentCheckpointIndex = 0;
    private float timer = 0f;
    private bool missionActive = false;

    void Start()
    {
        if (PlayerPrefs.GetInt("LapMission_Active", 0) == 1)
        {
            LoadMissionSettings();
            StartCoroutine(StartMissionWithCountdown());
        }

        UpdateLapDisplay();
    }

    void LoadMissionSettings()
    {
        requiredLaps = PlayerPrefs.GetInt("LapMission_Laps", 3);
        timeLimit = PlayerPrefs.GetFloat("LapMission_Time", 90f);
        PlayerPrefs.SetInt("LapMission_Active", 0);
    }

    IEnumerator StartMissionWithCountdown()
    {
        // Cho xe chạy bình thường, chỉ đếm rồi ẩn cube
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 100f;
            }

            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.fontSize = 120f;
        }

        yield return new WaitForSeconds(0.5f);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (startBarrier != null)
            startBarrier.SetActive(false); // Ẩn Cube sau countdown

        StartMission();
    }

    public void StartMission()
    {
        currentLap = 0;
        currentCheckpointIndex = 0;
        timer = 0f;
        missionActive = true;

        if (CheckpointPool.Instance != null)
            CheckpointPool.Instance.ShowAllCheckpoints();

        UpdateLapDisplay();
        Debug.Log("🏁 Bắt đầu nhiệm vụ đua!");
    }

    void Update()
    {
        if (!missionActive) return;

        timer += Time.deltaTime;

        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
        {
            float timeLeft = Mathf.Max(0f, timeLimit - timer);
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            QuestManager.instance.timerText.text = $"Thời gian còn lại: {minutes:00}:{seconds:00}";
            QuestManager.instance.timerText.gameObject.SetActive(true);
        }

        if (timer > timeLimit)
        {
            FailMission();
        }
    }

    public void OnCheckpointHit(string checkpointName)
    {
        if (!missionActive) return;

        string expected = checkpointOrder[currentCheckpointIndex];
        if (checkpointName != expected)
        {
            Debug.Log($"❌ Sai checkpoint. Cần: {expected}, Nhận: {checkpointName}");
            return;
        }

        Debug.Log($"✅ Checkpoint đúng: {checkpointName}");

        if (CheckpointPool.Instance != null)
            CheckpointPool.Instance.HideCheckpoint(checkpointName);

        currentCheckpointIndex++;

        if (currentCheckpointIndex >= checkpointOrder.Count)
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
        if (CheckpointPool.Instance != null)
            CheckpointPool.Instance.ShowAllCheckpoints();

        Debug.Log($"🔄 Chuẩn bị lap {currentLap + 1}");
    }

    void UpdateLapDisplay()
    {
        if (lapText != null)
            lapText.text = $"Lap: {currentLap}/{requiredLaps}";
    }

    void CompleteMission()
    {
        missionActive = false;

        Debug.Log("🎉 Đua hoàn thành!");

        if (QuestManager.instance != null)
        {
            QuestManager.instance.CompleteQuest();
            if (QuestManager.instance.timerText != null)
                QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        StartCoroutine(ReturnToMenu());
    }

    void FailMission()
    {
        missionActive = false;

        Debug.Log("❌ Thất bại - Hết thời gian!");

        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
            QuestManager.instance.timerText.gameObject.SetActive(false);

        StartCoroutine(ReturnToMenu());
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Thanh_Pho2");
    }
}
