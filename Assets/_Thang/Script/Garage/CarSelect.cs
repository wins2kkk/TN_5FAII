using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSelect : MonoBehaviour
{
    public GameObject allCarsContainer;
    private GameObject[] allCars;
    private int currentIndex = 0;
    public Car_shop carSelectionManager; // Tham chiếu đến CarSelectionManager
    public GameObject confirmationImage; // Hình ảnh hiển thị khi xác nhận chọn xe

    void Start()
    {
        if (allCarsContainer == null)
        {
            Debug.LogError("allCarsContainer is not assigned!");
            return;
        }

        allCars = new GameObject[allCarsContainer.transform.childCount];
        if (allCars.Length == 0)
        {
            Debug.LogError("No cars found under allCarsContainer!");
            return;
        }

        for (int i = 0; i < allCarsContainer.transform.childCount; i++)
        {
            allCars[i] = allCarsContainer.transform.GetChild(i).gameObject;
            allCars[i].SetActive(false);
        }

        if (PlayerPrefs.HasKey("SelectedCarIndex"))
        {
            currentIndex = PlayerPrefs.GetInt("SelectedCarIndex");
            if (currentIndex >= allCars.Length)
            {
                currentIndex = 0; // Reset nếu chỉ số vượt quá số lượng xe
                PlayerPrefs.SetInt("SelectedCarIndex", currentIndex);
                PlayerPrefs.Save();
            }
        }

        ShowCurrentCar();
        if (carSelectionManager != null)
            carSelectionManager.UpdateUI();
        Debug.Log("Current Car Index: " + currentIndex); // Kiểm tra xe hiện tại
    }

    void ShowCurrentCar()
    {
        if (allCars == null || allCars.Length == 0) return;

        foreach (GameObject car in allCars)
        {
            if (car != null) car.SetActive(false);
        }

        if (currentIndex >= 0 && currentIndex < allCars.Length && allCars[currentIndex] != null)
        {
            allCars[currentIndex].SetActive(true);
        }
    }

    public void NextCar()
    {
        if (allCars == null || allCars.Length == 0) return;

        currentIndex = (currentIndex + 1) % allCars.Length;
        ShowCurrentCar();
        if (carSelectionManager != null)
            carSelectionManager.OnCarChanged();
        Debug.Log("Moved to Car Index: " + currentIndex); // Kiểm tra khi chuyển tiếp
    }

    public void PreviousCar()
    {
        if (allCars == null || allCars.Length == 0) return;

        currentIndex = (currentIndex - 1 + allCars.Length) % allCars.Length;
        ShowCurrentCar();
        if (carSelectionManager != null)
            carSelectionManager.OnCarChanged();
        Debug.Log("Moved to Car Index: " + currentIndex); // Kiểm tra khi lùi
    }

    public void OnYesButtonClick()
    {
        if (allCars == null || allCars.Length == 0) return;

        PlayerPrefs.SetInt("SelectedCarIndex", currentIndex);
        PlayerPrefs.Save();

        // Hiển thị hình ảnh xác nhận
        if (confirmationImage != null)
        {
            confirmationImage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Confirmation Image is not assigned!");
        }

        Debug.Log("Selected Car Saved: " + currentIndex);
    }

    // Phương thức để lấy chỉ số xe hiện tại
    public int GetCurrentCarIndex()
    {
        return currentIndex;
    }
}