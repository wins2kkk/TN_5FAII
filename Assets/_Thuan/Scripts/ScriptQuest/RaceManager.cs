using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Cài đặt đua xe")]
    public int totalLaps = 3;
    public float maxRaceTime = 180f;
    public float countdownTime = 3f;

    [Header("Player & AI")]
    public Transform playerStartPoint;
    public List<GameObject> aiPrefabs;
    public List<Transform> aiSpawnPoints;

    [Header("UI - Sẽ được tự động tìm")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI countdownText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Result Text - Sẽ được tự động tìm")]
    public TextMeshProUGUI winResultText;
    public TextMeshProUGUI loseResultText;

    [Header("Track Waypoints")]
    public Transform waypointParent;

    private List<GameObject> spawnedAIs = new List<GameObject>();
    private GameObject player;
    private List<GameObject> allRacers = new List<GameObject>();
    private Transform[] trackWaypoints;

    private float raceTimeLeft;
    private bool raceStarted = false;
    private bool raceCompleted = false;
    private int currentCoinReward = 0; // Lưu số coin reward

    private List<string> finishOrder = new List<string>();
    private Dictionary<string, int> racerLapCounts = new Dictionary<string, int>();

    public bool RaceOver => raceCompleted;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // ✅ Delay để đảm bảo scene đã load hoàn toàn
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Đợi 1 frame để scene setup hoàn toàn
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        // Tự động tìm UI components
        FindUIComponents();

        // Setup cơ bản
        InitializeUI();
        FindTrackWaypoints();

        // Kiểm tra nếu có nhiệm vụ đua được lưu từ scene trước
        if (PlayerPrefs.GetInt("LapMission_Active", 0) == 1)
        {
            QuestData quest = new QuestData
            {
                questName = "Lap Race",
                lapCount = PlayerPrefs.GetInt("LapMission_Laps", 3),
                timeLimit = PlayerPrefs.GetFloat("LapMission_Time", 90f),
                coinReward = PlayerPrefs.GetInt("LapMission_Reward", 50),
                questType = QuestType.DuaAI
            };

            Debug.Log("🎯 Bắt đầu race mission từ dữ liệu PlayerPrefs");

            // Đợi thêm chút để đảm bảo mọi thứ đã sẵn sàng
            yield return new WaitForSeconds(0.5f);
            StartRaceMission(quest);

            PlayerPrefs.SetInt("LapMission_Active", 0);
        }
    }

    // ✅ Tự động tìm UI components
    void FindUIComponents()
    {
        Debug.Log("🔍 Tìm UI Components...");

        // Tìm timer text
        if (timerText == null)
        {
            timerText = FindUIComponent<TextMeshProUGUI>("TimerText", "Timer", "RaceTimer");
        }

        // Tìm lap text
        if (lapText == null)
        {
            lapText = FindUIComponent<TextMeshProUGUI>("LapText", "Lap", "CurrentLap");
        }

        // Tìm position text
        if (positionText == null)
        {
            positionText = FindUIComponent<TextMeshProUGUI>("PositionText", "Position", "PlayerPosition");
        }

        // Tìm countdown text
        if (countdownText == null)
        {
            countdownText = FindUIComponent<TextMeshProUGUI>("CountdownText", "Countdown", "RaceCountdown");
        }

        // Tìm win/lose panels
        if (winPanel == null)
        {
            winPanel = GameObject.Find("WinPanel") ?? FindInactiveGameObject("WinPanel");
        }

        if (losePanel == null)
        {
            losePanel = GameObject.Find("LosePanel") ?? FindInactiveGameObject("LosePanel");
        }

        // ✅ Tìm result text trong win/lose panels
        if (winPanel != null && winResultText == null)
        {
            winResultText = FindTextInPanel(winPanel, "WinResultText", "ResultText", "WinText", "MessageText");
        }

        if (losePanel != null && loseResultText == null)
        {
            loseResultText = FindTextInPanel(losePanel, "LoseResultText", "ResultText", "LoseText", "MessageText");
        }

        Debug.Log($"✅ UI Found: Timer:{timerText != null}, Lap:{lapText != null}, Position:{positionText != null}, Countdown:{countdownText != null}");
        Debug.Log($"✅ Result Text Found: Win:{winResultText != null}, Lose:{loseResultText != null}");
    }

    // ✅ Tìm text trong panel
    TextMeshProUGUI FindTextInPanel(GameObject panel, params string[] possibleNames)
    {
        // Tìm trong children của panel
        foreach (string name in possibleNames)
        {
            Transform child = panel.transform.Find(name);
            if (child != null)
            {
                TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    Debug.Log($"✅ Found result text: {name} in {panel.name}");
                    return text;
                }
            }
        }

        // Tìm trong tất cả children
        TextMeshProUGUI[] allTexts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (allTexts.Length > 0)
        {
            Debug.Log($"✅ Found first text component in {panel.name}");
            return allTexts[0];
        }

        return null;
    }

    // Helper method để tìm UI component
    T FindUIComponent<T>(params string[] possibleNames) where T : Component
    {
        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name) ?? FindInactiveGameObject(name);
            if (obj != null)
            {
                T component = obj.GetComponent<T>();
                if (component != null)
                {
                    Debug.Log($"✅ Found {typeof(T).Name}: {name}");
                    return component;
                }
            }
        }
        return null;
    }

    // Tìm inactive GameObject
    GameObject FindInactiveGameObject(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.name == name && t.gameObject.scene.isLoaded)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    void InitializeUI()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // ✅ Đảm bảo UI được setup đúng từ đầu
        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            countdownText.text = ""; // Clear text
        }
    }

    void Update()
    {
        if (!raceStarted || raceCompleted) return;

        raceTimeLeft -= Time.deltaTime;
        UpdateTimerUI();
        UpdatePositionUI();

        if (raceTimeLeft <= 0f)
        {
            HandleTimeUp();
        }
    }

    void HandleTimeUp()
    {
        Debug.Log("⏰ Hết thời gian!");

        // ✅ Dừng race trước khi xử lý
        raceStarted = false;
        raceCompleted = true;

        var positions = CalculateRacerPositions();
        if (positions.Count > 0)
        {
            string winner = positions[0].racerName;
            bool playerWon = winner == "Player";

            if (playerWon)
            {
                // ✅ Hiển thị win panel trước khi complete mission
                ShowWinPanel($"Nhiệm vụ thành công!\nBạn đã thắng cuộc đua!\nNhận được {currentCoinReward} coin!");
                CompleteMission(true, "Nhiệm vụ thành công! Bạn đã thắng cuộc đua!");
            }
            else
            {
                // ✅ Hiển thị lose panel trước khi complete mission
                ShowLosePanel($"Hết thời gian!\nBạn không dành được vị trí số 1!\nNhiệm vụ thất bại!");
                CompleteMission(false, "Hết thời gian! Nhiệm vụ thất bại!");
            }
        }
        else
        {
            // ✅ Hiển thị lose panel khi không có racer nào
            ShowLosePanel($"Hết thời gian!\nKhông có dữ liệu racer!\nNhiệm vụ thất bại!");
            CompleteMission(false, "Hết thời gian! Nhiệm vụ thất bại!");
        }
    }

    void FindTrackWaypoints()
    {
        if (waypointParent == null)
        {
            waypointParent = GameObject.Find("TrackWayPoints")?.transform;
        }

        if (waypointParent == null)
        {
            Debug.LogError("❌ Không tìm thấy TrackWayPoints!");
            return;
        }

        List<Transform> foundWaypoints = new List<Transform>();
        foreach (Transform child in waypointParent)
        {
            foundWaypoints.Add(child);
        }

        foundWaypoints.Sort((a, b) =>
        {
            int aNum = ExtractNumber(a.name);
            int bNum = ExtractNumber(b.name);
            return aNum.CompareTo(bNum);
        });

        trackWaypoints = foundWaypoints.ToArray();
        Debug.Log($"✅ Tìm thấy {trackWaypoints.Length} waypoints");
    }

    int ExtractNumber(string name)
    {
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\\d+").Value;
        return int.TryParse(digits, out int result) ? result : 0;
    }

    public void StartRaceMission(QuestData questData)
    {
        Debug.Log("🏁 Bắt đầu race mission!");

        // ✅ Lưu coin reward
        currentCoinReward = questData.coinReward;

        // Reset trạng thái
        raceStarted = false;
        raceCompleted = false;
        allRacers.Clear();
        finishOrder.Clear();
        racerLapCounts.Clear();

        // Setup từ quest data
        maxRaceTime = questData.timeLimit;
        totalLaps = questData.lapCount;
        raceTimeLeft = maxRaceTime;

        // ✅ Thực hiện setup theo thứ tự
        StartCoroutine(SetupRaceCoroutine());
    }

    private IEnumerator SetupRaceCoroutine()
    {
        // Tìm lại UI nếu cần
        FindUIComponents();

        // Tìm player
        yield return StartCoroutine(FindPlayerCoroutine());

        // Setup AI
        SetupAI();

        // Setup UI
        InitializeRaceUI();

        // Đợi một chút để đảm bảo mọi thứ đã sẵn sàng
        yield return new WaitForSeconds(0.5f);

        // Bắt đầu countdown
        StartCoroutine(CountdownAndStartRace());
    }

    private IEnumerator FindPlayerCoroutine()
    {
        int attempts = 0;
        while (player == null && attempts < 10)
        {
            // Thử tìm player bằng nhiều cách
            player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player == null)
            {
                // Tìm trong tất cả GameObject có tên chứa "Player"
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Player") && obj.GetComponent<Car_script>() != null)
                    {
                        player = obj;
                        break;
                    }
                }
            }

            if (player != null)
            {
                Debug.Log($"✅ Tìm thấy Player: {player.name}");
                break;
            }

            attempts++;
            Debug.Log($"🔍 Tìm Player attempt {attempts}...");
            yield return new WaitForSeconds(0.1f);
        }

        if (player == null)
        {
            Debug.LogError("❌ Không tìm thấy Player sau 10 attempts!");
            yield break;
        }

        // Setup player
        SetupPlayer();
    }

    void SetupPlayer()
    {
        if (player == null) return;

        // Reset physics
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Tắt control tạm thời
        var control = player.GetComponent<Car_script>();
        if (control != null) control.enabled = false;

        // Setup progress tracking
        var progress = player.GetComponent<RacerProgressWaypoint>();
        if (progress == null)
        {
            progress = player.AddComponent<RacerProgressWaypoint>();
        }

        progress.totalLaps = totalLaps;
        progress.trackWaypoints = trackWaypoints;
        progress.racerName = "Player";
        progress.ResetProgress();

        // ✅ Đảm bảo player có tag đúng
        if (player.tag != "Player")
        {
            player.tag = "Player";
        }

        allRacers.Add(player);
        racerLapCounts["Player"] = 0;

        Debug.Log($"✅ Player setup hoàn tất: {player.name}");
    }

    void SetupAI()
    {
        // Clear AI cũ
        foreach (var ai in spawnedAIs)
        {
            if (ai != null) Destroy(ai);
        }
        spawnedAIs.Clear();

        // Spawn AI mới
        int aiCount = Mathf.Min(aiSpawnPoints.Count, aiPrefabs.Count);
        for (int i = 0; i < aiCount; i++)
        {
            GameObject aiPrefab = aiPrefabs[i];
            Transform spawnPoint = aiSpawnPoints[i];

            GameObject newAI = Instantiate(aiPrefab, spawnPoint.position, spawnPoint.rotation);
            newAI.name = "AI_" + i;

            // Setup AI progress
            var progress = newAI.GetComponent<RacerProgressWaypoint>() ?? newAI.AddComponent<RacerProgressWaypoint>();
            progress.totalLaps = totalLaps;
            progress.trackWaypoints = trackWaypoints;
            progress.racerName = newAI.name;
            progress.ResetProgress();

            // Setup AI control
            var aiControl = newAI.GetComponent<AICarController>() ?? newAI.AddComponent<AICarController>();
            aiControl.enabled = false;

            spawnedAIs.Add(newAI);
            allRacers.Add(newAI);
            racerLapCounts[newAI.name] = 0;
        }

        Debug.Log($"✅ Setup {aiCount} AI racers");
    }

    void InitializeRaceUI()
    {
        // Tìm lại UI nếu cần
        if (lapText == null || positionText == null || countdownText == null)
        {
            FindUIComponents();
        }

        // Setup UI state
        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true); // ✅ Bật countdown
            countdownText.text = "GET READY!";
        }
    }

    private IEnumerator CountdownAndStartRace()
    {
        Debug.Log("🚦 Bắt đầu countdown...");

        float countdown = countdownTime;

        while (countdown > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(countdown).ToString();
                Debug.Log($"Countdown: {Mathf.CeilToInt(countdown)}");
            }

            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
        }

        yield return new WaitForSeconds(1f);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        StartRace();
    }

    void StartRace()
    {
        Debug.Log("🏁 Bắt đầu đua!");

        raceStarted = true;

        // Bật UI
        if (lapText != null) lapText.gameObject.SetActive(true);
        if (positionText != null) positionText.gameObject.SetActive(true);

        UpdateLapUI(1);

        // Bật player control
        if (player != null)
        {
            var control = player.GetComponent<Car_script>();
            if (control != null) control.enabled = true;

            var progress = player.GetComponent<RacerProgressWaypoint>();
            if (progress != null) progress.StartRace();
        }

        // Bật AI control
        foreach (var ai in spawnedAIs)
        {
            if (ai == null) continue;

            var control = ai.GetComponent<AICarController>();
            if (control != null) control.enabled = true;

            var progress = ai.GetComponent<RacerProgressWaypoint>();
            if (progress != null) progress.StartRace();
        }

        UpdatePositionUI();
        Debug.Log("✅ Race started successfully!");
    }

    public void OnRacerCompletedLap(string racerName, int completedLap)
    {
        racerLapCounts[racerName] = completedLap;
        Debug.Log($"🏁 {racerName} completed lap {completedLap}");
    }

    public void RacerFinished(string racerName)
    {
        if (raceCompleted) return;

        finishOrder.Add(racerName);
        raceCompleted = true;
        raceStarted = false;

        Debug.Log($"🏆 {racerName} finished the race!");

        // Tắt UI
        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);

        bool playerIsFirst = finishOrder.Count == 1 && racerName == "Player";
        bool playerFinished = racerName == "Player";

        if (playerIsFirst)
        {
            // ✅ Player thắng
            ShowWinPanel($"Nhiệm vụ thành công!\nBạn đã giành vị trí số 1!\nNhận được {currentCoinReward} coin!");
            CompleteMission(true, "Player won the race!");
        }
        else if (playerFinished)
        {
            // ✅ Player hoàn thành nhưng không phải top 1
            ShowLosePanel($"Bạn không dành được vị trí số 1!\nNhiệm vụ thất bại!");
            CompleteMission(false, "Player finished, but not first.");
        }
        else
        {
            // Nếu AI về đích trước
            ShowLosePanel($"Bạn không dành được vị trí số 1!\nNhiệm vụ thất bại!");
            CompleteMission(false, "AI finished first.");
        }
    }

    // ✅ Hiển thị win panel với text
    void ShowWinPanel(string message)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (winResultText != null)
            {
                winResultText.text = message;
            }

            Debug.Log($"🏆 Win Panel: {message}");
        }
    }

    // ✅ Hiển thị lose panel với text
    void ShowLosePanel(string message)
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);

            if (loseResultText != null)
            {
                loseResultText.text = message;
            }

            Debug.Log($"💀 Lose Panel: {message}");
        }
    }

    public void CompleteMission(bool success, string reason)
    {
        raceStarted = false;
        raceCompleted = true;

        Debug.Log($"🎯 Mission {(success ? "Success" : "Failed")}: {reason}");

        // Gọi QuestManager
        if (success)
        {
            QuestManager.instance?.CompleteQuest();
        }
        else
        {
            QuestManager.instance?.FailQuest(reason);
        }

        // Cleanup sau delay
        StartCoroutine(DelayedCleanup());
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(7f);
        PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
        SceneManager.LoadScene("Loading");
    }

    private IEnumerator DelayedCleanup()
    {
        yield return new WaitForSeconds(3f);
        CleanupRace();
    }

    void CleanupRace()
    {
        foreach (var ai in spawnedAIs)
        {
            if (ai != null) Destroy(ai);
        }

        spawnedAIs.Clear();
        allRacers.Clear();
        finishOrder.Clear();
        racerLapCounts.Clear();
        StartCoroutine(ReturnToMenu());
        Debug.Log("🧹 Race cleanup completed");
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int min = Mathf.FloorToInt(raceTimeLeft / 60f);
        int sec = Mathf.FloorToInt(raceTimeLeft % 60f);
        timerText.text = $"{min:D2}:{sec:D2}";
    }

    public void UpdateLapUI(int currentLap)
    {
        if (lapText != null)
        {
            lapText.text = $"Lap: {currentLap}/{totalLaps}";
        }
    }

    void UpdatePositionUI()
    {
        if (positionText == null || allRacers.Count == 0) return;

        var positions = CalculateRacerPositions();
        int playerPos = positions.FindIndex(p => p.racerName == "Player") + 1;

        if (playerPos > 0) // Đảm bảo tìm thấy player
        {
            positionText.text = $"POS {playerPos}/{allRacers.Count}";
        }
    }

    public List<RacerPositionData> CalculateRacerPositions()
    {
        List<RacerPositionData> positions = new List<RacerPositionData>();

        foreach (var racer in allRacers)
        {
            if (racer == null) continue;

            var progress = racer.GetComponent<RacerProgressWaypoint>();
            if (progress == null) continue;

            positions.Add(new RacerPositionData
            {
                racer = racer,
                racerName = progress.racerName,
                currentLap = progress.GetCurrentLap(),
                currentWaypointIndex = progress.GetCurrentWaypointIndex(),
                raceProgress = progress.GetDetailedProgress(),
                isFinished = progress.IsFinished()
            });
        }

        positions.Sort((a, b) =>
        {
            if (a.isFinished && !b.isFinished) return -1;
            if (!a.isFinished && b.isFinished) return 1;
            return b.raceProgress.CompareTo(a.raceProgress);
        });

        return positions;
    }
}