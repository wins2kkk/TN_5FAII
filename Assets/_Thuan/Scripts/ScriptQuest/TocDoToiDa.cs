using UnityEngine;
using TMPro;

public class TocDoToiDa : MonoBehaviour
{
    [Header("References")]
    public Transform[] targetPoints;        // Danh sách điểm có thể chọn
    public GameObject targetPrefab;         // Prefab vùng đích (có Collider IsTrigger)
    public string carTag = "Player";        // Tag của xe

    [Header("Settings")]
    public float maxAllowedSpeedKmh = 70f;  // Giới hạn tốc độ
    public TextMeshProUGUI speedText;       // Hiển thị tốc độ hiện tại

    private Transform carTransform;
    private Rigidbody carRb;
    private bool isActive = false;
    private GameObject currentTarget;       // Vùng đích hiện tại

    private void Start()
    {
        FindActiveCar();
    }

    private void FindActiveCar()
    {
        GameObject carObject = GameObject.FindGameObjectWithTag(carTag);
        if (carObject != null)
        {
            carTransform = carObject.transform;
            carRb = carObject.GetComponent<Rigidbody>();
        }
    }

    public void StartMission()
    {
        FindActiveCar();

        if (carTransform == null || carRb == null)
        {
            Debug.LogError("Không tìm thấy xe Player!");
            return;
        }

        // Xóa target cũ nếu có
        if (currentTarget != null) Destroy(currentTarget);

        // Chọn vị trí đích ngẫu nhiên
        Transform targetPoint = targetPoints[Random.Range(0, targetPoints.Length)];

        // Spawn vùng đích
        currentTarget = Instantiate(targetPrefab, targetPoint.position, targetPoint.rotation);
        Collider col = currentTarget.GetComponent<Collider>();
        if (col == null)
            col = currentTarget.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Thêm script xử lý trigger
        MissionTargetTrigger trigger = currentTarget.AddComponent<MissionTargetTrigger>();
        trigger.mission = this;

        // Bật waypoint dẫn đường nếu có
        WaypointManager.Instance?.CreatePointer(targetPoint.position, null);

        isActive = true;

        if (speedText != null)
        {
            speedText.text = "Tốc độ: 0 km/h";
            speedText.gameObject.SetActive(true);
        }

        Debug.Log("🚀 Bắt đầu thử thách tốc độ giới hạn!");
    }

    private void Update()
    {
        if (!isActive || carRb == null) return;

        float speedKmh = carRb.velocity.magnitude * 3.6f; // m/s -> km/h

        // Hiển thị tốc độ hiện tại
        if (speedText != null)
            speedText.text = $"Tốc độ: {speedKmh:F1} km/h";

        // Nếu vượt quá tốc độ cho phép => thua
        if (speedKmh > maxAllowedSpeedKmh + 0.5f)
        {
            FailMission();
        }
    }

    public void OnReachTarget()
    {
        if (!isActive) return;

        CompleteMission();
    }

    private void CompleteMission()
    {
        StopMission();
        Debug.Log("✅ Hoàn thành thử thách tốc độ giới hạn!");
        QuestManager.instance?.CompleteQuest();
    }

    private void FailMission()
    {
        StopMission();
        Debug.Log("❌ Vượt quá tốc độ cho phép!");
        QuestManager.instance?.FailQuest("Hetgio");
    }

    public void StopMission()
    {
        isActive = false;

        // Ẩn text tốc độ
        if (speedText != null)
        {
            speedText.text = "";
            speedText.gameObject.SetActive(false);
        }

        // Xóa target và waypoint
        if (currentTarget != null) Destroy(currentTarget);
        WaypointManager.Instance?.RemoveWaypoint();
    }
}

// Script gắn vào vùng đích
public class MissionTargetTrigger : MonoBehaviour
{
    public TocDoToiDa mission;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(mission.carTag))
        {
            mission.OnReachTarget();
        }
    }
}
