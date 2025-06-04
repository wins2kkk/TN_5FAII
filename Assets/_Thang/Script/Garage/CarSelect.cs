using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarSelect : MonoBehaviour
{
    public GameObject allCarsContainer;
    private GameObject[] allCars;
    private int currentIndex = 0;

    void Start()
    {
        allCars = new GameObject[allCarsContainer.transform.childCount];

        for (int i = 0; i < allCarsContainer.transform.childCount; i++)
        {
            allCars[i] = allCarsContainer.transform.GetChild(i).gameObject;
            allCars[i].SetActive(false);
        }

        if (PlayerPrefs.HasKey("SelectedCarIndex"))
        {
            currentIndex = PlayerPrefs.GetInt("SelectedCarIndex");
        }

        ShowCurrentCar();
    }

    void ShowCurrentCar()
    {
        foreach (GameObject car in allCars)
        {
            car.SetActive(false);
        }

        allCars[currentIndex].SetActive(true);
    }


    public void NextCar()
    {
        currentIndex = (currentIndex + 1) % allCars.Length;
        ShowCurrentCar();
    }

    public void PreviousCar()
    {
        currentIndex = (currentIndex - 1 + allCars.Length) % allCars.Length;
        ShowCurrentCar();
    }

    public void OnYesButtonClick(string sceneName)
    {
        PlayerPrefs.SetInt("SelectedCarIndex", currentIndex);
        PlayerPrefs.Save();

        Debug.Log("Selected Car Saved");

        SceneManager.LoadScene(sceneName);
    }

}
