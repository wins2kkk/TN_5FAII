//using UnityEngine;

//public class RaceInitializer : MonoBehaviour
//{
//    void Start()
//    {
//        int totalWaypoints = CheckPointManager.Instance.TotalWaypointCount;

//        // Init cho tất cả xe có RacerProgressTracker
//        var trackers = FindObjectsOfType<PlayerProgressTracker>();
//        foreach (var tracker in trackers)
//        {
//            tracker.InitWaypoints(totalWaypoints);
//        }

//        Debug.Log($"✅ Đã InitWaypoints cho {trackers.Length} xe với {totalWaypoints} waypoint.");
//    }
//}
