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

    [Header("Result Display")]
    public TextMeshProUGUI resultText; // Text hiển thị kết quả win/lose
    public GameObject resultPanel; // Panel chứa kết quả

    [Header("Checkpoint Order")]
    public List<string> checkpointOrder = new List<string> { "Check1", "Check2", "Check3" };

    [Header("Objects")]
    public GameObject startBarrier;

    private int currentLap = 0;
    private int currentCheckpointIndex = 0;
    private float timer = 0f;
    private bool missionActive = false;

    private Transform player;
    private Transform lastCheckpointTransform;

    void Start()
    {
        // 🔍 Tìm Player tự động qua tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("🚫 Không tìm thấy Player có tag 'Player'");
        }

        if (PlayerPrefs.GetInt("LapMission_Active", 0) == 1)
        {
            LoadMissionSettings();
            StartCoroutine(StartMissionWithCountdown());
        }

        UpdateLapDisplay();

        // Ẩn result panel ban đầu
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void LoadMissionSettings()
    {
        requiredLaps = PlayerPrefs.GetInt("LapMission_Laps", 3);
        timeLimit = PlayerPrefs.GetFloat("LapMission_Time", 90f);
        PlayerPrefs.SetInt("LapMission_Active", 0);
    }

    IEnumerator StartMissionWithCountdown()
    {
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
            startBarrier.SetActive(false);

        StartMission();
    }

    public void StartMission()
    {
        currentLap = 0;
        currentCheckpointIndex = 0;
        timer = 0f;
        missionActive = true;

        // 🔄 Thay đổi: Chỉ hiện checkpoint đầu tiên thay vì tất cả
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
        if (!missionActive || player == null) return;

        timer += Time.deltaTime;

        // Chỉ dùng QuestManager để hiển thị thời gian
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

        // ⚠️ Kiểm tra rơi khỏi map
        if (player.position.y < -100f)
        {
            Debug.Log("⚠️ Người chơi rơi khỏi map");
            ReturnToLastCheckpoint();
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
        {
            CheckpointPool.Instance.HideCheckpoint(checkpointName);

            // 🔁 Ghi nhớ checkpoint cuối cùng
            var checkpoint = CheckpointPool.Instance.checkpoints.Find(c => c.name == checkpointName);
            if (checkpoint != null)
                lastCheckpointTransform = checkpoint.transform;
        }

        currentCheckpointIndex++;

        // 🔄 Thay đổi: Hiện checkpoint tiếp theo nếu chưa hết lap
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

        // 🔄 Thay đổi: Chỉ hiện checkpoint đầu tiên của lap mới
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
        missionActive = false;
        Debug.Log("🎉 Đua hoàn thành!");

        // Hiển thị kết quả trực tiếp

        ShowResult("Nhiệm vụ hoàn thành ");
        // Ẩn timer của QuestManager
        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
        {
            QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(500); // Thưởng 50 coin khi thắng
        }
        StartCoroutine(ReturnToMenu());
    }

    void FailMission()
    {
        missionActive = false;
        Debug.Log("❌ Thất bại - Hết thời gian!");

        // Hiển thị kết quả trực tiếp
        ShowResult("Hết thời gian nhiệm vụ thất bại !");

        // Ẩn timer của QuestManager
        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
        {
            QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        StartCoroutine(ReturnToMenu());
    }

    void ShowResult(string message)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = message;

            resultText.gameObject.SetActive(true);
        }


    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(5f); // Hiển thị kết quả 5 giây
        PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
        SceneManager.LoadScene("Loading");
    }

    public void ReturnToLastCheckpoint()
    {
        if (lastCheckpointTransform != null && player != null)
        {
            player.position = lastCheckpointTransform.position + Vector3.up * 2f;
            player.rotation = lastCheckpointTransform.rotation;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("🔁 Quay lại checkpoint gần nhất");
        }
    }

    // Method để skip kết quả (nếu muốn)
    public void SkipResult()
    {
        StopAllCoroutines();
        PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
        SceneManager.LoadScene("Loading");
    }
}