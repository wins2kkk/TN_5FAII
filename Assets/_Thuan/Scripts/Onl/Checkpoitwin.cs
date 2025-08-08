using System.Collections.Generic;
using UnityEngine;

public class Checkpointwin : MonoBehaviour
{
    public int requiredCheckpoints = 5;
    private HashSet<Transform> checkpointsPassed = new HashSet<Transform>();

    public bool IsLapValid => checkpointsPassed.Count >= requiredCheckpoints;
    public int CheckpointsPassedCount => checkpointsPassed.Count;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            if (!checkpointsPassed.Contains(other.transform))
            {
                checkpointsPassed.Add(other.transform);
                Debug.Log($"✅ Checkpoint: {other.name} | Total: {checkpointsPassed.Count}/5");
            }
        }

        if (other.CompareTag("Finish"))
        {
            if (IsLapValid)
            {
                Debug.Log("🏁 Đủ checkpoint! Lap hợp lệ.");
                FindObjectOfType<LapSystem>().PlayerPassedFinishLine();
                checkpointsPassed.Clear(); // Reset cho lap sau
            }
            else
            {
                Debug.Log("❌ Chưa đủ checkpoint! Không tính lap.");
            }
        }
    }

    public void ResetCheckpoints()
    {
        checkpointsPassed.Clear();
    }
}
