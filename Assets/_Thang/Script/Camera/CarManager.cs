using UnityEngine;

public class CarManager : MonoBehaviour
{
    public GameObject[] Cars;
    public CarCameraSwitcher cameraSwitcher;

    private int currentCarIndex = -1;

    void Start()
    {
        for (int i = 0; i < Cars.Length; i++)
        {
            if (Cars[i].activeInHierarchy)
            {
                SetCurrentCar(i);
                break;
            }
        }
    }

    public void SwitchToCar(int indexToActivate)
    {
        for (int i = 0; i < Cars.Length; i++)
        {
            Cars[i].SetActive(i == indexToActivate);
        }

        SetCurrentCar(indexToActivate);
    }

    public void SetCurrentCar(int index)
    {
        if (index < 0 || index >= Cars.Length) return;

        currentCarIndex = index;

        // Tìm camera trong xe (child camera của xe đó)
        Camera cockpitCam = Cars[index].GetComponentInChildren<Camera>(true);

        if (cockpitCam != null && cameraSwitcher != null)
        {
            cameraSwitcher.SetCockpitCamera(cockpitCam);
        }
    }
}
