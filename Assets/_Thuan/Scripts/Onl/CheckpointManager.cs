using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public static CheckPointManager Instance;

    [Header("Tự động load waypoint")]
    public Transform waypointParent;   // Kéo GameObject WaypointOrigin vào đây
    public List<Waypointt> allWaypoints = new List<Waypointt>();

    public int TotalWaypointCount => allWaypoints.Count;

    private void Awake()
    {
        Instance = this;
        allWaypoints.Clear();

        if (waypointParent != null)
        {
            // Tự động lấy tất cả waypoint con theo thứ tự hierarchy
            foreach (Transform child in waypointParent)
            {
                Waypointt wp = child.GetComponent<Waypointt>();
                if (wp != null)
                    allWaypoints.Add(wp);
            }
        }
        else
        {
            // Nếu không set waypointParent, tìm toàn bộ waypoint trong scene
            foreach (var wp in FindObjectsOfType<Waypointt>())
            {
                allWaypoints.Add(wp);
            }
        }

        // Gán index
        for (int i = 0; i < allWaypoints.Count; i++)
        {
            allWaypoints[i].waypointIndex = i;
        }

        // Tính DistanceFromStart tự động

        float cumulativeDistance = 0f;
        for (int i = 0; i < allWaypoints.Count; i++)
        {
            Waypointt wp = allWaypoints[i];
            if (i > 0)
            {
                cumulativeDistance += Vector3.Distance(allWaypoints[i - 1].transform.position, wp.transform.position);
            }
            wp.distanceFromStart = cumulativeDistance;
        }


    }
}