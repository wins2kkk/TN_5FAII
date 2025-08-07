using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
public class RacePositionManager : MonoBehaviour
{
    [System.Serializable]
    public class RacerData
    {
        public Transform racerTransform;
        public RacerProgressTracker tracker;
        public bool isPlayer;
        public int currentLap;
        public float totalDistanceAlongTrack;
    }

    public TextMeshProUGUI positionText;
    private List<RacerData> racers = new List<RacerData>();

    void Start()
    {
        racers.Clear();
        var allTrackers = FindObjectsOfType<RacerProgressTracker>();

        foreach (var tracker in allTrackers)
        {
            bool isPlayer = tracker.GetComponent<Car_script>() != null;
            racers.Add(new RacerData
            {
                racerTransform = tracker.transform,
                tracker = tracker,
                isPlayer = isPlayer
            });
        }
    }

    void Update()
    {
        var waypoints = CheckPointManager.Instance.allWaypoints;
        int totalWaypoints = waypoints.Count;

        float trackLength = waypoints[waypoints.Count - 1].distanceFromStart +
                            Vector3.Distance(waypoints[waypoints.Count - 1].transform.position, waypoints[0].transform.position);

        foreach (var r in racers)
        {
            r.currentLap = r.tracker.currentLap;

            int currIndex = r.tracker.currentWaypointIndex >= 0 ? r.tracker.currentWaypointIndex : 0;
            int nextIndex = (currIndex + 1) % totalWaypoints;

            Waypointt currWaypoint = waypoints[currIndex];
            Waypointt nextWaypoint = waypoints[nextIndex];

            float segLength = Vector3.Distance(currWaypoint.transform.position, nextWaypoint.transform.position);
            float distToNext = Vector3.Distance(r.racerTransform.position, nextWaypoint.transform.position);
            float segProgress = (segLength > 0f) ? (1f - Mathf.Clamp01(distToNext / segLength)) : 0f;

            float distanceAlongTrack = currWaypoint.distanceFromStart + segProgress * segLength;
            r.totalDistanceAlongTrack = r.currentLap * trackLength + distanceAlongTrack;
        }

        var sorted = racers.OrderByDescending(r => r.totalDistanceAlongTrack).ToList();
        int playerIndex = sorted.FindIndex(r => r.isPlayer);
        if (playerIndex != -1 && positionText != null)
        {
            positionText.text = $"Position: {playerIndex + 1}/{racers.Count}";
        }
    }
}
