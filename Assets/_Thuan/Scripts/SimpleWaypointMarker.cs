using UnityEngine;

public class SimpleWaypointMarker : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject iconObject;
    public TextMesh labelText;

    private Transform playerTransform;

    private void Start()
    {
        // Tìm player
        if (WaypointManager.Instance != null)
        {
            playerTransform = WaypointManager.Instance.player;
        }
    }

    private void Update()
    {
        // Luôn quay về phía player
        if (playerTransform != null)
        {
            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0; // Chỉ xoay theo trục Y

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    // Set label cho waypoint
    public void SetLabel(string text)
    {
        if (labelText != null)
        {
            labelText.text = text;
        }
    }
}