using UnityEngine;

public class CarCamera : MonoBehaviour
{
    public float followDistance = 5f; // Khoảng cách từ camera đến vị trí trung bình
    public float height = 2f; // Chiều cao camera so với vị trí trung bình
    public float rotationDamping = 3f; // Tốc độ xoay mượt của camera
    public float avoidDistance = 2f; // Khoảng cách tối thiểu để tránh vật cản
    public LayerMask obstacleLayer; // Layer của các vật cản

    private Vector3 targetPosition; // Vị trí mục tiêu của camera
    private Quaternion targetRotation; // Góc quay mục tiêu của camera

    void LateUpdate()
    {
        // Tìm tất cả các GameObject có Tag "Player"
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects == null || playerObjects.Length == 0) return;

        // Chuyển đổi thành mảng Transform
        Transform[] cars = new Transform[playerObjects.Length];
        for (int i = 0; i < playerObjects.Length; i++)
        {
            cars[i] = playerObjects[i].transform;
        }

        // Tính trung bình vị trí của các xe
        Vector3 averagePosition = CalculateAveragePosition(cars);
        Vector3 averageForward = CalculateAverageForward(cars);

        // Tính vị trí lý tưởng của camera
        Vector3 idealPosition = averagePosition - (averageForward * followDistance) + (Vector3.up * height);
        targetPosition = idealPosition;

        // Kiểm tra vật cản bằng raycast
        AvoidObstacles(ref targetPosition);

        // Di chuyển camera mượt mà đến vị trí mục tiêu
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * rotationDamping);

        // Xoay camera theo hướng trung bình của xe
        float currentAngle = transform.eulerAngles.y;
        float desiredAngle = Mathf.Atan2(averageForward.x, averageForward.z) * Mathf.Rad2Deg;
        float angle = Mathf.LerpAngle(currentAngle, desiredAngle, Time.deltaTime * rotationDamping);
        targetRotation = Quaternion.Euler(0, angle, 0);
        transform.rotation = targetRotation;
    }

    Vector3 CalculateAveragePosition(Transform[] cars)
    {
        Vector3 sum = Vector3.zero;
        foreach (Transform car in cars)
        {
            if (car != null) sum += car.position;
        }
        return sum / cars.Length;
    }

    Vector3 CalculateAverageForward(Transform[] cars)
    {
        Vector3 sum = Vector3.zero;
        int validCars = 0;
        foreach (Transform car in cars)
        {
            if (car != null)
            {
                sum += car.forward;
                validCars++;
            }
        }
        return validCars > 0 ? sum / validCars : Vector3.forward;
    }

    void AvoidObstacles(ref Vector3 targetPos)
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects == null || playerObjects.Length == 0) return;

        Transform[] cars = new Transform[playerObjects.Length];
        for (int i = 0; i < playerObjects.Length; i++)
        {
            cars[i] = playerObjects[i].transform;
        }

        Vector3 directionToCamera = (targetPos - CalculateAveragePosition(cars)).normalized;
        float distanceToCamera = Vector3.Distance(CalculateAveragePosition(cars), targetPos);

        RaycastHit hit;
        if (Physics.Raycast(CalculateAveragePosition(cars), directionToCamera, out hit, distanceToCamera, obstacleLayer))
        {
            float distanceToObstacle = hit.distance;
            if (distanceToObstacle < avoidDistance)
            {
                targetPos = CalculateAveragePosition(cars) + (directionToCamera * (distanceToObstacle - 0.5f));
            }
        }
    }

    void OnDrawGizmos()
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects != null && playerObjects.Length > 0)
        {
            Transform[] cars = new Transform[playerObjects.Length];
            for (int i = 0; i < playerObjects.Length; i++)
            {
                cars[i] = playerObjects[i].transform;
            }

            Gizmos.color = Color.red;
            Vector3 averagePosition = CalculateAveragePosition(cars);
            Gizmos.DrawLine(averagePosition, targetPosition);
        }
    }
}