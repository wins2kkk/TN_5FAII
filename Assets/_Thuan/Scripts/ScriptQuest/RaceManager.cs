// ========== RACEMANAGER.CS - FIXED VERSION ==========
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Cài đặt đua xe")]
    public int totalLaps = 3;
    public float maxRaceTime = 180f;
    public float countdownTime = 3f;

    [Header("Player & AI")]
    public GameObject playerPrefab;
    public Transform playerStartPoint;
    public List<GameObject> aiPrefabs;
    public List<Transform> aiSpawnPoints;

    [Header("UI")]
    public Text timerText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI countdownText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Track Waypoints")]
    public Transform waypointParent;

    private List<GameObject> spawnedAIs = new List<GameObject>();
    private GameObject player;
    private List<GameObject> allRacers = new List<GameObject>();
    private Transform[] trackWaypoints;

    private float raceTimeLeft;
    private bool raceStarted = false;
    private bool raceCompleted = false;

    // 🆕 Tracking race results
    private List<string> finishOrder = new List<string>();
    private Dictionary<string, int> racerLapCounts = new Dictionary<string, int>();

    public bool RaceOver => raceCompleted;

    void Awake()
    {
        Instance = this;
        InitializeUI();
        FindTrackWaypoints();
    }

    void InitializeUI()
    {
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        lapText?.gameObject.SetActive(false);
        positionText?.gameObject.SetActive(false);
        countdownText?.gameObject.SetActive(false);
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
        Debug.Log("⏰ Time's up! Determining winner based on progress...");

        var positions = CalculateRacerPositions();
        if (positions.Count > 0)
        {
            string winner = positions[0].racerName;
            if (winner == "Player")
            {
                CompleteMission(true, "Time up - Player leads!");
            }
            else
            {
                CompleteMission(false, $"Time up - {winner} leads!");
            }
        }
        else
        {
            CompleteMission(false, "Time up - No clear winner!");
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
            Debug.LogError("Không tìm thấy TrackWayPoints!");
            return;
        }

        List<Transform> foundWaypoints = new List<Transform>();
        foreach (Transform child in waypointParent)
        {
            foundWaypoints.Add(child);
        }

        foundWaypoints.Sort((a, b) =>
        {
            int numA = ExtractNumber(a.name);
            int numB = ExtractNumber(b.name);
            return numA.CompareTo(numB);
        });

        trackWaypoints = foundWaypoints.ToArray();
        Debug.Log($"Đã tìm thấy {trackWaypoints.Length} waypoints cho track");
    }

    int ExtractNumber(string name)
    {
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;
        return int.TryParse(digits, out int result) ? result : 0;
    }

    public void StartRaceMission(QuestData questData)
    {
        // Reset race state
        raceStarted = false;
        raceCompleted = false;
        allRacers.Clear();
        finishOrder.Clear();
        racerLapCounts.Clear();

        maxRaceTime = questData.timeLimit;
        totalLaps = questData.lapCount;
        raceTimeLeft = maxRaceTime;

        SetupPlayer();
        SetupAI();
        InitializeRaceUI();

        StartCoroutine(CountdownAndStartRace());
    }

    void SetupPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && playerStartPoint != null)
        {
            // Disable/enable CharacterController properly
            CharacterController charController = player.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }

            // Move to start position
            player.transform.position = playerStartPoint.position;
            player.transform.rotation = playerStartPoint.rotation;

            // Reset physics
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (charController != null)
            {
                charController.enabled = true;
            }

            // Disable control during countdown
            var control = player.GetComponent<Car_script>();
            if (control != null) control.enabled = false;

            // Setup progress tracking
            var progress = player.GetComponent<RacerProgressWaypoint>();
            if (progress == null) progress = player.AddComponent<RacerProgressWaypoint>();
            progress.totalLaps = totalLaps;
            progress.trackWaypoints = trackWaypoints;
            progress.ResetProgress();
            progress.racerName = "Player";

            allRacers.Add(player);
            racerLapCounts["Player"] = 0;

            Debug.Log("✅ Player setup completed");
        }
        else
        {
            Debug.LogError("❌ Player setup failed - Player or playerStartPoint is null");
        }
    }

    void SetupAI()
    {
        // Clean up old AI
        foreach (var ai in spawnedAIs)
        {
            if (ai != null) Destroy(ai);
        }
        spawnedAIs.Clear();

        // 🆕 Debug spawn points và prefabs
        Debug.Log($"🤖 Setting up AI - Spawn points: {aiSpawnPoints.Count}, Prefabs: {aiPrefabs.Count}");

        // Spawn new AI
        int aiCount = Mathf.Min(aiSpawnPoints.Count, 3); // 🆕 Giới hạn tối đa 3 AI
        for (int i = 0; i < aiCount; i++)
        {
            if (i >= aiSpawnPoints.Count || aiPrefabs.Count == 0)
            {
                Debug.LogWarning($"❌ Cannot spawn AI {i} - insufficient spawn points or prefabs");
                break;
            }

            // 🆕 Chọn prefab theo vòng lặp thay vì random
            GameObject aiPrefab = aiPrefabs[i % aiPrefabs.Count];
            Transform spawnPoint = aiSpawnPoints[i];

            Debug.Log($"🤖 Spawning AI_{i} at {spawnPoint.name}");

            GameObject newAI = Instantiate(aiPrefab, spawnPoint.position, spawnPoint.rotation);
            string aiName = "AI_" + i;
            newAI.name = aiName; // 🆕 Set name cho GameObject

            // 🆕 Setup progress tracking TRƯỚC
            var aiProgress = newAI.GetComponent<RacerProgressWaypoint>();
            if (aiProgress == null) aiProgress = newAI.AddComponent<RacerProgressWaypoint>();

            aiProgress.totalLaps = totalLaps;
            aiProgress.trackWaypoints = trackWaypoints;
            aiProgress.racerName = aiName;
            aiProgress.ResetProgress();
            aiProgress.showDebugInfo = true; // 🆕 Enable debug cho AI

            // 🆕 Setup AI controller
            var aiControl = newAI.GetComponent<AICarController>();
            if (aiControl == null)
            {
                Debug.LogWarning($"⚠️ AI_{i} doesn't have AICarController component!");
                aiControl = newAI.AddComponent<AICarController>();
            }

            aiControl.waypointParent = waypointParent;
            aiControl.FindWaypointsFromParent();
            aiControl.enabled = false; // Disable until race starts

            // 🆕 Ensure AI has required components
            if (newAI.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning($"⚠️ AI_{i} missing Rigidbody component!");
            }

            spawnedAIs.Add(newAI);
            allRacers.Add(newAI);
            racerLapCounts[aiName] = 0;

            Debug.Log($"✅ AI_{i} ({aiName}) setup completed - Total components: {newAI.GetComponents<Component>().Length}");
        }

        Debug.Log($"🏁 Total racers: {allRacers.Count} (1 Player + {spawnedAIs.Count} AI)");
    }

    void InitializeRaceUI()
    {
        lapText?.gameObject.SetActive(false);
        positionText?.gameObject.SetActive(false);
        countdownText?.gameObject.SetActive(true);
    }

    private IEnumerator CountdownAndStartRace()
    {
        float countdown = countdownTime;

        while (countdown > 0)
        {
            countdownText.text = Mathf.CeilToInt(countdown).ToString();
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);
        countdownText?.gameObject.SetActive(false);

        StartRace();
    }

    void StartRace()
    {
        raceStarted = true;
        lapText?.gameObject.SetActive(true);
        positionText?.gameObject.SetActive(true);
        UpdateLapUI(1);

        // Enable player control
        if (player != null)
        {
            var control = player.GetComponent<Car_script>();
            if (control != null) control.enabled = true;

            var progress = player.GetComponent<RacerProgressWaypoint>();
            if (progress != null) progress.StartRace();
        }

        // 🆕 Enable AI controllers với debug info
        foreach (GameObject ai in spawnedAIs)
        {
            if (ai != null)
            {
                var aiControl = ai.GetComponent<AICarController>();
                if (aiControl != null)
                {
                    aiControl.enabled = true;
                    Debug.Log($"✅ {ai.name} AI controller enabled");
                }
                else
                {
                    Debug.LogError($"❌ {ai.name} missing AICarController!");
                }

                var progress = ai.GetComponent<RacerProgressWaypoint>();
                if (progress != null)
                {
                    progress.StartRace();
                    Debug.Log($"✅ {ai.name} progress tracking started");
                }
                else
                {
                    Debug.LogError($"❌ {ai.name} missing RacerProgressWaypoint!");
                }
            }
        }

        UpdatePositionUI();
        Debug.Log("🏁 Race started!");
    }

    // 🆕 Được gọi khi một racer hoàn thành lap
    public void OnRacerCompletedLap(string racerName, int completedLap)
    {
        racerLapCounts[racerName] = completedLap;
        Debug.Log($"🏁 {racerName} completed lap {completedLap}!");

        // 🆕 Debug current lap status
        DebugCurrentLapStatus();
    }

    // 🆕 Debug function để theo dõi lap status
    void DebugCurrentLapStatus()
    {
        Debug.Log("📊 Current Lap Status:");
        foreach (var kvp in racerLapCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} laps");
        }
    }

    public void RacerFinished(string racerName)
    {
        if (raceCompleted) return;

        finishOrder.Add(racerName);
        Debug.Log($"🏆 {racerName} finished the race! (Position: {finishOrder.Count})");

        raceCompleted = true;
        raceStarted = false;

        lapText?.gameObject.SetActive(false);
        positionText?.gameObject.SetActive(false);

        if (racerName == "Player")
        {
            winPanel?.SetActive(true);
            CompleteMission(true, "Player won the race!");
        }
        else
        {
            losePanel?.SetActive(true);
            CompleteMission(false, $"{racerName} won the race!");
        }
    }

    public void CompleteMission(bool success, string reason = "")
    {
        raceCompleted = true;
        raceStarted = false;

        Debug.Log("🏁 RACE RESULTS:");
        Debug.Log($"Reason: {reason}");
        for (int i = 0; i < finishOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {finishOrder[i]}");
        }

        Debug.Log("📊 Final Lap Counts:");
        foreach (var kvp in racerLapCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} laps");
        }

        CleanupRace();

        if (success)
        {
            Debug.Log("✅ Nhiệm vụ đua xe hoàn thành");
            QuestManager.instance?.CompleteQuest();
        }
        else
        {
            Debug.Log("❌ Thua cuộc");
        }
    }

    void CleanupRace()
    {
        foreach (GameObject ai in spawnedAIs)
        {
            if (ai != null) Destroy(ai);
        }
        spawnedAIs.Clear();
        allRacers.Clear();
        finishOrder.Clear();
        racerLapCounts.Clear();
    }

    void UpdatePositionUI()
    {
        if (positionText == null || allRacers.Count == 0) return;

        var racerPositions = CalculateRacerPositions();
        int playerPosition = racerPositions.Count; // 🆕 Default to last position

        // 🆕 Tìm position của player
        for (int i = 0; i < racerPositions.Count; i++)
        {
            if (racerPositions[i].racerName == "Player")
            {
                playerPosition = i + 1;
                break;
            }
        }

        positionText.text = $"POS {playerPosition}/{allRacers.Count}";

        // 🆕 Debug position calculation
        if (Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"🏁 Position Update - Player: {playerPosition}/{allRacers.Count}");
            for (int i = 0; i < Mathf.Min(3, racerPositions.Count); i++)
            {
                var pos = racerPositions[i];
                Debug.Log($"  {i + 1}. {pos.racerName} - Progress: {pos.raceProgress:F2} (Lap: {pos.currentLap}, WP: {pos.currentWaypointIndex})");
            }
        }
    }

    public List<RacerPositionData> CalculateRacerPositions()
    {
        List<RacerPositionData> positions = new List<RacerPositionData>();

        foreach (GameObject racer in allRacers)
        {
            if (racer == null) continue;

            var progress = racer.GetComponent<RacerProgressWaypoint>();
            if (progress == null)
            {
                Debug.LogWarning($"⚠️ {racer.name} missing RacerProgressWaypoint component!");
                continue;
            }

            RacerPositionData positionData = new RacerPositionData
            {
                racer = racer,
                racerName = progress.racerName,
                currentLap = progress.GetCurrentLap(),
                currentWaypointIndex = progress.GetCurrentWaypointIndex(),
                raceProgress = progress.GetDetailedProgress(),
                isFinished = progress.IsFinished()
            };

            positions.Add(positionData);
        }

        // 🆕 Improved sorting logic
        positions.Sort((a, b) =>
        {
            // Finished racers always rank higher
            if (a.isFinished && !b.isFinished) return -1;
            if (!a.isFinished && b.isFinished) return 1;

            // If both finished or both not finished, compare by progress
            int progressComparison = b.raceProgress.CompareTo(a.raceProgress);

            // 🆕 If progress is very close, use lap + waypoint as tiebreaker
            if (Mathf.Abs(a.raceProgress - b.raceProgress) < 0.01f)
            {
                if (a.currentLap != b.currentLap)
                    return b.currentLap.CompareTo(a.currentLap);
                return b.currentWaypointIndex.CompareTo(a.currentWaypointIndex);
            }

            return progressComparison;
        });

        return positions;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int min = Mathf.FloorToInt(raceTimeLeft / 60f);
        int sec = Mathf.FloorToInt(raceTimeLeft % 60f);
        timerText.text = $"{min:D2}:{sec:D2}";
    }

    public void UpdateLapUI(int currentLap)
    {
        if (lapText != null)
            lapText.text = $"Lap: {currentLap}/{totalLaps}";
    }
}