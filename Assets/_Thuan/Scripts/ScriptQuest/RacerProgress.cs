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
    private int currentLap = 1;
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
        currentLap = 1;
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
        isProcessingLapCompletion = true; // 🆕 Đánh dấu đang xử lý lap completion

        if (showDebugInfo)
            Debug.Log($"[{racerName}] 🏁 Completed lap {currentLap}! Moving to lap {currentLap + 1}");

        // 🆕 Thông báo đến RaceManager về việc hoàn thành lap TRƯỚC KHI thay đổi currentLap
        RaceManager.Instance.OnRacerCompletedLap(racerName, currentLap);

        // Tăng lap và reset waypoint
        currentLap++;
        currentWaypointIndex = 0;

        // Cập nhật UI cho Player
        if (racerName == "Player")
            RaceManager.Instance.UpdateLapUI(currentLap);

        // Kiểm tra xem đã hoàn thành cuộc đua chưa
        if (currentLap > totalLaps)
        {
            FinishRace();
            return;
        }

        // 🆕 Cho phép xử lý waypoint sau một delay ngắn
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

        // 🆕 Nếu đã hoàn thành cuộc đua, trả về giá trị tối đa
        if (finished) return (float)totalLaps;

        // Tính số lap đã hoàn thành (lap hiện tại - 1)
        float completedLaps = (float)(currentLap - 1);

        // Tiến độ trong lap hiện tại
        float currentLapProgress = 0f;

        if (trackWaypoints.Length > 0)
        {
            // Tiến độ cơ bản từ waypoint đã qua
            currentLapProgress = (float)currentWaypointIndex / trackWaypoints.Length;

            // 🆕 Thêm tiến độ nhỏ dựa trên khoảng cách đến waypoint tiếp theo
            if (currentWaypointIndex < trackWaypoints.Length && !isProcessingLapCompletion)
            {
                Transform targetWaypoint = trackWaypoints[currentWaypointIndex];
                float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);

                // Tính tiến độ micro trong khoảng waypoint hiện tại
                float maxReachDistance = waypointReachDistance * 2f;
                float microProgress = Mathf.Clamp01(1f - (distanceToWaypoint / maxReachDistance));
                microProgress = microProgress / trackWaypoints.Length; // Scale theo số waypoint

                currentLapProgress += microProgress;
            }
        }

        float totalProgress = completedLaps + currentLapProgress;

        // 🆕 Debug log để kiểm tra
        if (showDebugInfo && racerName == "Player")
        {
            Debug.Log($"[{racerName}] Progress: {totalProgress:F3} (Lap: {currentLap}, WP: {currentWaypointIndex}/{trackWaypoints.Length})");
        }

        return totalProgress;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (trackWaypoints == null || trackWaypoints.Length == 0) return;
        if (currentWaypointIndex >= trackWaypoints.Length) return;

        Transform targetWaypoint = trackWaypoints[currentWaypointIndex];

        // Vẽ đường đến waypoint tiếp theo
        Gizmos.color = racerName == "Player" ? Color.green : Color.blue;
        Gizmos.DrawLine(transform.position, targetWaypoint.position);

        // Vẽ vòng tròn quanh waypoint tiếp theo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetWaypoint.position, waypointReachDistance);

#if UNITY_EDITOR
        // 🆕 Hiển thị thông tin debug trong Editor
        if (showDebugInfo)
        {
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                $"{racerName}\nLap: {currentLap}\nWP: {currentWaypointIndex}\nProgress: {GetDetailedProgress():F2}");
        }
#endif
    }
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