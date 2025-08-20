using UnityEngine;

public class SurvivalCheckpoint : MonoBehaviour
{
    public int index; // số thứ tự checkpoint trong mission
    private SurvivalRaceMission mission;

    private void Start()
    {
        mission = FindObjectOfType<SurvivalRaceMission>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Checkpoint {index}] Triggered by {other.name}");

        if (mission == null) return;

        if (other.CompareTag(mission.carTag))
        {
            Debug.Log($"[Checkpoint {index}] Player entered ✅");
            mission.PassCheckpoint(index);
        }
    }
}
