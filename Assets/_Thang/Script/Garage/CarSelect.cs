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

    ///UI slider Thong tin Xe   
    void Start()
    {
        // Nếu bạn muốn set tạm bằng code:
        //carStatsArray = new CarStats[7]
        //{
        //new CarStats { speed = 0.5f, engine = 0.6f, steer = 0.4f },
        //new CarStats { speed = 0.6f, engine = 0.7f, steer = 0.3f },
        //new CarStats { speed = 0.7f, engine = 0.8f, steer = 0.5f },
        //new CarStats { speed = 0.8f, engine = 0.9f, steer = 0.6f },
        //new CarStats { speed = 0.9f, engine = 1.0f, steer = 0.7f },
        //new CarStats { speed = 0.4f, engine = 0.5f, steer = 0.2f },
        //new CarStats { speed = 1.0f, engine = 0.8f, steer = 0.9f },
        //};
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
        UpdateStatSliders(); // cập nhật thanh slider
    }
    void UpdateStatSliders()
    {
        if (carStatsArray == null || currentIndex >= carStatsArray.Length) return;

        CarStats stats = carStatsArray[currentIndex];

        speedSlider.value = stats.speed;
        engineSlider.value = stats.engine;
        steerSlider.value = stats.steer;
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

    public void OnYesButtonClick(string sceneName)
    {
        if (allCars == null || allCars.Length == 0) return;

        PlayerPrefs.SetInt("SelectedCarIndex", currentIndex);
        PlayerPrefs.Save();

        Debug.Log("Selected Car Saved: " + currentIndex);

        // Chuyển scene nếu tên hợp lệ
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is null or empty!");
        }
    }


    // Phương thức để lấy chỉ số xe hiện tại
    public int GetCurrentCarIndex()
    {
        return currentIndex;
    }
   
}