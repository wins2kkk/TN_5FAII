using UnityEngine;
using System.Collections.Generic;

public class AICarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float maxSpeed = 15f;
    public float acceleration = 10f;
    public float brakeForce = 20f;
    public float maxSteerAngle = 45f;
    public float steerSpeed = 8f;

    [Header("Wheel References")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Visual Settings")]
    public float wheelRotationSpeed = 360f;

    [Header("AI Avoidance Settings")]
    public string aiTag = "AI";
    public float avoidDistance = 6f;
    public float avoidSlowFactor = 0.5f;

    private Rigidbody rb;
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private float currentSpeed = 0f;
    private float currentSteerAngle = 0f;
    private float targetSteerAngle = 0f;
    private float waypointReachDistance = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody missing!");
            return;
        }

        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        // 🆕 Lấy waypoint từ RacerProgress nếu có
        var progress = GetComponent<RacerProgressWaypoint>();
        if (progress != null && progress.trackWaypoints != null && progress.trackWaypoints.Length > 0)
        {
            waypoints = progress.trackWaypoints;
            waypointReachDistance = progress.waypointReachDistance;
        }
        else
        {
            Debug.LogError("AI missing RacerProgressWaypoint or waypoints!");  
        }
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetDirection = CalculateTargetDirection();
        CalculateSteerInput(targetDirection);
        CalculateSpeedInput();
        ApplyMovement();
        CheckWaypoint();
        UpdateWheelVisuals();
    }

    Vector3 CalculateTargetDirection()
    {
        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist < waypointReachDistance * 1.5f && waypoints.Length > 1)
        {
            int nextIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Vector3 nextPos = waypoints[nextIndex].position;
            float blend = 1f - (dist / (waypointReachDistance * 1.5f));
            targetPos = Vector3.Lerp(targetPos, nextPos, blend * 0.5f);
        }

        return (targetPos - transform.position).normalized;
    }

    void CalculateSteerInput(Vector3 dir)
    {
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        float steerInput = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);
        steerInput = Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), 0.7f);
        targetSteerAngle = steerInput * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSpeed * 2f * Time.fixedDeltaTime);
    }

    void CalculateSpeedInput()
    {
        float steerIntensity = Mathf.Pow(Mathf.Abs(currentSteerAngle) / maxSteerAngle, 1.5f);
        float steerFactor = Mathf.Lerp(1f, 0.4f, steerIntensity);
        float targetSpeed = maxSpeed * steerFactor;

        Vector3 targetDir = CalculateTargetDirection();
        float angle = Vector3.Angle(transform.forward, targetDir);
        float angleFactor = Mathf.InverseLerp(0f, 120f, angle);
        targetSpeed *= Mathf.Lerp(1f, 0.3f, angleFactor);

        float dist = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (dist < waypointReachDistance * 2f)
        {
            float slowFactor = Mathf.InverseLerp(waypointReachDistance * 2f, 0f, dist);
            targetSpeed *= Mathf.Lerp(1f, 0.5f, slowFactor);
        }

        if (currentSpeed < targetSpeed)
            currentSpeed += acceleration * Time.fixedDeltaTime;
        else
            currentSpeed -= brakeForce * Time.fixedDeltaTime;

        if (IsAICarTooCloseAhead(out float nearest))
        {
            float avoidFactor = Mathf.InverseLerp(0f, avoidDistance, nearest);
            targetSpeed *= Mathf.Lerp(avoidSlowFactor, 1f, avoidFactor);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }

    void ApplyMovement()
    {
        Vector3 force = transform.forward * currentSpeed;
        rb.velocity = new Vector3(force.x, rb.velocity.y, force.z);
        Quaternion steerRot = Quaternion.Euler(0f, currentSteerAngle * Time.fixedDeltaTime * steerSpeed, 0f);
        rb.MoveRotation(rb.rotation * steerRot);
        rb.angularDrag = 5f;
        rb.drag = 0.3f;
    }

    void CheckWaypoint()
    {
        float dist = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (dist < waypointReachDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0;
        }
    }

    void UpdateWheelVisuals()
    {
        if (!frontLeftWheel || !frontRightWheel || !rearLeftWheel || !rearRightWheel) return;

        float rotation = (currentSpeed / maxSpeed) * wheelRotationSpeed * Time.fixedDeltaTime;
        frontLeftWheel.Rotate(rotation, 0, 0);
        frontRightWheel.Rotate(rotation, 0, 0);
        rearLeftWheel.Rotate(rotation, 0, 0);
        rearRightWheel.Rotate(rotation, 0, 0);

        frontLeftWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        frontRightWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
    }

    bool IsAICarTooCloseAhead(out float nearestDistance)
    {
        nearestDistance = float.MaxValue;
        GameObject[] allAICars = GameObject.FindGameObjectsWithTag(aiTag);
        foreach (GameObject other in allAICars)
        {
            if (other == gameObject) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < avoidDistance)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, dir) > 0.5f)
                {
                    nearestDistance = dist;
                    return true;
                }
            }
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (!waypoints[i]) continue;
            Gizmos.color = (i == currentWaypointIndex) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(waypoints[i].position, waypointReachDistance);
            if (i < waypoints.Length - 1 && waypoints[i + 1])
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
        }
    }
}
