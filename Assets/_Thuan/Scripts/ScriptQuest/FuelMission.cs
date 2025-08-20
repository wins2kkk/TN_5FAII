using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FuelMission : MonoBehaviour
{
    [Header("References")]
    public Transform fuelPoint;
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;
    public int fuelCost = 0;

    [Header("Settings")]
    public string carTag = "Player";

    private Transform carTransform;
    private bool isActive = false;
    private bool missionCompleted = false;
    private Collider fuelZoneCollider;
    private Xang_Script fuelScript;

    private void Awake()
    {
        fuelZoneCollider = GetComponent<Collider>();
        fuelZoneCollider.enabled = false;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmRefuel);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelRefuel);
    }

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
            fuelScript = carTransform.GetComponent<Xang_Script>();
        }
        else
        {
           // Debug.LogError("❌ Không tìm thấy xe với tag: " + carTag);
        }
    }

    public void StartMission()
    {
        FindActiveCar();

        if (carTransform == null || fuelScript == null)
        {
            Debug.LogError("Không thể bắt đầu nhiệm vụ đổ xăng: Thiếu xe hoặc Xang_Script.");
            return;
        }

        isActive = true;
        missionCompleted = false;
        fuelZoneCollider.enabled = true;
        WaypointManager.Instance?.CreatePointer(fuelPoint.position, null);
        Debug.Log("🚀 Nhiệm vụ đổ xăng bắt đầu");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || missionCompleted || carTransform == null) return;

        if (other.transform == carTransform)
        {
            Debug.Log("🛑 Xe đã đến cây xăng");
            ShowConfirmPanel();
        }
    }

    private void ShowConfirmPanel()
    {
        if (confirmPanel != null)
            ChapNhanButton.Instance.Show($"Nhiệm vụ: đổ xăng với {fuelCost} coin?", ConfirmRefuel, CancelRefuel);


    }

    private void CancelRefuel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        Debug.Log("❌ Người chơi đã hủy đổ xăng");
    }

    private void ConfirmRefuel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (!CoinManager.Instance.HasEnoughCoins(fuelCost))
        {
            Debug.Log("🚫 Không đủ coin để đổ xăng!");
            return;
        }

        CoinManager.Instance.SpendCoins(fuelCost);
        fuelScript.currentFuel = fuelScript.maxFuel;

        if (fuelScript.fuelSlider != null)
            fuelScript.fuelSlider.value = fuelScript.currentFuel;

        CompleteMission();
    }

    private void CompleteMission()
    {
        missionCompleted = true;
        isActive = false;
        fuelZoneCollider.enabled = false;
        WaypointManager.Instance?.RemoveWaypoint();

        Debug.Log("✅ Đã đổ xăng thành công!");
        QuestManager.instance?.CompleteQuest(); // Nếu bạn dùng hệ thống nhiệm vụ
    }
}
