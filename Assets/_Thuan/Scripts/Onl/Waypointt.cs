using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Waypointt : MonoBehaviour
{
    [Header("Waypoint Status")]
    public Waypointt previousWaypoint;
    public Waypointt nextWaypoint;

    [Range(0f, 6f)]
    public float waypointWidth = 5f;

    [Header("Waypoint Info")]
    public int waypointIndex; // 👈 Thêm index của waypoint
    [Header("Distance Along Track")]
    public float distanceFromStart = 0f;

    private void Reset()
    {
        // Tự thêm BoxCollider trigger nếu chưa có
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(waypointWidth, 2f, 1f); // Dễ bắt trigger khi xe qua
    }

    public Vector3 GetPosition()
    {
        Vector3 minBound = transform.position + transform.right * waypointWidth / 2f;
        Vector3 maxBound = transform.position - transform.right * waypointWidth / 2f;

        return Vector3.Lerp(minBound, maxBound, Random.Range(0f, 1f));
    }

    private void OnTriggerEnter(Collider other)
    {
        RacerProgressTracker tracker = other.GetComponent<RacerProgressTracker>();
        if (tracker != null)
        {
            int totalWaypoints = CheckPointManager.Instance.TotalWaypointCount;

            // Nếu đi đúng thứ tự (hoặc quay vòng lại 0)
            if ((tracker.currentWaypointIndex + 1) % totalWaypoints == waypointIndex)
            {
                tracker.currentWaypointIndex = waypointIndex;

                // Nếu quay lại waypoint 0 thì tăng lap
                if (waypointIndex == 0)
                {
                    tracker.currentLap++;
                }
            }
        }
    }
}