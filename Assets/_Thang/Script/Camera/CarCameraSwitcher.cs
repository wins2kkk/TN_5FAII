using UnityEngine;
using UnityEngine.UI;

public class CarCameraSwitcher : MonoBehaviour
{
    public Camera followCamera;          // Camera bên ngoài
    private Camera cockpitCamera;        // Camera trong buồng lái (gán sau)
    public Button switchButton;

    private bool isCockpit = false;

    void Start()
    {
        // Auto gán nếu chưa gán sẵn trong Editor
        if (followCamera == null)
            followCamera = GameObject.Find("FollowCamera").GetComponent<Camera>();

        // Tắt cả 2 camera để setup lại rõ ràng
        if (followCamera != null) followCamera.enabled = false;
        if (cockpitCamera != null) cockpitCamera.enabled = false;

        // Luôn bật camera ngoài khi bắt đầu
        ActivateFollowCamera();

        if (switchButton != null)
            switchButton.onClick.AddListener(SwitchCamera);
    }

    public void SetCockpitCamera(Camera newCamera)
    {
        cockpitCamera = newCamera;

        // Đảm bảo luôn tắt khi gán ban đầu
        if (cockpitCamera != null)
            cockpitCamera.enabled = false;
    }

    public void SwitchCamera()
    {
        isCockpit = !isCockpit;

        if (isCockpit)
            ActivateCockpitCamera();
        else
            ActivateFollowCamera();
    }

    private void ActivateFollowCamera()
    {
        if (followCamera != null)
            followCamera.enabled = true;

        if (cockpitCamera != null)
            cockpitCamera.enabled = false;
    }

    private void ActivateCockpitCamera()
    {
        if (followCamera != null)
            followCamera.enabled = false;

        if (cockpitCamera != null)
            cockpitCamera.enabled = true;
    }
}
