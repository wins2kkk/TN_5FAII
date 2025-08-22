using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;

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
    public Transform[] possiblePickupPoints;
    public Transform[] possibleDropoffPoints;
    public Transform npcExitPosition;
    public Transform npcWalkAwayPoint;

    [Header("Extra Spawn Point")]
    public Transform[] possibleSpawnPoints;

    [Header("Pickup Paths")]
    public Transform[] pickupPathLeft;
    public Transform[] pickupPathRight;

    [Header("NPC")]
    public GameObject npcPrefab;

    private enum TaxiState { WaitingForPickup, PickingUp, GoingToDropoff, DroppingOff, Completed }
    private TaxiState currentState = TaxiState.WaitingForPickup;

    private Transform carTransform;
    private Rigidbody carRigidbody;
    private GameObject currentNPC;
    private TaxiNPC taxiNPCComponent;
    private Vector3 lastCarPosition;
    private bool isActive = false;
    private bool missionCompleted = false;
    private bool missionFailed = false;
    private float carSpeed;

    private Transform pickupPoint;
    private Transform dropoffPoint;

    private bool npcTouchedCar = false;
    private Coroutine currentMovementCoroutine;

    void Start()
    {
        FindActiveCar();
        // 🔎 Tự tìm npcExitPosition nếu chưa có
        if (npcExitPosition == null)
        {
            GameObject exitObj = GameObject.Find("NPC_Exit");
            if (exitObj != null) npcExitPosition = exitObj.transform;
        }

        // 🔎 Tự tìm npcWalkAwayPoint nếu chưa có
        if (npcWalkAwayPoint == null)
        {
            GameObject walkObj = GameObject.Find("NPC_WalkAway");
            if (walkObj != null) npcWalkAwayPoint = walkObj.transform;
        }
    }

    void Update()
    {
        if (!isActive || missionCompleted || missionFailed || currentState == TaxiState.Completed || carTransform == null)
            return;

        CalculateCarSpeed();

        switch (currentState)
        {
            case TaxiState.WaitingForPickup:
                HandleWaitingForPickup();
                break;
            case TaxiState.GoingToDropoff:
                HandleGoingToDropoff();
                break;
        }
    }

    public void StartMission()
    {
        if (missionFailed) return;

        FindActiveCar();
        if (carTransform == null) return;

        pickupPoint = GetNearestPoint(possiblePickupPoints);

        Transform spawnPoint = GetNearestPoint(possibleSpawnPoints);
        if (spawnPoint != null)
            SpawnNPCAt(spawnPoint.position);
        else
            SpawnNPCAt(pickupPoint.position);

        dropoffPoint = possibleDropoffPoints[Random.Range(0, possibleDropoffPoints.Length)];

        isActive = true;
        missionCompleted = false;
        missionFailed = false;
        currentState = TaxiState.WaitingForPickup;
        npcTouchedCar = false;

        taxiNPCComponent?.SetWalking(false);
        UpdateStatusText("Đi đón khách tại điểm đã chỉ định");

        WaypointManager.Instance?.CreatePointer(pickupPoint.position, null);

        if (distanceText) distanceText.gameObject.SetActive(true);
    }

    public void FailMission()
    {
        StopAllCoroutines();

        if (currentNPC != null)
        {
            DOTween.Kill(currentNPC.transform);
            Destroy(currentNPC);
            currentNPC = null;
        }

        WaypointManager.Instance?.RemoveWaypoint();

        isActive = false;
        missionCompleted = false;
        missionFailed = true;
        currentState = TaxiState.Completed;
        npcTouchedCar = false;

        if (statusText) statusText.gameObject.SetActive(false);
        if (distanceText) distanceText.gameObject.SetActive(false);

        // 🔥 Gọi QuestManager để xử lý thất bại
        QuestManager.instance?.FailQuest("Nhiệm vụ Taxi thất bại!");
    }

    Transform GetNearestPoint(Transform[] points)
    {
        if (points == null || points.Length == 0) return null;

        Transform nearest = points[0];
        float minDist = Vector3.Distance(carTransform.position, nearest.position);

        foreach (var p in points)
        {
            float d = Vector3.Distance(carTransform.position, p.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p;
            }
        }
        return nearest;
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

    void SpawnNPCAt(Vector3 position)
    {
        if (currentNPC != null)
        {
            DOTween.Kill(currentNPC.transform);
            Destroy(currentNPC);
        }

        currentNPC = Instantiate(npcPrefab, position, Quaternion.identity);
        taxiNPCComponent = currentNPC.GetComponent<TaxiNPC>();

        var detector = currentNPC.GetComponent<TaxiNPC>();
        if (detector == null) detector = currentNPC.AddComponent<TaxiNPC>();
        detector.Initialize(this);

        taxiNPCComponent?.SetWalking(false);

        var rb = currentNPC.GetComponent<Rigidbody>();
        if (rb) rb.velocity = Vector3.zero;

        var agent = currentNPC.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) agent.enabled = false;

        npcTouchedCar = false;
    }

    public void OnNPCTouchedCar()
    {
        if (currentState != TaxiState.PickingUp || npcTouchedCar) return;
        npcTouchedCar = true;

        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
            DOTween.Kill(currentNPC.transform);
        }

        StartCoroutine(CompletePickupAfterTouch());
    }

    IEnumerator CompletePickupAfterTouch()
    {
        taxiNPCComponent?.SetWalking(false);
        yield return new WaitForSeconds(0.2f);
        Destroy(currentNPC);
        CompletePickup();
    }

    void HandleWaitingForPickup()
    {
        float dist = Vector3.Distance(carTransform.position, pickupPoint.position);
        if (distanceText)
        {
            distanceText.text = $"Khoảng cách đến khách: {dist:F1}m";
            distanceText.gameObject.SetActive(true);
        }

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
        if (distanceText)
        {
            distanceText.text = $"Khoảng cách đến điểm trả: {dist:F1}m";
            distanceText.gameObject.SetActive(true);
        }

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
        currentMovementCoroutine = StartCoroutine(PickupSequence());
    }

    IEnumerator PickupSequence()
    {
        taxiNPCComponent?.SetWalking(false);
        yield return new WaitForSeconds(0.5f);

        if (npcTouchedCar) yield break;

        Transform[] path = null;
        if (pickupPathLeft.Length > 0 && pickupPathRight.Length > 0)
        {
            float distLeft = Vector3.Distance(currentNPC.transform.position, pickupPathLeft[0].position);
            float distRight = Vector3.Distance(currentNPC.transform.position, pickupPathRight[0].position);
            path = distLeft <= distRight ? pickupPathLeft : pickupPathRight;
        }
        else if (pickupPathLeft.Length > 0) path = pickupPathLeft;
        else if (pickupPathRight.Length > 0) path = pickupPathRight;

        if (path != null && path.Length > 0)
            yield return MoveNPC_Path(path);
        else
        {
            Vector3 doorPos = carTransform.position + carTransform.right * 1.2f;
            yield return MoveNPC_DOTween(doorPos);
        }

        if (!npcTouchedCar)
        {
            yield return new WaitForSeconds(0.3f);
            Destroy(currentNPC);
            CompletePickup();
        }
    }

    void StartDropoff()
    {
        currentState = TaxiState.DroppingOff;
        UpdateStatusText("Đang trả khách...");
        StartCoroutine(DropoffSequence());
    }

    IEnumerator DropoffSequence()
    {
        SpawnNPCAt(npcExitPosition.position);
        yield return new WaitForSeconds(1f);
        yield return MoveNPC_DOTween(npcWalkAwayPoint.position);
        yield return new WaitForSeconds(2f);
        Destroy(currentNPC);
        CompleteDropoff();
    }

    IEnumerator MoveNPC_DOTween(Vector3 targetPos)
    {
        if (!currentNPC || npcTouchedCar) yield break;
        taxiNPCComponent?.SetWalking(true);

        float dist = Vector3.Distance(currentNPC.transform.position, targetPos);
        float duration = dist / npcMoveSpeed;

        Vector3 dir = (targetPos - currentNPC.transform.position).normalized;
        if (dir != Vector3.zero)
            currentNPC.transform.rotation = Quaternion.LookRotation(dir);

        bool done = false;
        currentNPC.transform.DOMove(targetPos, duration).SetEase(Ease.Linear).OnComplete(() => done = true);
        yield return new WaitUntil(() => done || npcTouchedCar);

        if (!npcTouchedCar)
            taxiNPCComponent?.SetWalking(false);
    }

    IEnumerator MoveNPC_Path(Transform[] pathPoints)
    {
        if (!currentNPC || pathPoints.Length == 0 || npcTouchedCar) yield break;
        taxiNPCComponent?.SetWalking(true);

        Vector3[] waypoints = new Vector3[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++) waypoints[i] = pathPoints[i].position;

        float totalDist = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++) totalDist += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        float duration = totalDist / npcMoveSpeed;

        Vector3 initialDir = (waypoints[0] - currentNPC.transform.position).normalized;
        if (initialDir != Vector3.zero) currentNPC.transform.rotation = Quaternion.LookRotation(initialDir);

        bool done = false;
        currentNPC.transform.DOPath(waypoints, duration, PathType.Linear).SetEase(Ease.Linear).SetLookAt(0.1f).OnComplete(() => done = true);

        yield return new WaitUntil(() => done || npcTouchedCar);

        if (!npcTouchedCar)
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

        if (distanceText) distanceText.gameObject.SetActive(false);

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
