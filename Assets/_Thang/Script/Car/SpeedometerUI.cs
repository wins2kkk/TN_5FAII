using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    public Car_script carController;         // Tham chiếu tới xe
    public GameObject needle;                // Kim chỉ tốc độ
    private float startRotation = 220f;
    private float endRotation = -49f;

    private float desiredPosition;
    public float speedMultiplier = 1f;       // Tỉ lệ scale tốc độ

    void Update()
    {
        // Nếu chưa có xe gắn, tự tìm xe có tag "Player"
        if (carController == null)
        {
            GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
            if (playerCar != null)
                carController = playerCar.GetComponent<Car_script>();
        }

        if (carController == null || needle == null)
            return;

        float speed = carController.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
        UpdateNeedle(speed);
    }


    private float currentRotationZ = 0f; // Góc kim hiện tại

    void UpdateNeedle(float speed)
    {
        desiredPosition = startRotation - endRotation;
        float speedPercent = Mathf.Clamp01(speed / 180f);  // 180 là KPH tối đa bạn muốn hiển thị
        float targetRotationZ = startRotation - speedPercent * desiredPosition;

        // Mượt hóa bằng Lerp
        currentRotationZ = Mathf.Lerp(currentRotationZ, targetRotationZ, Time.deltaTime * 5f); // số 5f là tốc độ trơn

        needle.transform.eulerAngles = new Vector3(0, 0, currentRotationZ);
    }

}
