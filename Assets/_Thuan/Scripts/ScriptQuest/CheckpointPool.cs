using UnityEngine;
using System.Collections.Generic;

public class CheckpointPool : MonoBehaviour
{
    [System.Serializable]

    public class CheckpointInfo
    {
        public string name;
        public Transform transform;
    }

    [Header("Checkpoint Pool")]
    public List<CheckpointInfo> checkpoints = new List<CheckpointInfo>();

    public static CheckpointPool Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        HideAllCheckpoints();
    }

    public void ShowCheckpoint(string checkpointName)
    {
        CheckpointInfo checkpoint = checkpoints.Find(x => x.name == checkpointName);
        if (checkpoint != null && checkpoint.transform != null)
        {
            checkpoint.transform.gameObject.SetActive(true);
            Debug.Log($"✅ Hiện checkpoint: {checkpointName}");
        }
    }

    public void HideCheckpoint(string checkpointName)
    {
        CheckpointInfo checkpoint = checkpoints.Find(x => x.name == checkpointName);
        if (checkpoint != null && checkpoint.transform != null)
        {
            checkpoint.transform.gameObject.SetActive(false);
            Debug.Log($"🔒 Ẩn checkpoint: {checkpointName}");
        }
    }

    public void ShowAllCheckpoints()
    {
        foreach (CheckpointInfo checkpoint in checkpoints)
        {
            if (checkpoint.transform != null)
                checkpoint.transform.gameObject.SetActive(true);
        }
        Debug.Log("🌟 Hiện tất cả checkpoint");
    }

    public void HideAllCheckpoints()
    {
        foreach (CheckpointInfo checkpoint in checkpoints)
        {
            if (checkpoint.transform != null)
                checkpoint.transform.gameObject.SetActive(false);
        }
        Debug.Log("🔒 Ẩn tất cả checkpoint");
    }

    [ContextMenu("Auto Setup Checkpoints")]
    public void AutoSetupCheckpoints()
    {
        checkpoints.Clear();
        CheckpointTrigger[] allCheckpoints = FindObjectsOfType<CheckpointTrigger>();

        foreach (CheckpointTrigger checkpoint in allCheckpoints)
        {
            CheckpointInfo info = new CheckpointInfo
            {
                name = checkpoint.checkpointName,
                transform = checkpoint.transform
            };
            checkpoints.Add(info);
        }

        Debug.Log($"📋 Tự động setup {checkpoints.Count} checkpoint");
    }
}