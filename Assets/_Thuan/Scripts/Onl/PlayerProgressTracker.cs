using UnityEngine;

public class PlayerProgressTracker : MonoBehaviour
{
    public int currentLap = 0;
    public int currentWaypointIndex = 0;
    public int totalWaypoints = 0;
    public bool[] passedWaypoints;
    public float distanceAlongTrack = 0f;

    [Header("Lap Settings")]
    public int checkpointsNeeded = 5; // số checkpoint tối thiểu để tính lap
    private bool readyToFinishLap = false;

    private void Start()
    {
        totalWaypoints = CheckPointManager.Instance.TotalWaypointCount;
        passedWaypoints = new bool[totalWaypoints];
    }

    public void PassWaypoint(int index, float distanceFromStart)
    {
        currentWaypointIndex = index;
        distanceAlongTrack = currentLap * 10000f + distanceFromStart;

        if (index >= 0 && index < totalWaypoints)
            passedWaypoints[index] = true;

        // Kiểm tra đã qua đủ số checkpoint yêu cầu chưa
        int passedCount = 0;
        foreach (bool passed in passedWaypoints)
            if (passed) passedCount++;

        if (passedCount >= checkpointsNeeded)
            readyToFinishLap = true;
    }

    public bool CanFinishLap()
    {
        return readyToFinishLap;
    }

    public void CompleteLap()
    {
        if (readyToFinishLap)
        {
            currentLap++;
            readyToFinishLap = false;
            for (int i = 0; i < passedWaypoints.Length; i++)
                passedWaypoints[i] = false;

            Debug.Log($"✅ Player hoàn thành Lap {currentLap}");
        }
        else
        {
            Debug.Log("⚠ Chưa qua đủ checkpoint, không tính lap");
        }
    }
}
