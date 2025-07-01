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
           // Debug.Log($"🚘 Đã tìm thấy xe: {carTransform.name}");
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
            trapMeshRenderer.enabled = true; // hiện mesh khi nhận nhiệm vụ

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
                CompleteMission();
            }
            else
            {
                Debug.Log("⚠️ Tốc độ không đủ. Cần >= " + requiredSpeed + " m/s");
                StartCoroutine(ShowMessage("Tốc độ không đủ cần chạy nhanh hơn", 2f));
            }
        }
    }

    private IEnumerator ShowMessage(string message, float delay)
    {
        thongbao.text = message;
        thongbao.gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        thongbao.text = "";
        thongbao.gameObject.SetActive(false);
    }

    private void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;

        if (trapZoneCollider != null)
            trapZoneCollider.enabled = false;

        if (trapMeshRenderer != null)
            trapMeshRenderer.enabled = false; // ẩn mesh khi hoàn thành

        WaypointManager.Instance?.RemoveWaypoint();
        Debug.Log("✅ Nhiệm vụ SpeedTrap hoàn thành!");
        QuestManager.instance?.CompleteQuest();
    }
}
