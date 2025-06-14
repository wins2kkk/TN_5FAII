using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [Header("Cấu hình NPC")]
    public List<GameObject> sceneNPCs = new List<GameObject>();  // Danh sách NPC có sẵn trong scene
    public float activationRadius = 50f;

    [Header("Player")]
    public Transform player;

    void Start()
    {
        // Tìm Player nếu chưa gán
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("Không tìm thấy Player! Gán Player vào Inspector hoặc gắn tag 'Player' cho nhân vật.");
            return;
        }

        // Nếu có NPC nào null trong danh sách thì cảnh báo
        foreach (GameObject npc in sceneNPCs)
        {
            if (npc == null)
                Debug.LogWarning("NPC trong danh sách chưa được gán đầy đủ trong Inspector!", this);
        }
    }

    void Update()
    {
        if (player == null) return;

        foreach (GameObject npc in sceneNPCs)
        {
            if (npc == null) continue;

            float dist = Vector3.Distance(player.position, npc.transform.position);
            bool shouldBeActive = dist <= activationRadius;

            if (npc.activeSelf != shouldBeActive)
                npc.SetActive(shouldBeActive);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, activationRadius);
        }
    }
}
