// ========== UPDATED RACERPOSITIONDATA ==========
using UnityEngine;

[System.Serializable]
public class RacerPositionData
{
    public GameObject racer;
    public string racerName;
    public int currentLap;
    public int currentWaypointIndex;
    public float raceProgress;
    public bool isFinished; // 🆕
}