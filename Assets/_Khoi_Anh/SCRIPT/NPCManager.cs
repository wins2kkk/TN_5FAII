using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class NPCType
{
    public string name;
    public GameObject prefab;
    public int count;
}

public class NPCManager : MonoBehaviour
{
    [Header("Cấu hình NPC")]
    public List<NPCType> npcTypes = new List<NPCType>();
    public float activationRadius = 50f;

    [Header("Player")]
    public Transform player;

    private List<GameObject> npcPool = new List<GameObject>();

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("Không tìm thấy Player! Gán Player vào Inspector hoặc gắn tag 'Player' cho nhân vật.");
            return;
        }

        foreach (var type in npcTypes)
        {
            for (int i = 0; i < type.count; i++)
            {
                Vector3 spawnPos = GetRandomPoint(player.position, activationRadius);
                GameObject npc = Instantiate(type.prefab, spawnPos, Quaternion.identity);
                npc.SetActive(false);
                npcPool.Add(npc);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        foreach (GameObject npc in npcPool)
        {
            if (npc == null) continue;

            float dist = Vector3.Distance(player.position, npc.transform.position);
            bool shouldBeActive = dist <= activationRadius;

            if (npc.activeSelf != shouldBeActive)
            {
                npc.SetActive(shouldBeActive);
            }
        }
    }

    Vector3 GetRandomPoint(Vector3 center, float range)
    {
        for (int i = 0; i < 10; i++) // thử nhiều lần để tìm được điểm hợp lệ trên NavMesh
        {
            Vector2 random = Random.insideUnitCircle * range;
            Vector3 point = new Vector3(center.x + random.x, center.y, center.z + random.y);

            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        Debug.LogWarning("Không tìm thấy vị trí hợp lệ trên NavMesh, spawn tại vị trí trung tâm");
        return center;
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
