using UnityEngine;
using System.Collections.Generic;

public class AICarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float maxSpeed = 15f;  // Giảm tốc độ để dễ cua hơn
    public float acceleration = 10f;
    public float brakeForce = 20f;
    public float maxSteerAngle = 45f;  // Tăng góc lái tối đa
    public float steerSpeed = 8f;  // Tăng tốc độ lái

    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    public float waypointReachDistance = 3f;
    public bool loopWaypoints = true;
    public Transform waypointParent; // 👈 THÊM DÒNG NÀY


    [Header("Wheel References")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Visual Settings")]
    public float wheelRotationSpeed = 360f;

    private Rigidbody rb;
    private int currentWaypointIndex = 0;
    private float currentSpeed = 0f;
    private float currentSteerAngle = 0f;
    private float targetSteerAngle = 0f;

    [Header("AI Avoidance Settings")]
    public string aiTag = "AI"; // tag của các xe AI khác
    public float avoidDistance = 6f; // Khoảng cách an toàn để tránh va chạm
    public float avoidSlowFactor = 0.5f; // Tốc độ sẽ giảm xuống bao nhiêu % nếu gần xe khác

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing on AI Car!");
            return;
        }

        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        // Tự tìm waypoint nếu chưa có
        if (waypoints == null || waypoints.Length == 0)
        {
            FindWaypointsFromParent();
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned or found for AI car: " + name);
        }
    }


    public void FindWaypointsFromParent()
    {
        if (waypointParent == null)
        {
            Debug.LogWarning("waypointParent chưa được gán!");
            return;
        }

        List<Transform> foundWaypoints = new List<Transform>();

        foreach (Transform child in waypointParent)
        {
            foundWaypoints.Add(child);
        }

        // Sắp xếp theo số trong tên Waypoint
        foundWaypoints.Sort((a, b) =>
        {
            int numA = ExtractNumber(a.name);
            int numB = ExtractNumber(b.name);
            return numA.CompareTo(numB);
        });

        waypoints = foundWaypoints.ToArray();
    }

    // Hàm hỗ trợ để tách số từ tên waypoint
    int ExtractNumber(string name)
    {
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;
        return int.TryParse(digits, out int result) ? result : 0;
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        // Debug thông tin
        if (Debug.isDebugBuild)
        {
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);
            Debug.DrawLine(transform.position, waypoints[currentWaypointIndex].position, Color.red);
        }

        // Tính toán hướng đến waypoint tiếp theo
        Vector3 targetDirection = CalculateTargetDirection();

        // Tính toán góc lái cần thiết
        CalculateSteerInput(targetDirection);

        // Tính toán tốc độ dựa trên góc cua
        CalculateSpeedInput();

        // Áp dụng lực di chuyển và lái
        ApplyMovement();

        // Cập nhật waypoint
        CheckWaypoint();

        // Cập nhật animation bánh xe
        UpdateWheelVisuals();

        // Debug log (chỉ mỗi giây một lần)
        if (Time.fixedTime % 1f < Time.fixedDeltaTime)
        {
            float angleToTarget = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
            //Debug.Log($"Car: {name} | Speed: {currentSpeed:F1} | Steer: {currentSteerAngle:F1} | Angle to target: {angleToTarget:F1}");
        }
    }

    Vector3 CalculateTargetDirection()
    {
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // Look ahead - nhìn về waypoint tiếp theo nếu gần waypoint hiện tại
        float distanceToCurrentWaypoint = Vector3.Distance(transform.position, targetPosition);
        if (distanceToCurrentWaypoint < waypointReachDistance * 1.5f && waypoints.Length > 1)
        {
            int nextWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Vector3 nextWaypointPos = waypoints[nextWaypointIndex].position;

            // Blend giữa waypoint hiện tại và waypoint tiếp theo
            float blendFactor = 1f - (distanceToCurrentWaypoint / (waypointReachDistance * 1.5f));
            targetPosition = Vector3.Lerp(targetPosition, nextWaypointPos, blendFactor * 0.5f);
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        return direction;
    }

    void CalculateSteerInput(Vector3 targetDirection)
    {
        // Tính góc giữa hướng xe hiện tại và hướng đến waypoint
        float angleToTarget = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);

        // Tăng độ nhạy của góc lái
        float steerInput = angleToTarget / maxSteerAngle;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);

        // Áp dụng curve để tăng độ nhạy khi góc nhỏ
        steerInput = Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), 0.7f);

        targetSteerAngle = steerInput * maxSteerAngle;

        // Làm mượt góc lái với tốc độ cao hơn
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSpeed * 2f * Time.fixedDeltaTime);
    }

    void CalculateSpeedInput()
    {
        // --- 1. Giảm tốc khi cua gắt ---
        float steerIntensity = Mathf.Pow(Mathf.Abs(currentSteerAngle) / maxSteerAngle, 1.5f); // Độ gắt của góc cua
        float steerFactor = Mathf.Lerp(1f, 0.4f, steerIntensity); // Giảm tốc khi cua
        float targetSpeed = maxSpeed * steerFactor;

        // --- 2. Giảm tốc khi xe lệch hướng mục tiêu ---
        Vector3 targetDir = CalculateTargetDirection();
        float angleToTarget = Vector3.Angle(transform.forward, targetDir); // không dùng SignedAngle
        float angleFactor = Mathf.InverseLerp(0f, 120f, angleToTarget); // càng lệch thì giảm càng nhiều
        targetSpeed *= Mathf.Lerp(1f, 0.3f, angleFactor);

        // --- 3. Giảm tốc nếu gần waypoint ---
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (distanceToWaypoint < waypointReachDistance * 2f)
        {
            float slowFactor = Mathf.InverseLerp(waypointReachDistance * 2f, 0f, distanceToWaypoint);
            targetSpeed *= Mathf.Lerp(1f, 0.5f, slowFactor); // Giảm còn 50% nếu sát waypoint
        }

        // --- 4. Điều chỉnh tốc độ ---
        if (currentSpeed < targetSpeed)
        {
            currentSpeed += acceleration * Time.fixedDeltaTime;
        }
        else
        {
            currentSpeed -= brakeForce * Time.fixedDeltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // Log debug thêm
        if (Time.frameCount % 30 == 0)
        {
            //Debug.Log($"[AI] Speed: {currentSpeed:F1} | Steer: {currentSteerAngle:F1} | AngleToTarget: {angleToTarget:F1} | TargetSpeed: {targetSpeed:F1}");
        }
        // --- 4. Giảm tốc nếu xe AI khác ở gần phía trước ---
        if (IsAICarTooCloseAhead(out float nearest))
        {
            float avoidFactor = Mathf.InverseLerp(0f, avoidDistance, nearest);
            targetSpeed *= Mathf.Lerp(avoidSlowFactor, 1f, avoidFactor); // giảm còn avoidSlowFactor nếu rất gần
        }

    }



    void ApplyMovement()
    {
        // Áp dụng lực di chuyển về phía trước
        Vector3 forwardForce = transform.forward * currentSpeed;
        rb.velocity = new Vector3(forwardForce.x, rb.velocity.y, forwardForce.z);

        // Thay vì dùng torque, ta xoay trực tiếp bằng MoveRotation
        Quaternion steerRotation = Quaternion.Euler(0f, currentSteerAngle * Time.fixedDeltaTime * steerSpeed, 0f);
        rb.MoveRotation(rb.rotation * steerRotation);

        // Điều chỉnh lực cản để xe ổn định
        rb.angularDrag = 5f;
        rb.drag = 0.3f;
    }


    void CheckWaypoint()
    {
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);

        if (distanceToWaypoint < waypointReachDistance)
        {
            currentWaypointIndex++;

            // Xử lý khi đến waypoint cuối cùng
            if (currentWaypointIndex >= waypoints.Length)
            {
                if (loopWaypoints)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    currentWaypointIndex = waypoints.Length - 1;
                    currentSpeed = 0f;
                }
            }
        }
    }

    void UpdateWheelVisuals()
    {
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null) return;

        // Xoay bánh xe theo tốc độ
        float wheelRotation = (currentSpeed / maxSpeed) * wheelRotationSpeed * Time.fixedDeltaTime;
        frontLeftWheel.Rotate(wheelRotation, 0, 0);
        frontRightWheel.Rotate(wheelRotation, 0, 0);
        rearLeftWheel.Rotate(wheelRotation, 0, 0);
        rearRightWheel.Rotate(wheelRotation, 0, 0);

        // Bánh trước xoay theo góc lái
        frontLeftWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        frontRightWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
    }


    // Hàm debug để hiển thị waypoint trong Scene view
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Vẽ đường nối giữa các waypoint
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Vẽ waypoint
            Gizmos.color = (i == currentWaypointIndex) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(waypoints[i].position, waypointReachDistance);

            // Vẽ đường nối
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
            else if (loopWaypoints && i == waypoints.Length - 1 && waypoints[0] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }

        // Vẽ hướng xe đang đi
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
        }

    }
    bool IsAICarTooCloseAhead(out float nearestDistance)
    {
        nearestDistance = float.MaxValue;

        GameObject[] allAICars = GameObject.FindGameObjectsWithTag(aiTag);

        foreach (GameObject otherCar in allAICars)
        {
            if (otherCar == this.gameObject) continue;

            float distance = Vector3.Distance(transform.position, otherCar.transform.position);
            if (distance < avoidDistance)
            {
                Vector3 dirToOther = (otherCar.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToOther); // kiểm tra có ở phía trước

                if (dot > 0.5f) // chỉ tránh xe ở phía trước
                {
                    nearestDistance = distance;
                    return true;
                }
            }
        }

        return false;
    }

}