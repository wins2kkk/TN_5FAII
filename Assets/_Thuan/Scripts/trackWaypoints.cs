using UnityEngine;

[System.Serializable]
public class trackWaypoints : MonoBehaviour
{
    [Header("Waypoint Creation")]
    public GameObject waypointPrefab;
    public Transform waypointParent;

    [ContextMenu("Create Waypoint Here")]
    void CreateWaypoint()
    {
        if (waypointPrefab != null)
        {
            GameObject newWaypoint = Instantiate(waypointPrefab, transform.position, Quaternion.identity);
            if (waypointParent != null)
            {
                newWaypoint.transform.SetParent(waypointParent);
            }
            newWaypoint.name = "Waypoint_" + waypointParent.childCount;
        }
    }
}