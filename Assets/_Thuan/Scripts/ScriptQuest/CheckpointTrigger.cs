using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public string checkpointName = "Check1";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        LapMission lapMission = FindObjectOfType<LapMission>();
        if (lapMission != null)
        {
            lapMission.OnCheckpointHit(checkpointName);
        }
    }
}
