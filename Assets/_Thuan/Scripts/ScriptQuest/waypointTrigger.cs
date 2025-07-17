using UnityEngine;

public class WaypointTrigger : MonoBehaviour
{
    public int waypointIndex;

    private void OnTriggerEnter(Collider other)
    {
        RacerProgressWaypoint progress = other.GetComponent<RacerProgressWaypoint>();
        if (progress != null)
        {
            progress.UpdateProgress(waypointIndex);
        }
    }
}
