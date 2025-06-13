using UnityEngine;

public class CarRotation : MonoBehaviour
{
    public float rotationSpeed = 10f;
    private float mouseX;

    void Update()
    {
        // Tự quay nếu không di chuyển chuột
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // Xoay bằng chuột khi người dùng kéo
        if (Input.GetMouseButton(0)) // Nút chuột trái
        {
            mouseX = Input.GetAxis("Mouse X") * rotationSpeed * 2f;
            transform.Rotate(0, -mouseX, 0);
        }
    }
}