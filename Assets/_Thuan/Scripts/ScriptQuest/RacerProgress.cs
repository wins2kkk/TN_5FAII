// ========== RACERPROGRESSWAYPOINT.CS - FIXED VERSION ==========
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RacerProgressWaypoint : MonoBehaviour
{
    public string racerName = "Player";
    public Transform[] trackWaypoints;
    public float waypointReachDistance = 5f;

    private int currentWaypointIndex = 0;
    private int currentLap = 0;
    public int totalLaps = 3;
    private bool finished = false;
    private bool hasStarted = false;

    // 🆕 Thêm để tránh duplicate waypoint trigger
    private bool isProcessingLapCompletion = false;
    private float lastWaypointTriggerTime = 0f;
    private float waypointCooldown = 0.5f; // Cooldown để tránh trigger liên tục
    public Transform lastPassedCheckpoint { get; private set; }

    [Header("Debug")]
    public bool showDebugInfo = true;

    public void StartRace()
    {
        hasStarted = true;
        if (showDebugInfo)
            Debug.Log($"[{racerName}] Race started!");
    }

    public void ResetProgress()
    {
        currentLap = 0;
        currentWaypointIndex = 0;
        finished = false;
        hasStarted = false;
        isProcessingLapCompletion = false; // 🆕
        lastWaypointTriggerTime = 0f; // 🆕

        if (showDebugInfo)
            Debug.Log($"[{racerName}] Progress reset - Lap: {currentLap}, Waypoint: {currentWaypointIndex}");
    }

    void Update()
    {
        if (!hasStarted || finished || RaceManager.Instance == null || RaceManager.Instance.RaceOver) return;
        if (trackWaypoints == null || trackWaypoints.Length == 0) return;
        if (isProcessingLapCompletion) return; // 🆕 Tránh xử lý khi đang complete lap

        CheckWaypointProgress();
    }

    void CheckWaypointProgress()
    {
        if (currentWaypointIndex >= trackWaypoints.Length) return;

        // 🆕 Kiểm tra cooldown để tránh trigger liên tục
        if (Time.time - lastWaypointTriggerTime < waypointCooldown) return;

        Transform targetWaypoint = trackWaypoints[currentWaypointIndex];
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);

        if (distanceToWaypoint <= waypointReachDistance)
        {
            lastWaypointTriggerTime = Time.time; // 🆕 Cập nhật thời gian trigger

            if (showDebugInfo)
                Debug.Log($"[{racerName}] ✅ Reached waypoint {currentWaypointIndex} ({targetWaypoint.name}) - Lap {currentLap}");

            currentWaypointIndex++;

            // Kiểm tra xem đã hoàn thành vòng đua chưa
            if (currentWaypointIndex >= trackWaypoints.Length)
            {
                CompleteCurrentLap();
            }
        }
    }

    // 🆕 Hàm riêng để xử lý hoàn thành lap
    void CompleteCurrentLap()
    {
        isProcessingLapCompletion = true;

        // Thông báo lap hoàn thành
        RaceManager.Instance.OnRacerCompletedLap(racerName, currentLap);

        // Tăng lap
        currentLap++;

        if (showDebugInfo)
            Debug.Log($"[{racerName}] 🏁 Completed lap {currentLap}/{totalLaps}");

        // Reset waypoint
        currentWaypointIndex = 0;

        // Cập nhật UI cho Player
        if (racerName == "Player")
            RaceManager.Instance.UpdateLapUI(currentLap);

        // Kiểm tra hoàn tất cuộc đua
        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        Invoke(nameof(ResumeWaypointProcessing), 0.1f);
    }


    // 🆕 Hàm để resume waypoint processing
    void ResumeWaypointProcessing()
    {
        isProcessingLapCompletion = false;
        if (showDebugInfo)
            Debug.Log($"[{racerName}] 🎯 Ready for lap {currentLap} - Next waypoint: {currentWaypointIndex}");
    }

    // 🆕 Hàm riêng để xử lý hoàn thành cuộc đua
    void FinishRace()
    {
        finished = true;
        isProcessingLapCompletion = false;

        if (showDebugInfo)
            Debug.Log($"[{racerName}] 🏆 FINISHED THE RACE! Total laps completed: {totalLaps}");

        RaceManager.Instance.RacerFinished(racerName);
    }

    // Getter methods
    public int GetCurrentLap() => currentLap;
    public int GetCurrentWaypointIndex() => currentWaypointIndex;
    public bool IsFinished() => finished;
    public bool HasStarted() => hasStarted;

    // 🆕 Fixed detailed progress calculation
    public float GetDetailedProgress()
    {
        if (trackWaypoints == null || trackWaypoints.Length == 0) return 0f;
        if (!hasStarted) return 0f;

        // Nếu đã hoàn thành cuộc đua thì trả về max
        if (finished) return (float)totalLaps;

        // Lap đã hoàn thành = currentLap (vì lap bắt đầu từ 0)
        float completedLaps = (float)currentLap;

        // Tiến độ trong lap hiện tại
        float currentLapProgress = 0f;

        if (trackWaypoints.Length > 0)
        {
            currentLapProgress = (float)currentWaypointIndex / trackWaypoints.Length;

            if (currentWaypointIndex < trackWaypoints.Length && !isProcessingLapCompletion)
            {
                Transform targetWaypoint = trackWaypoints[currentWaypointIndex];
                float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);

                float maxReachDistance = waypointReachDistance * 2f;
                float microProgress = Mathf.Clamp01(1f - (distanceToWaypoint / maxReachDistance));
                microProgress = microProgress / trackWaypoints.Length;

                currentLapProgress += microProgress;
            }
        }

        float totalProgress = completedLaps + currentLapProgress;

        if (showDebugInfo && racerName == "Player")
        {
            Debug.Log($"[{racerName}] Progress: {totalProgress:F3} (Lap: {currentLap}/{totalLaps}, WP: {currentWaypointIndex}/{trackWaypoints.Length})");
        }

        return totalProgress;
    }


    // Debug visualization
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (trackWaypoints == null || trackWaypoints.Length == 0) return;
        if (currentWaypointIndex >= trackWaypoints.Length) return;

        Transform targetWaypoint = trackWaypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        // Vẽ đường đến waypoint
        Gizmos.color = racerName == "Player" ? Color.green : Color.blue;
        Gizmos.DrawLine(transform.position, targetWaypoint.position);

        // Vẽ vòng tròn quanh waypoint
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetWaypoint.position, waypointReachDistance);

        if (showDebugInfo)
        {
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                $"{racerName}\nLap: {currentLap}/{totalLaps}\nWP: {currentWaypointIndex}\nProgress: {GetDetailedProgress():F2}");
        }
    }
#endif


    public Transform GetLastCheckpoint()
    {
        return lastPassedCheckpoint;
    }

    public void UpdateProgress(int newWaypointIndex)
    {
        currentWaypointIndex = newWaypointIndex;

        if (trackWaypoints != null && newWaypointIndex >= 0 && newWaypointIndex < trackWaypoints.Length)
        {
            lastPassedCheckpoint = trackWaypoints[newWaypointIndex];
        }
    }
}