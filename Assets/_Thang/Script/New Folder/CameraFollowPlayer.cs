using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Tính vị trí mới theo hướng quay của xe
        Vector3 desiredPos = target.TransformPoint(offset);

        // Di chuyển camera mượt
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);

        // Xoay camera nhìn theo xe
        transform.LookAt(target);
    }

}
