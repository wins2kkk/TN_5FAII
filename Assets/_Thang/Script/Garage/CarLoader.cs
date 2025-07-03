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

            // 👉 Đảm bảo tất cả xe khác không có tag "Player"
            allCars[i].tag = "Untagged";
        }

        if (selectedIndex >= 0 && selectedIndex < allCars.Length)
        {
            //allCars[selectedIndex].SetActive(true);
            GameObject selectedCar = allCars[selectedIndex];
            selectedCar.SetActive(true);
            selectedCar.tag = "Player"; // 👉 Gán tag Player cho xe đang được chọn
        }
        else
        {
            //Debug.LogWarning("Invalid car index, activating first car as fallback.");
            //allCars[0].SetActive(true);
            Debug.LogWarning("Invalid car index, activating first car as fallback.");
            allCars[0].SetActive(true);
            allCars[0].tag = "Player"; // 👉 Gán tag fallback
        }
    }
}
