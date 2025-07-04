using System.Collections;
using TMPro;
using UnityEngine;

public class BanTocDo : MonoBehaviour
{
    [Header("Settings")]
    public float requiredSpeed = 22.22f; // tương đương 80 km/h
    public string carTag = "Player";
    public TextMeshProUGUI thongbao;

    [Header("Trap Zone")]
    public Transform speedTrapPoint;

    private Transform carTransform;
    private bool isActive = false;
    private bool missionCompleted = false;
    private Collider trapZoneCollider;
    private MeshRenderer trapMeshRenderer;

    private void Awake()
    {
        trapZoneCollider = GetComponent<Collider>();
        trapMeshRenderer = GetComponent<MeshRenderer>();

        if (trapZoneCollider != null)
            trapZoneCollider.enabled = false;

        if (trapMeshRenderer != null)
            trapMeshRenderer.enabled = false; // ẩn mesh khi bắt đầu
    }

    private void Start()
    {
        FindActiveCar();
    }

    private void FindActiveCar()
    {
        GameObject carObj = GameObject.FindGameObjectWithTag(carTag);
        if (carObj != null)
        {
            carTransform = carObj.transform;
        }
        else
        {
            Debug.LogError("Không tìm thấy xe với tag: " + carTag);
        }
    }

    public void StartMission()
    {
        FindActiveCar();

        if (carTransform == null)
        {
            Debug.LogError("🚫 Không thể bắt đầu nhiệm vụ: chưa tìm thấy xe.");
            return;
        }

        missionCompleted = false;
        isActive = true;

        if (trapZoneCollider != null)
            trapZoneCollider.enabled = true;

        if (trapMeshRenderer != null)
            trapMeshRenderer.enabled = true;

        // ✅ Tạo waypoint khi bắt đầu
        WaypointManager.Instance?.CreatePointer(speedTrapPoint.position, null);

        Debug.Log("🚀 Nhiệm vụ SpeedTrap đã bắt đầu");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || missionCompleted || carTransform == null || other.transform != carTransform)
            return;

        Rigidbody rb = carTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float speed = rb.velocity.magnitude;
            Debug.Log($"📷 Vượt qua vùng tốc độ: {speed:F2} m/s");

            if (speed >= requiredSpeed)
            {
                CompleteMission(); // ✅ Đủ tốc độ mới hoàn thành
            }
            else
            {
                Debug.Log("⚠️ Tốc độ không đủ. Cần >= " + requiredSpeed + " m/s");
                StartCoroutine(ShowMessage("Tốc độ không đủ, cần chạy nhanh hơn!", 2f));

                // 🔁 Reset trap zone để người chơi có thể thử lại
                StartCoroutine(ResetTrapZone());
            }
        }
    }

    private IEnumerator ResetTrapZone()
    {
        if (trapZoneCollider != null)
            trapZoneCollider.enabled = false;

        yield return new WaitForSeconds(1f); // chờ xe ra khỏi trigger

        if (!missionCompleted && trapZoneCollider != null)
            trapZoneCollider.enabled = true;

        Debug.Log("🔁 Vùng kiểm tra tốc độ đã reset, có thể thử lại.");
    }

    private IEnumerator ShowMessage(string message, float delay)
    {
        if (thongbao != null)
        {
            thongbao.text = message;
            thongbao.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(delay);

        if (thongbao != null)
        {
            thongbao.text = "";
            thongbao.gameObject.SetActive(false);
        }
    }

    private void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;

        if (trapZoneCollider != null)
            trapZoneCollider.enabled = false;

        if (trapMeshRenderer != null)
            trapMeshRenderer.enabled = false;

        // ✅ Chỉ xóa checkpoint khi hoàn thành
        WaypointManager.Instance?.RemoveWaypoint();

        Debug.Log("✅ Nhiệm vụ SpeedTrap hoàn thành!");
        QuestManager.instance?.CompleteQuest();
    }
}
