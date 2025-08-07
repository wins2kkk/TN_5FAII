using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarLoader : MonoBehaviour
{
    public GameObject allCarsContainer;
    private GameObject[] allCars;

    void Start()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0); // mặc định là 0 nếu chưa chọn

        allCars = new GameObject[allCarsContainer.transform.childCount];

        for (int i = 0; i < allCars.Length; i++)
        {
            allCars[i] = allCarsContainer.transform.GetChild(i).gameObject;
            allCars[i].SetActive(false);
            allCars[i].tag = "Untagged";
        }

        GameObject selectedCar;

        if (selectedIndex >= 0 && selectedIndex < allCars.Length)
        {
            selectedCar = allCars[selectedIndex];
        }
        else
        {
            Debug.LogWarning("Invalid car index, activating first car as fallback.");
            selectedCar = allCars[0];
        }

        selectedCar.SetActive(true);
        selectedCar.tag = "Player";

        // 👉 GÁN CAMERA TRONG BUỒNG LÁI CHO SWITCHER (nếu có)
        Camera cockpitCam = selectedCar.GetComponentInChildren<Camera>(true); // Tìm camera đang tắt
        if (cockpitCam != null)
        {
            cockpitCam.enabled = false; // Tắt khi mới vào game

            CarCameraSwitcher cameraSwitcher = FindObjectOfType<CarCameraSwitcher>();
            if (cameraSwitcher != null)
            {
                cameraSwitcher.SetCockpitCamera(cockpitCam);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy CarCameraSwitcher trong scene.");
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy camera trong xe của prefab.");
        }
    }

}
