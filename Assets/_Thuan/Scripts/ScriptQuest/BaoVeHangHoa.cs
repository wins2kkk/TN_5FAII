using UnityEngine;
using TMPro;

public class BaoVeHangHoa : MonoBehaviour
{
    [Header("Cargo Settings")]
    public int cargoHealth = 100;
    public int damagePerHit = 20;
    public float minCrashForce = 5f;   // lực va chạm tối thiểu để tính hư hại

    [Header("Delivery Settings")]
    public Transform[] deliveryPoints; // danh sách điểm đích có thể giao hàng
    private Transform currentDeliveryPoint;
    public float deliveryRadius = 5f;  // khoảng cách để tính là đã tới nơi

    [Header("UI")]
    public TextMeshProUGUI cargoText;

    private Rigidbody carRigidbody;
    private bool isActive = false;
    private bool missionCompleted = false;

    // cooldown để tránh bị trừ nhiều lần khi đâm 1 lần
    public float damageCooldown = 1f;
    private float lastDamageTime = -999f;

    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null) Debug.LogError("🚨 Xe Player thiếu Rigidbody!");

        if (GetComponent<Collider>() == null) Debug.LogError("🚨 Xe Player thiếu Collider!");

        UpdateUI(false); // ẩn UI khi chưa bắt đầu
    }

    public void StartMission()
    {
        isActive = true;
        missionCompleted = false;
        cargoHealth = 100;
        UpdateUI(true); // bật UI khi bắt đầu mission

        // Random điểm đích
        if (deliveryPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, deliveryPoints.Length);
            currentDeliveryPoint = deliveryPoints[randomIndex];

            // tạo waypoint tới điểm đích
            WaypointManager.Instance?.CreatePointer(currentDeliveryPoint.position, null);

            Debug.Log($"🚀 BaoVeHangHoa mission started → Điểm đến: {currentDeliveryPoint.name}");
        }
        else
        {
            Debug.LogError("🚨 Chưa gán Delivery Points!");
        }
    }

    private void Update()
    {
        if (!isActive || missionCompleted || currentDeliveryPoint == null) return;

        // Kiểm tra nếu xe tới điểm đích
        if (Vector3.Distance(transform.position, currentDeliveryPoint.position) <= deliveryRadius)
        {
            if (cargoHealth == 100)
                CompleteMission();
            else
                FailMission();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isActive || missionCompleted) return;

        float now = Time.time;
        if (now - lastDamageTime < damageCooldown) return; // cooldown

        float crashForce = collision.relativeVelocity.magnitude;
        Debug.Log($"💥 Va chạm với {collision.gameObject.name}, lực = {crashForce}");

        if (crashForce > minCrashForce)
        {
            cargoHealth -= damagePerHit;
            if (cargoHealth < 0) cargoHealth = 0;

            Debug.Log($"📦 Cargo bị hư! Còn lại: {cargoHealth}%");
            UpdateUI(true);

            lastDamageTime = now; // lưu thời gian trừ máu

            if (cargoHealth <= 0)
                FailMission();
        }
    }

    private void UpdateUI(bool show)
    {
        if (cargoText != null)
        {
            cargoText.text = $"Cargo: {cargoHealth}%";
            cargoText.gameObject.SetActive(show);
        }
    }

    private void FailMission()
    {
        isActive = false;
        missionCompleted = true;
        WaypointManager.Instance?.RemoveWaypoint();
        Debug.Log("❌ Mission Failed!");
        QuestManager.instance?.FailQuest("Hàng hỏng");
        UpdateUI(false); // ẩn UI khi thua
    }

    private void CompleteMission()
    {
        if (missionCompleted) return;

        isActive = false;
        missionCompleted = true;
        WaypointManager.Instance?.RemoveWaypoint();

        Debug.Log("✅ Giao hàng thành công!");
        QuestManager.instance?.CompleteQuest();
        UpdateUI(false); // ẩn UI khi thắng
    }

    private void OnDrawGizmosSelected()
    {
        if (deliveryPoints != null && deliveryPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (var point in deliveryPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, deliveryRadius);
            }
        }
    }
}
