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

    [Header("Win Panel")]
    public GameObject winPanel;
    public TextMeshProUGUI winCoinText;

    [Header("Lose Panel")]
    public GameObject losePanel;

    [Header("Panel Animation Settings")]
    public float animationDuration = 0.8f;
    public AnimationCurve slideDownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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
        InitializePanels();
    }

    void InitializePanels()
    {
        // Ẩn tất cả panels
        if (winPanel != null)
            winPanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);
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

        if (CheckpointPool.Instance != null)
        {
            CheckpointPool.Instance.HideAllCheckpoints();
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
            var checkpoint = CheckpointPool.Instance.checkpoints.Find(c => c.name == checkpointName);
            if (checkpoint != null)
                lastCheckpointTransform = checkpoint.transform;
        }

        currentCheckpointIndex++;

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

        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
        {
            QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(500);
        }

        ShowWinPanel();
        StartCoroutine(AutoReturnToMenu());
    }

    void FailMission()
    {
        missionActive = false;
        Debug.Log("❌ Thất bại - Hết thời gian!");

        if (QuestManager.instance != null && QuestManager.instance.timerText != null)
        {
            QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        ShowLosePanel();
        StartCoroutine(AutoReturnToMenu());
    }

    void ShowWinPanel()
    {
        if (winPanel != null)
        {
            // Cập nhật text hiển thị coins
            if (winCoinText != null)
            {
                winCoinText.text = "500";
            }

            StartCoroutine(AnimatePanel(winPanel, true));
        }
    }

    void ShowLosePanel()
    {
        if (losePanel != null)
        {
            StartCoroutine(AnimatePanel(losePanel, true));
        }
    }

    IEnumerator AnimatePanel(GameObject panel, bool showPanel)
    {
        if (panel == null) yield break;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        if (showPanel)
        {
            // Hiện panel
            panel.SetActive(true);

            // Vị trí ban đầu (ở trên màn hình)
            Vector2 startPos = new Vector2(panelRect.anchoredPosition.x, Screen.height);
            Vector2 targetPos = Vector2.zero; // Vị trí giữa màn hình

            panelRect.anchoredPosition = startPos;

            // Animation slide down
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = slideDownCurve.Evaluate(t);

                panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveValue);

                // Thêm hiệu ứng alpha
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = curveValue;
                }

                yield return null;
            }

            // Đảm bảo vị trí cuối cùng chính xác
            panelRect.anchoredPosition = targetPos;

            if (panel.GetComponent<CanvasGroup>() != null)
            {
                panel.GetComponent<CanvasGroup>().alpha = 1f;
            }
        }
        else
        {
            // Ẩn panel (slide up)
            Vector2 startPos = panelRect.anchoredPosition;
            Vector2 targetPos = new Vector2(startPos.x, Screen.height);

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = slideDownCurve.Evaluate(t);

                panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveValue);

                // Hiệu ứng alpha khi ẩn
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
                }

                yield return null;
            }

            panel.SetActive(false);
        }
    }

    IEnumerator AutoReturnToMenu()
    {
        yield return new WaitForSeconds(5f);

        // Ẩn panel trước khi chuyển scene
        if (winPanel != null && winPanel.activeInHierarchy)
        {
            yield return StartCoroutine(AnimatePanel(winPanel, false));
        }

        if (losePanel != null && losePanel.activeInHierarchy)
        {
            yield return StartCoroutine(AnimatePanel(losePanel, false));
        }

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

    public void SkipResult()
    {
        StopAllCoroutines();

        // Ẩn panels nhanh
        if (winPanel != null && winPanel.activeInHierarchy)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null && losePanel.activeInHierarchy)
        {
            losePanel.SetActive(false);
        }

        PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
        SceneManager.LoadScene("Loading");
    }
}