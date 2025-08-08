using System.Collections.Generic;
using UnityEngine;

public class RacerProgressTracker : MonoBehaviour
{
    public int currentLap = 0;
    public int currentWaypointIndex = -1;
    public bool completedFullLap = false;

    // public List<int> waypointsPassed = new List<int>()
    // 
    //public float distanceAlongTrack = 0f; // Thêm nếu chưa có

    //public void PassWaypoint(int index, float distanceFromStart)
    //{
    //    currentWaypointIndex = index;
    //    distanceAlongTrack = currentLap * 10000f + distanceFromStart;
    //}
}
