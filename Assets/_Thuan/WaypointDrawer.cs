using UnityEngine;

public class WaypointDrawer : MonoBehaviour
{
    public Transform[] waypoints;
    public float radius = 2f;

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Vẽ sphere
            Gizmos.DrawWireSphere(waypoints[i].position, radius);

            // Vẽ line tới waypoint tiếp theo
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
