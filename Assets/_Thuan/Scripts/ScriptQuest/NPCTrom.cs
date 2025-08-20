using UnityEngine;

public class NPCWaypointCar : MonoBehaviour
{
    [HideInInspector] public Transform[] waypoints; // Gán từ code nhiệm vụ
    public float speed = 10f;
    public float turnSpeed = 50f;
    public float waypointReachDistance = 6f;
    public float wheelRadius = 0.33f;
    public float maxSteerAngle = 30f;

    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Stability Settings")]
    public float uprightForce = 5f;     // Lực kéo xe về thẳng đứng
    public float downForce = 500f;      // Lực ép xuống đất

    private int currentWaypointIndex = 0;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Điều khiển waypoint
        MoveTowardsWaypoint();

        // Giữ thăng bằng xe
        KeepCarUpright();

        // Ép xuống đất
        ApplyDownForce();
    }

    private void MoveTowardsWaypoint()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;

        // Quay mượt
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        // Tiến tới waypoint
        rb.MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);

        // Quay bánh
        RotateWheels();

        // Chuyển waypoint khi đến gần
        if (Vector3.Distance(transform.position, targetWaypoint.position) < waypointReachDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0;
        }
    }

    private void RotateWheels()
    {
        Vector3 localTarget = transform.InverseTransformPoint(waypoints[currentWaypointIndex].position);
        float steerAngle = Mathf.Clamp(Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg, -maxSteerAngle, maxSteerAngle);

        // Góc cua bánh trước
        Vector3 flEuler = frontLeftWheel.localEulerAngles;
        Vector3 frEuler = frontRightWheel.localEulerAngles;
        flEuler.y = steerAngle;
        frEuler.y = steerAngle;
        frontLeftWheel.localEulerAngles = flEuler;
        frontRightWheel.localEulerAngles = frEuler;

        // Quay bánh theo tốc độ
        float rotationSpeed = (speed / wheelRadius) * Time.fixedDeltaTime * Mathf.Rad2Deg;
        frontLeftWheel.Rotate(Vector3.right, rotationSpeed);
        frontRightWheel.Rotate(Vector3.right, rotationSpeed);
        rearLeftWheel.Rotate(Vector3.right, rotationSpeed);
        rearRightWheel.Rotate(Vector3.right, rotationSpeed);
    }

    private void KeepCarUpright()
    {
        // Dùng Quaternion để giữ xe thẳng đứng
        Quaternion uprightRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, uprightRotation, uprightForce * Time.fixedDeltaTime);
    }

    private void ApplyDownForce()
    {
        // Raycast xuống đất để kiểm tra xe có ở trên mặt đất không
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            rb.AddForce(-transform.up * downForce);
        }
    }
}
