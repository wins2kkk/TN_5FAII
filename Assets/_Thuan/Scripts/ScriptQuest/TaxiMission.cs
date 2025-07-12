using UnityEngine;
using System.Collections;
using TMPro;

public class TaxiMission : MonoBehaviour
{
    [Header("Settings")]
    public string carTag = "Player";
    public float pickupRadius = 5f;
    public float dropoffRadius = 5f;
    public float npcMoveSpeed = 2f;
    public float carSpeedThreshold = 0.5f;

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI distanceText;

    [Header("Points")]
    public Transform pickupPoint;
    public Transform dropoffPoint;
    public Transform npcSpawnPoint;
    public Transform npcSeatPosition;
    public Transform npcExitPosition;
    public Transform npcWalkAwayPoint;

    [Header("NPC")]
    public GameObject npcPrefab;

    private enum TaxiState { WaitingForPickup, PickingUp, GoingToDropoff, DroppingOff, Completed }

    private TaxiState currentState = TaxiState.WaitingForPickup;
    private Transform carTransform;
    private Rigidbody carRigidbody;
    private GameObject currentNPC;
    private Renderer[] npcRenderers;
    private TaxiNPC taxiNPCComponent;
    private Vector3 lastCarPosition;
    private bool isActive = false;
    private bool missionCompleted = false;
    private float carSpeed;

    void Start() => FindActiveCar();

    void Update()
    {
        if (!isActive || missionCompleted || carTransform == null) return;

        CalculateCarSpeed();

        switch (currentState)
        {
            case TaxiState.WaitingForPickup: HandleWaitingForPickup(); break;
            case TaxiState.GoingToDropoff: HandleGoingToDropoff(); break;
        }
    }

    public void StartMission()
    {
        FindActiveCar();
        if (carTransform == null) return;

        isActive = true;
        missionCompleted = false;
        currentState = TaxiState.WaitingForPickup;

        SpawnNPC();
        WaypointManager.Instance?.CreatePointer(pickupPoint.position, null);
        UpdateStatusText("Đi đón khách tại điểm đã chỉ định");
    }

    void FindActiveCar()
    {
        var car = GameObject.FindGameObjectWithTag(carTag);
        if (car)
        {
            carTransform = car.transform;
            carRigidbody = car.GetComponent<Rigidbody>();
            lastCarPosition = carTransform.position;
        }
    }

    void CalculateCarSpeed()
    {
        carSpeed = carRigidbody ? carRigidbody.velocity.magnitude :
                   Vector3.Distance(carTransform.position, lastCarPosition) / Time.deltaTime;

        lastCarPosition = carTransform.position;
    }

    bool IsCarStopped() => carSpeed < carSpeedThreshold;

    void SpawnNPC()
    {
        currentNPC = Instantiate(npcPrefab, npcSpawnPoint.position, npcSpawnPoint.rotation);
        taxiNPCComponent = currentNPC.GetComponent<TaxiNPC>();

        var model = currentNPC.transform.Find("Model");
        npcRenderers = model ? model.GetComponentsInChildren<Renderer>() : currentNPC.GetComponentsInChildren<Renderer>();
    }

    void HandleWaitingForPickup()
    {
        float dist = Vector3.Distance(carTransform.position, pickupPoint.position);
        distanceText.text = $"Khoảng cách đến khách: {dist:F1}m";

        if (dist <= pickupRadius)
        {
            if (IsCarStopped()) StartPickup();
            else UpdateStatusText("Dừng xe để đón khách");
        }
        else UpdateStatusText("Đi đón khách tại điểm đã chỉ định");
    }

    void HandleGoingToDropoff()
    {
        float dist = Vector3.Distance(carTransform.position, dropoffPoint.position);
        distanceText.text = $"Khoảng cách đến điểm trả: {dist:F1}m";

        if (dist <= dropoffRadius)
        {
            if (IsCarStopped()) StartDropoff();
            else UpdateStatusText("Dừng xe để trả khách");
        }
        else UpdateStatusText("Đưa khách đến điểm trả");
    }

