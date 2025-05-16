using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    public Transform car;
    public Vector3 offset = new Vector3(0, 2, -5);

    void LateUpdate()
    {
        if (car == null) return;

        // Di chuyển camera theo xe
        transform.position = car.TransformPoint(offset);

        // Lấy góc xoay của xe nhưng bỏ trục X và Z (chỉ giữ trục Y)
        Vector3 carEuler = car.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, carEuler.y, 0f);
    }
}
