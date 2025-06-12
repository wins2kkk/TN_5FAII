using UnityEngine;
using System.Collections;

public class ParkingMission : MonoBehaviour
{
    [Header("References")]
    public Transform parkingPoint;

    [Header("Settings")]
    public float stayTime = 2f; // Thời gian cần ở yên
    public string carTag = "Player"; // Tag của xe

    private Transform carTransform;
    private bool isActive = false;
    private bool missionCompleted = false;
    private Collider parkingCollider;
    private float timer = 0f;

    private void Awake()
    {
        parkingCollider = GetComponent<Collider>();
        parkingCollider.enabled = false;
    }

    private void Start()
    {
        FindActiveCar();
    }

    private void FindActiveCar()
    {
        // Tìm xe active có tag "Player"
        GameObject carObject = GameObject.FindGameObjectWithTag(carTag);
        if (carObject != null)
        {
            carTransform = carObject.transform;
            Debug.Log($"Found active car: {carTransform.name}");
        }
        else
        {
            Debug.LogError("No active car found with tag: " + carTag);
        }
    }

    public void StartMission()
    {
        // Tìm lại xe trước khi bắt đầu mission (đề phòng xe đã thay đổi)
        FindActiveCar();

        if (carTransform == null)
        {
            Debug.LogError("Cannot start mission: No car found!");
            return;
        }

        missionCompleted = false;
        isActive = true;
        timer = 0f;
        parkingCollider.enabled = true;
        WaypointManager.Instance?.CreatePointer(parkingPoint.position, null);
        Debug.Log("🚀 ParkingMission started");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive || missionCompleted || carTransform == null || other.transform != carTransform)
            return;

        // Kiểm tra xe có nằm hoàn toàn trong khu vực không
        if (IsCarFullyInside())
        {
            timer += Time.deltaTime;
            Debug.Log($"Xe đang đỗ đúng... {timer:F1}s");
            if (timer >= stayTime)
            {
                CompleteMission();
            }
        }
        else
        {
            timer = 0f; // Reset timer nếu xe không nằm hoàn toàn trong khu vực
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (carTransform != null && other.transform == carTransform)
        {
            timer = 0f; // Reset timer khi xe ra khỏi khu vực
            Debug.Log("Xe đã rời khỏi khu vực đỗ");
        }
    }

    // Kiểm tra đơn giản: xe có nằm hoàn toàn trong khu vực không
    private bool IsCarFullyInside()
    {
        if (carTransform == null) return false;

        BoxCollider carBox = carTransform.GetComponent<BoxCollider>();
        if (carBox == null) return false;

        Bounds carBounds = carBox.bounds;
        Bounds parkBounds = parkingCollider.bounds;

        // Kiểm tra bounds của xe có nằm hoàn toàn trong bounds của khu vực đỗ không
        return parkBounds.Contains(carBounds.min) && parkBounds.Contains(carBounds.max);
    }

    private void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;
        parkingCollider.enabled = false;
        WaypointManager.Instance?.RemoveWaypoint();
        Debug.Log("✅ Đỗ xe thành công!");
        QuestManager.instance?.CompleteQuest();
    }
}