    void StartPickup()
    {
        currentState = TaxiState.PickingUp;
        UpdateStatusText("Đang đón khách...");
        StartCoroutine(PickupSequence());
    }

    IEnumerator PickupSequence()
    {
        taxiNPCComponent?.LookAtTarget(carTransform);
        taxiNPCComponent?.SetWalking(false);
        taxiNPCComponent?.PlayGreeting();

        taxiNPCComponent?.animator?.SetTrigger("Wave");

        yield return new WaitForSeconds(2f); // Wait for wave

        if (npcExitPosition != null)
            yield return MoveNPC(currentNPC.transform.position, npcExitPosition.position);

        HideNPC();

        if (npcSeatPosition != null)
        {
            currentNPC.transform.position = npcSeatPosition.position;
            currentNPC.transform.rotation = npcSeatPosition.rotation;
        }

        CompletePickup();
    }

    void HideNPC()
    {
        foreach (var r in npcRenderers)
            if (r) r.enabled = false;
    }

    void ShowNPC()
    {
        foreach (var r in npcRenderers)
            if (r) r.enabled = true;
    }

    void StartDropoff()
    {
        currentState = TaxiState.DroppingOff;
        UpdateStatusText("Đang trả khách...");
        StartCoroutine(DropoffSequence());
    }

    IEnumerator DropoffSequence()
    {
        ShowNPC();

        Vector3 exitPos = npcExitPosition ? npcExitPosition.position : dropoffPoint.position;
        currentNPC.transform.position = exitPos;
        taxiNPCComponent?.LookAtTarget(carTransform);
        taxiNPCComponent?.PlayFarewell();

        taxiNPCComponent?.animator?.SetTrigger("Wave");
        yield return new WaitForSeconds(2f);

        if (npcWalkAwayPoint != null)
            yield return MoveNPC(exitPos, npcWalkAwayPoint.position);
        else
        {
            Vector3 dir = (exitPos - carTransform.position).normalized;
            yield return MoveNPC(exitPos, exitPos + dir * 10f);
        }

        taxiNPCComponent?.SetIdle();

        yield return new WaitForSeconds(2.5f); // 🔁 Đợi lâu hơn trước khi Destroy

        Destroy(currentNPC);
        CompleteDropoff();
    }

    IEnumerator MoveNPC(Vector3 from, Vector3 to)
    {
        taxiNPCComponent?.SetWalking(true);
        float dist = Vector3.Distance(from, to);
        float time = dist / npcMoveSpeed;
        float t = 0f;

        Quaternion rot = Quaternion.LookRotation(to - from);
        currentNPC.transform.rotation = rot;

        while (t < time)
        {
            if (!currentNPC) yield break;
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / time);
            currentNPC.transform.position = Vector3.Lerp(from, to, progress);
            yield return null;
        }

        currentNPC.transform.position = to;
        taxiNPCComponent?.SetWalking(false);
    }

    void CompletePickup()
    {
        currentState = TaxiState.GoingToDropoff;
        WaypointManager.Instance?.RemoveWaypoint();
        WaypointManager.Instance?.CreatePointer(dropoffPoint.position, null);
        UpdateStatusText("Đưa khách đến điểm trả");
    }

    void CompleteDropoff()
    {
        missionCompleted = true;
        isActive = false;
        currentState = TaxiState.Completed;

        WaypointManager.Instance?.RemoveWaypoint();
        UpdateStatusText("Nhiệm vụ hoàn thành!");
        distanceText?.gameObject.SetActive(false);

        CoinManager.Instance?.AddCoins(300);
        QuestManager.instance?.CompleteQuest();
    }

    void UpdateStatusText(string msg)
    {
        if (statusText)
        {
            statusText.text = msg;
            statusText.gameObject.SetActive(true);
        }
    }
}
