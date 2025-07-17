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

    [Header("Respawn Settings")]
    public float fallThreshold = -30f;
    public float respawnDelay = 2f;
    public LayerMask groundLayer = 1;

    [Header("UI - Sẽ được tự động tìm")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI countdownText;
    public GameObject winPanel;
    public TextMeshProUGUI coinText;
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
    public int currentCoinReward = 0;
    private bool isRespawning = false;

    private List<string> finishOrder = new List<string>();
    private Dictionary<string, int> racerLapCounts = new Dictionary<string, int>();

    // ✅ Thêm reference đến ScriptableObject quest
    private QuestData currentQuestData;

    public bool RaceOver => raceCompleted;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        FindUIComponents();
        InitializeUI();
        FindTrackWaypoints();

        if (PlayerPrefs.GetInt("LapMission_Active", 0) == 1)
        {
            // ✅ Lấy quest data từ QuestManager thay vì PlayerPrefs
            if (QuestManager.instance != null && QuestManager.instance.currentQuest != null)
            {
                currentQuestData = QuestManager.instance.currentQuest;
                Debug.Log("🎯 Bắt đầu race mission từ ScriptableObject");
                yield return new WaitForSeconds(0.5f);
                StartRaceMission(currentQuestData);
            }
            else
            {
                // Fallback nếu không có QuestManager
                QuestData quest = new QuestData
                {
                    questName = "Lap Race",
                    lapCount = PlayerPrefs.GetInt("LapMission_Laps", 3),
                    timeLimit = PlayerPrefs.GetFloat("LapMission_Time", 90f),
                    coinReward = PlayerPrefs.GetInt("LapMission_Reward", 50),
                    questType = QuestType.DuaAI
                };
                currentQuestData = quest;
                Debug.Log("🎯 Bắt đầu race mission từ PlayerPrefs (fallback)");
                yield return new WaitForSeconds(0.5f);
                StartRaceMission(quest);
            }

            PlayerPrefs.SetInt("LapMission_Active", 0);
        }
    }

    void Update()
    {
        if (!raceStarted || raceCompleted) return;

        raceTimeLeft -= Time.deltaTime;
        UpdateTimerUI();
        UpdatePositionUI();

        CheckPlayerFall();

        if (raceTimeLeft <= 0f)
        {
            HandleTimeUp();
        }
    }

    void CheckPlayerFall()
    {
        if (player == null || isRespawning) return;

        if (player.transform.position.y < fallThreshold)
        {
            StartCoroutine(RespawnPlayer());
        }
    }

    IEnumerator RespawnPlayer()
    {
        if (isRespawning) yield break;

        isRespawning = true;
        Debug.Log("🔄 Player rơi khỏi map, đang respawn...");

        var carScript = player.GetComponent<Car_script>();
        if (carScript != null) carScript.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        Transform nearestWaypoint = GetNearestWaypoint();
        Transform lastCheckpoint = player.GetComponent<RacerProgressWaypoint>()?.GetLastCheckpoint();
        if (lastCheckpoint != null)
        {
            player.transform.position = lastCheckpoint.position + Vector3.up * 2f;
            player.transform.rotation = lastCheckpoint.rotation;
            Debug.Log($"✅ Player respawn tại checkpoint đã vượt: {lastCheckpoint.name}");
        }
        else
        {
            if (playerStartPoint != null)
            {
                player.transform.position = playerStartPoint.position;
                player.transform.rotation = playerStartPoint.rotation;
                Debug.Log("✅ Player đã respawn tại start point");
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (carScript != null)
        {
            carScript.enabled = true;
        }

        isRespawning = false;
    }

    Transform GetNearestWaypoint()
    {
        if (trackWaypoints == null || trackWaypoints.Length == 0 || player == null)
            return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (Transform waypoint in trackWaypoints)
        {
            float distance = Vector3.Distance(player.transform.position, waypoint.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    void FindUIComponents()
    {
        Debug.Log("🔍 Tìm UI Components...");

        if (timerText == null)
        {
            timerText = FindUIComponent<TextMeshProUGUI>("TimerText", "Timer", "RaceTimer");
        }

        if (lapText == null)
        {
            lapText = FindUIComponent<TextMeshProUGUI>("LapText", "Lap", "CurrentLap");
        }

        if (positionText == null)
        {
            positionText = FindUIComponent<TextMeshProUGUI>("PositionText", "Position", "PlayerPosition");
        }

        if (countdownText == null)
        {
            countdownText = FindUIComponent<TextMeshProUGUI>("CountdownText", "Countdown", "RaceCountdown");
        }

        if (winPanel == null)
        {
            winPanel = GameObject.Find("WinPanel") ?? FindInactiveGameObject("WinPanel");
        }

        if (losePanel == null)
        {
            losePanel = GameObject.Find("LosePanel") ?? FindInactiveGameObject("LosePanel");
        }

        // ✅ Tìm coin text trong win panel
        if (winPanel != null && coinText == null)
        {
            coinText = FindTextInPanel(winPanel, "CoinText", "CoinRewardText", "CoinAmount", "Coin");
        }

        // ✅ Tìm result text trong win panel
        if (winPanel != null && winResultText == null)
        {
            winResultText = FindTextInPanel(winPanel, "WinResultText", "ResultText", "WinText", "MessageText");
        }

        // ✅ Tìm result text trong lose panel
        if (losePanel != null && loseResultText == null)
        {
            loseResultText = FindTextInPanel(losePanel, "LoseResultText", "ResultText", "LoseText", "MessageText");
        }

        Debug.Log($"✅ UI Found: Timer:{timerText != null}, Lap:{lapText != null}, Position:{positionText != null}, Countdown:{countdownText != null}");
        Debug.Log($"✅ Panels Found: Win:{winPanel != null}, Lose:{losePanel != null}");
        Debug.Log($"✅ Result Text Found: Win:{winResultText != null}, Lose:{loseResultText != null}");
    }

    TextMeshProUGUI FindTextInPanel(GameObject panel, params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            Transform child = panel.transform.Find(name);
            if (child != null)
            {
                TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    Debug.Log($"✅ Found text: {name} in {panel.name}");
                    return text;
                }
            }
        }

        TextMeshProUGUI[] allTexts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in allTexts)
        {
            if (!text.name.ToLower().Contains("button"))
            {
                Debug.Log($"✅ Found first non-button text in {panel.name}: {text.name}");
                return text;
            }
        }

        if (allTexts.Length > 0)
        {
            Debug.Log($"✅ Found first text component in {panel.name}");
            return allTexts[0];
        }

        return null;
    }

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

        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            countdownText.text = "";
        }

        if (coinText != null) coinText.gameObject.SetActive(false);
    }

    void HandleTimeUp()
    {
        Debug.Log("⏰ Hết thời gian!");

        raceStarted = false;
        raceCompleted = true;

        var positions = CalculateRacerPositions();
        if (positions.Count > 0)
        {
            string winner = positions[0].racerName;
            bool playerIsTop1 = winner == "Player";
            bool playerFinished = false;

            if (player != null)
            {
                var progress = player.GetComponent<RacerProgressWaypoint>();
                if (progress != null)
                {
                    playerFinished = progress.IsFinished();
                }
            }

            if (playerIsTop1 && playerFinished)
            {
                ShowWinPanel();
                CompleteMission(true, "Nhiệm vụ thành công! Bạn đã thắng cuộc đua!");
            }
            else
            {
                ShowLosePanel();
                CompleteMission(false, "Hết thời gian! Bạn không thể hoàn thành nhiệm vụ.");
            }
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
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;
        return int.TryParse(digits, out int result) ? result : 0;
    }

    public void StartRaceMission(QuestData questData)
    {
        Debug.Log("🏁 Bắt đầu race mission!");

        // ✅ Lưu reference đến quest data
        currentQuestData = questData;
        currentCoinReward = questData.coinReward;

        raceStarted = false;
        raceCompleted = false;
        allRacers.Clear();
        finishOrder.Clear();
        racerLapCounts.Clear();

        maxRaceTime = questData.timeLimit;
        totalLaps = questData.lapCount;
        raceTimeLeft = maxRaceTime;

        StartCoroutine(SetupRaceCoroutine());
    }

    private IEnumerator SetupRaceCoroutine()
    {
        FindUIComponents();
        yield return StartCoroutine(FindPlayerCoroutine());
        SetupAI();
        InitializeRaceUI();
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(CountdownAndStartRace());
    }

    private IEnumerator FindPlayerCoroutine()
    {
        int attempts = 0;
        while (player == null && attempts < 10)
        {
            player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player == null)
            {
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

        SetupPlayer();
    }

    void SetupPlayer()
    {
        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var control = player.GetComponent<Car_script>();
        if (control != null) control.enabled = false;

        var progress = player.GetComponent<RacerProgressWaypoint>();
        if (progress == null)
        {
            progress = player.AddComponent<RacerProgressWaypoint>();
        }

        progress.totalLaps = totalLaps;
        progress.trackWaypoints = trackWaypoints;
        progress.racerName = "Player";
        progress.ResetProgress();

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
        foreach (var ai in spawnedAIs)
        {
            if (ai != null) Destroy(ai);
        }
        spawnedAIs.Clear();

        int aiCount = Mathf.Min(aiSpawnPoints.Count, aiPrefabs.Count);
        for (int i = 0; i < aiCount; i++)
        {
            GameObject aiPrefab = aiPrefabs[i];
            Transform spawnPoint = aiSpawnPoints[i];

            GameObject newAI = Instantiate(aiPrefab, spawnPoint.position, spawnPoint.rotation);
            newAI.name = "AI_" + i;

            var progress = newAI.GetComponent<RacerProgressWaypoint>() ?? newAI.AddComponent<RacerProgressWaypoint>();
            progress.totalLaps = totalLaps;
            progress.trackWaypoints = trackWaypoints;
            progress.racerName = newAI.name;
            progress.ResetProgress();

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
        if (lapText == null || positionText == null || countdownText == null)
        {
            FindUIComponents();
        }

        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
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

        if (lapText != null) lapText.gameObject.SetActive(true);
        if (positionText != null) positionText.gameObject.SetActive(true);

        UpdateLapUI(1);

        if (player != null)
        {
            var control = player.GetComponent<Car_script>();
            if (control != null) control.enabled = true;

            var progress = player.GetComponent<RacerProgressWaypoint>();
            if (progress != null) progress.StartRace();
        }

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

        if (lapText != null) lapText.gameObject.SetActive(false);
        if (positionText != null) positionText.gameObject.SetActive(false);

        bool playerIsFirst = finishOrder.Count == 1 && racerName == "Player";
        bool playerFinished = racerName == "Player";

        if (playerIsFirst)
        {
            ShowWinPanel();
            CompleteMission(true, "Player won the race!");
        }
        else if (playerFinished)
        {
            // Player finished but not first
            ShowLosePanel();
            CompleteMission(false, "Bạn đã hoàn thành cuộc đua nhưng không giành được vị trí đầu tiên!");
        }
        else
        {
            // AI finished first, player still racing
            ShowLosePanel();
            CompleteMission(false, "AI đã về đích trước bạn!");
        }
    }

    // ✅ Hiển thị win panel với coin từ ScriptableObject
    void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);

            // ✅ Hiển thị message win
            if (winResultText != null)
            {
                winResultText.text = "Nhiệm vụ thành công!\nBạn đã giành vị trí số 1!";
            }

            // ✅ Hiển thị coin reward từ ScriptableObject
            if (coinText != null && currentQuestData != null)
            {
                coinText.gameObject.SetActive(true);
                coinText.text = $"+{currentQuestData.coinReward}";
                Debug.Log($"💰 Coin reward displayed from ScriptableObject: +{currentQuestData.coinReward}");
            }
            else if (coinText != null)
            {
                // Fallback nếu không có ScriptableObject
                coinText.gameObject.SetActive(true);
                coinText.text = $"+{currentCoinReward}";
                Debug.Log($"💰 Coin reward displayed fallback: +{currentCoinReward}");
            }

            Debug.Log("🏆 Win Panel displayed");
        }
    }

    // ✅ Hiển thị lose panel - chỉ hiển thị panel, xóa hết text
    void ShowLosePanel()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);

            // ✅ Xóa hết text trong lose panel
            if (loseResultText != null)
            {
                loseResultText.text = "";
            }

            // ✅ Ẩn coin text khi lose
            if (coinText != null)
            {
                coinText.gameObject.SetActive(false);
            }

            Debug.Log("💀 Lose Panel displayed (no text)");
        }
    }

    public void CompleteMission(bool success, string reason)
    {
        raceStarted = false;
        raceCompleted = true;

        Debug.Log($"🎯 Mission {(success ? "Success" : "Failed")}: {reason}");

        if (success)
        {
            QuestManager.instance?.CompleteQuest();
        }
        else
        {
            QuestManager.instance?.FailQuest(reason);
        }

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

        float displayTime = Mathf.Max(raceTimeLeft, 0f);
        int min = Mathf.FloorToInt(displayTime / 60f);
        int sec = Mathf.FloorToInt(displayTime % 60f);
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

        if (playerPos > 0)
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