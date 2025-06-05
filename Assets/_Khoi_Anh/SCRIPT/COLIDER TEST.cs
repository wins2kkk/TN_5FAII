using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class COLIDERTEST : MonoBehaviour
{
    private Camera mainCamera;
    private float yOffset; // giữ nguyên tọa độ Y của Cube
    private bool isDragging = false;

    void Start()
    {
        mainCamera = Camera.main;
        yOffset = transform.position.y;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Bắt đầu kéo
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0)) // Thả chuột
        {
            isDragging = false;
        }

        if (isDragging)
        {
            MoveCubeWithMouse();
        }
    }

    void MoveCubeWithMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, yOffset, 0)); // mặt phẳng XZ ở độ cao yOffset

        float enter;
        if (groundPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.position = new Vector3(hitPoint.x, yOffset, hitPoint.z);
        }
    }
}
