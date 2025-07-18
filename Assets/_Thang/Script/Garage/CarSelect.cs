using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // THÊM DÒNG NÀY để dùng Slider

public class CarSelect : MonoBehaviour
{
    [System.Serializable]
    public class CarStats
    {
        [Range(0, 1)] public float speed;
        [Range(0, 1)] public float engine;
        [Range(0, 1)] public float steer;
    }

    public CarStats[] carStatsArray = new CarStats[7];

    public GameObject allCarsContainer;
    private GameObject[] allCars;
    private int currentIndex = 0;
    public Car_shop carSelectionManager;
    public GameObject confirmationImage;

    // ✅ THÊM 3 thanh slider này:
    public Slider speedSlider;
    public Slider engineSlider;
    public Slider steerSlider;

    // ✅ UI Elements cho việc chọn xe - THÊM DÒNG NÀY
    public GameObject selectButton;  // Nút "Chọn"

    ///UI slider Thong tin Xe   
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
        Debug.Log("Start - Current Car Index: " + currentIndex); // Kiểm tra xe hiện tại
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
        UpdateStatSliders(); // cập nhật thanh slider
        UpdateSelectionUI(); // cập nhật UI chọn xe

    }

    // ✅ THÊM: Cập nhật UI dựa trên xe đã được chọn hay chưa
    void UpdateSelectionUI()
    {
        // Kiểm tra xe hiện tại có phải xe đã lưu không
        int savedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", -1);
        bool isCurrentCarSelected = (savedCarIndex == currentIndex);

        Debug.Log("UpdateSelectionUI - Current Index: " + currentIndex + ", Saved Index: " + savedCarIndex + ", Is Selected: " + isCurrentCarSelected);

        // Hiện/ẩn nút chọn
        if (selectButton != null)
        {
            selectButton.SetActive(!isCurrentCarSelected);
            Debug.Log("Select Button Active: " + selectButton.activeSelf);
        }
        else
        {
            Debug.LogWarning("selectButton is NULL! Please assign it in Inspector.");
        }
    }

    void UpdateStatSliders()
    {
        if (carStatsArray == null || currentIndex >= carStatsArray.Length) return;

        CarStats stats = carStatsArray[currentIndex];

        if (speedSlider != null) speedSlider.value = stats.speed;
        if (engineSlider != null) engineSlider.value = stats.engine;
        if (steerSlider != null) steerSlider.value = stats.steer;
    }

    public void NextCar()
    {
        if (allCars == null || allCars.Length == 0) return;

        currentIndex = (currentIndex + 1) % allCars.Length;
        ShowCurrentCar();
        if (carSelectionManager != null)
            carSelectionManager.OnCarChanged();
        Debug.Log("NextCar - Moved to Car Index: " + currentIndex);
    }

    public void PreviousCar()
    {
        if (allCars == null || allCars.Length == 0) return;

        currentIndex = (currentIndex - 1 + allCars.Length) % allCars.Length;
        ShowCurrentCar();
        if (carSelectionManager != null)
            carSelectionManager.OnCarChanged();
        Debug.Log("PreviousCar - Moved to Car Index: " + currentIndex);
    }

    // ✅ PHIÊN BẢN MỚI: Chỉ lưu xe và ẩn nút chọn
    public void OnYesButtonClick()
    {
        Debug.Log("=== OnYesButtonClick ĐƯỢC GỌI ===");

        if (allCars == null || allCars.Length == 0)
        {
            Debug.LogError("allCars is null or empty!");
            return;
        }

        Debug.Log("Trước khi lưu - Current Index: " + currentIndex);

        // Lưu xe đã chọn
        PlayerPrefs.SetInt("SelectedCarIndex", currentIndex);
        PlayerPrefs.Save();

        Debug.Log("=== XE ĐÃ ĐƯỢC LƯU ===");
        Debug.Log("Current Car Index được lưu: " + currentIndex);
        Debug.Log("Tên xe: " + (allCars[currentIndex] != null ? allCars[currentIndex].name : "null"));
        Debug.Log("PlayerPrefs SelectedCarIndex sau khi lưu: " + PlayerPrefs.GetInt("SelectedCarIndex"));
        Debug.Log("=====================");

        // Ẩn nút "Chọn"
        if (selectButton != null)
        {
            selectButton.SetActive(false);
            Debug.Log("Đã ẩn nút Select Button");
        }
        else
        {
            Debug.LogWarning("selectButton is NULL!");
        }

        // Ẩn confirmation panel nếu có
        if (confirmationImage != null)
        {
            confirmationImage.SetActive(false);
            Debug.Log("Đã ẩn confirmation panel");
        }

        // Cập nhật UI trong car selection manager
        if (carSelectionManager != null)
            carSelectionManager.UpdateUI();

        // Cập nhật lại UI selection
        UpdateSelectionUI();
    }

    // Phương thức để lấy chỉ số xe hiện tại
    public int GetCurrentCarIndex()
    {
        return currentIndex;
    }
}