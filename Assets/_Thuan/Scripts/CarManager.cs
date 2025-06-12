//using UnityEngine;

//public class CarManager : MonoBehaviour
//{
//    public static CarManager Instance { get; private set; }

//    [Header("Car Setup")]
//    public GameObject[] availableCars; // Assign all car prefabs here

//    private GameObject currentActiveCar;
//    private int selectedCarIndex;

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//            InitializeCar();
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }

//    private void InitializeCar()
//    {
//        // Get selected car index from PlayerPrefs
//        selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);

//        // Spawn the selected car
//        SpawnSelectedCar();
//    }

//    private void SpawnSelectedCar()
//    {
//        // Destroy previous car if exists
//        if (currentActiveCar != null)
//        {
//            Destroy(currentActiveCar);
//        }

//        // Spawn new car
//        if (availableCars != null && selectedCarIndex < availableCars.Length)
//        {
//            currentActiveCar = Instantiate(availableCars[selectedCarIndex]);
//            Debug.Log($"Spawned car: {currentActiveCar.name}");

//            // Notify all systems about the new car
//            NotifyCarChanged();
//        }
//    }

//    public Transform GetActiveCarTransform()
//    {
//        return currentActiveCar?.transform;
//    }

//    public GameObject GetActiveCar()
//    {
//        return currentActiveCar;
//    }

//    private void NotifyCarChanged()
//    {
//        // Find and update ParkingMission
//        ParkingMission[] parkingMissions = FindObjectsOfType<ParkingMission>();
//        foreach (var mission in parkingMissions)
//        {
//            mission.UpdateCarReference(currentActiveCar.transform);
//        }

//        // You can add other systems here that need to know about car changes
//    }

//    // Call this when car selection changes (if needed during gameplay)
//    public void SwitchCar(int newCarIndex)
//    {
//        if (newCarIndex >= 0 && newCarIndex < availableCars.Length)
//        {
//            selectedCarIndex = newCarIndex;
//            PlayerPrefs.SetInt("SelectedCarIndex", selectedCarIndex);
//            PlayerPrefs.Save();
//            SpawnSelectedCar();
//        }
//    }
//}