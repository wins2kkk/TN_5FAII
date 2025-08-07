using UnityEngine;

public class CockpitCamera : MonoBehaviour
{
    public Transform carTransform;
    public Vector3 localPositionOffset;
    public Vector3 rotationOffset;
    public float rotationSmoothSpeed = 5f;

    private bool foundCar = false;

    void LateUpdate()
    {
        if (!foundCar || carTransform == null)
        {
            // Tìm xe có tag "Player" sau khi xe được bật lên
            GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
            if (playerCar != null)
            {
                carTransform = playerCar.transform;
                foundCar = true;
            }
            else
            {
                return; // Chưa tìm được xe, thoát
            }
        }

        transform.position = carTransform.TransformPoint(localPositionOffset);

        Quaternion targetRotation = carTransform.rotation * Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }
}
