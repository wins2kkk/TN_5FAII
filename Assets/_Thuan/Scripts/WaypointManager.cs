using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    [Header("Waypoint Settings")]
    public GameObject waypointPrefab;
    public Transform player;
    public Camera playerCamera;

    [Header("UI Elements - Sẽ được tự động tìm lại")]
    public GameObject waypointUI;
    public TextMeshProUGUI distanceText;
    public RectTransform waypointIndicator;

    [Header("Indicator Images")]
    public Sprite onScreenSprite;
    public Sprite offScreenSprite;
    private Image indicatorImage;

    [Header("Settings")]
    public float arrivalDistance = 3f;
    public bool showDistance = true;

    [Header("Debug Info")]
    public bool debugMode = true;

    // Current waypoint - Data được giữ lại
    private GameObject currentWaypoint;
    private Vector3 currentTargetPosition;
    private bool isWaypointActive = false;
    private Action onReachedCallback;

    private void Awake()
    {
        // Singleton pattern với DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đăng ký event khi scene load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        SetupReferences();
    }

    // Được gọi mỗi khi load scene mới
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode) Debug.Log($"Scene loaded: {scene.name}");
        StartCoroutine(DelayedSetup());
    }

    // Delay để đảm bảo UI đã được tạo
    private IEnumerator DelayedSetup()
    {
        // Chờ lâu hơn để UI được tạo hoàn toàn
        yield return new WaitForSeconds(0.1f);

        // Thử tìm nhiều lần nếu cần
        int attempts = 0;
        int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            FindUIReferences();

            // Nếu tìm được đủ UI elements thì dừng
            if (waypointUI != null && distanceText != null && waypointIndicator != null)
            {
                if (debugMode) Debug.Log($"Found all UI elements after {attempts + 1} attempts");
                break;
            }

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Could not find all UI elements after maximum attempts");
            LogMissingUIElements();
        }

        SetupReferences();
    }

    // Tự động tìm lại UI references với nhiều phương pháp
    private void FindUIReferences()
    {
        // Tìm player và camera
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                if (debugMode) Debug.Log("Found Player: " + playerObj.name);
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindObjectOfType<Camera>();

            if (playerCamera != null && debugMode)
                Debug.Log("Found Camera: " + playerCamera.name);
        }

        // Tìm UI elements với nhiều cách khác nhau
        FindWaypointUI();
        FindDistanceText();
        FindWaypointIndicator();
    }

    private void FindWaypointUI()
    {
        if (waypointUI != null) return;

        // Thử tìm theo tên
        waypointUI = GameObject.Find("WaypointUI");

        // Nếu không tìm được, thử tìm trong Canvas
        if (waypointUI == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                // Tìm trực tiếp trong canvas
                Transform found = canvas.transform.Find("WaypointUI");
                if (found != null)
                {
                    waypointUI = found.gameObject;
                    break;
                }

                // Tìm đệ quy trong tất cả children
                waypointUI = FindChildByName(canvas.transform, "WaypointUI");
                if (waypointUI != null) break;
            }
        }

        // Nếu vẫn không tìm được, tìm theo pattern tên
        if (waypointUI == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.scene.isLoaded && obj.name.ToLower().Contains("waypoint") &&
                    obj.name.ToLower().Contains("ui"))
                {
                    waypointUI = obj;
                    break;
                }
            }
        }

        if (waypointUI != null && debugMode)
            Debug.Log("Found WaypointUI: " + waypointUI.name);
    }

    private void FindDistanceText()
    {
        if (distanceText != null) return;

        // Thử tìm theo tên
        GameObject distObj = GameObject.Find("DistanceText");
        if (distObj != null)
        {
            distanceText = distObj.GetComponent<TextMeshProUGUI>();
        }

        // Nếu không tìm được, tìm trong waypointUI
        if (distanceText == null && waypointUI != null)
        {
            distanceText = waypointUI.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Nếu vẫn không có, tìm trong tất cả Canvas
        if (distanceText == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                GameObject found = FindChildByName(canvas.transform, "DistanceText");
                if (found != null)
                {
                    distanceText = found.GetComponent<TextMeshProUGUI>();
                    if (distanceText != null) break;
                }
            }
        }

        // Tìm theo pattern tên
        if (distanceText == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.name.ToLower().Contains("distance"))
                {
                    distanceText = text;
                    break;
                }
            }
        }

        if (distanceText != null && debugMode)
            Debug.Log("Found DistanceText: " + distanceText.name);
    }

    private void FindWaypointIndicator()
    {
        if (waypointIndicator != null) return;

        // Thử tìm theo tên
        GameObject indicatorObj = GameObject.Find("WaypointIndicator");
        if (indicatorObj != null)
        {
            waypointIndicator = indicatorObj.GetComponent<RectTransform>();
        }

        // Nếu không tìm được, tìm trong waypointUI
        if (waypointIndicator == null && waypointUI != null)
        {
            Transform found = waypointUI.transform.Find("WaypointIndicator");
            if (found != null)
            {
                waypointIndicator = found.GetComponent<RectTransform>();
            }
        }

        // Tìm theo component Image có sprite phù hợp
        if (waypointIndicator == null)
        {
            Image[] allImages = FindObjectsOfType<Image>();
            foreach (Image img in allImages)
            {
                if (img.name.Contains("Waypoint") || img.name.Contains("Indicator"))
                {
                    waypointIndicator = img.GetComponent<RectTransform>();
                    break;
                }
            }
        }

        if (waypointIndicator != null && debugMode)
            Debug.Log("Found WaypointIndicator: " + waypointIndicator.name);
    }

    // Helper method để tìm child theo tên đệ quy
    private GameObject FindChildByName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child.gameObject;
            }

            GameObject found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private void LogMissingUIElements()
    {
        if (waypointUI == null) Debug.LogError("WaypointUI not found!");
        if (distanceText == null) Debug.LogError("DistanceText not found!");
        if (waypointIndicator == null) Debug.LogError("WaypointIndicator not found!");

        // Log tất cả UI objects trong scene để debug
        Debug.Log("=== ALL UI OBJECTS IN SCENE ===");
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            LogChildrenRecursive(canvas.transform, 1);
        }
    }

    private void LogChildrenRecursive(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Debug.Log($"{indent}- {child.name} ({child.GetType().Name})");
            if (child.childCount > 0)
            {
                LogChildrenRecursive(child, depth + 1);
            }
        }
    }

    // Setup references sau khi tìm được
    private void SetupReferences()
    {
        // Lấy Image component từ waypoint indicator
        if (waypointIndicator != null && indicatorImage == null)
        {
            indicatorImage = waypointIndicator.GetComponent<Image>();
            if (indicatorImage != null && debugMode)
                Debug.Log("Found indicator image component");
        }

        // Hiển thị lại UI nếu có waypoint đang active
        if (isWaypointActive && waypointUI != null)
        {
            waypointUI.SetActive(true);
            if (debugMode) Debug.Log("Reactivated waypoint UI");
        }
    }

    private void Update()
    {
        if (isWaypointActive)
        {
            UpdateWaypointUI();
            CheckArrivalDistance();
        }
    }

    // Tạo waypoint - Method chính
    public GameObject CreatePointer(Vector3 targetPosition, Action onReached = null)
    {
        // Xóa waypoint cũ nếu có
        RemoveWaypoint();

        // Lưu thông tin
        currentTargetPosition = targetPosition;
        onReachedCallback = onReached;
        isWaypointActive = true;

        // Tạo waypoint 3D trong thế giới
        if (waypointPrefab != null)
        {
            currentWaypoint = Instantiate(waypointPrefab, targetPosition, Quaternion.identity);
        }

        // Hiển thị UI
        if (waypointUI != null)
        {
            waypointUI.SetActive(true);
            if (debugMode) Debug.Log("Activated waypoint UI");
        }
        else
        {
            Debug.LogWarning("WaypointUI is null! Trying to find it again...");
            StartCoroutine(DelayedSetup());
        }

        return currentWaypoint;
    }

    // Overload với label
    public GameObject CreatePointer(Vector3 targetPosition, string label, Action onReached = null)
    {
        GameObject waypoint = CreatePointer(targetPosition, onReached);

        // Set label nếu có TextMesh
        if (waypoint != null)
        {
            TextMesh textMesh = waypoint.GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = label;
            }
        }

        return waypoint;
    }

    // Xóa waypoint
    public void RemoveWaypoint()
    {
        if (currentWaypoint != null)
        {
            Destroy(currentWaypoint);
            currentWaypoint = null;
        }

        if (waypointUI != null)
        {
            waypointUI.SetActive(false);
        }

        isWaypointActive = false;
        onReachedCallback = null;
    }

    // Cập nhật UI với null checks
    private void UpdateWaypointUI()
    {
        if (player == null || playerCamera == null) return;

        // Tính khoảng cách
        float distance = Vector3.Distance(player.position, currentTargetPosition);

        // Hiển thị khoảng cách
        if (showDistance && distanceText != null)
        {
            distanceText.text = $"{distance:F1}m";
        }

        // Cập nhật indicator
        UpdateScreenIndicator();
    }

    // Cập nhật indicator trên màn hình
    private void UpdateScreenIndicator()
    {
        if (waypointIndicator == null || playerCamera == null) return;

        Vector3 screenPoint = playerCamera.WorldToScreenPoint(currentTargetPosition);

        // Kiểm tra trong tầm nhìn
        bool isVisible = screenPoint.z > 0 &&
                        screenPoint.x > 0 && screenPoint.x < Screen.width &&
                        screenPoint.y > 0 && screenPoint.y < Screen.height;

        if (isVisible)
        {
            // Trong tầm nhìn
            ChangeIndicatorImage(true);
            waypointIndicator.position = screenPoint;
            waypointIndicator.rotation = Quaternion.identity;
        }
        else
        {
            // Ngoài tầm nhìn
            ChangeIndicatorImage(false);
            ShowOffScreenIndicator(screenPoint);
        }
    }

    // Thay đổi hình ảnh indicator
    private void ChangeIndicatorImage(bool isOnScreen)
    {
        if (indicatorImage == null) return;

        if (isOnScreen)
        {
            if (onScreenSprite != null)
            {
                indicatorImage.sprite = onScreenSprite;
            }
        }
        else
        {
            if (offScreenSprite != null)
            {
                indicatorImage.sprite = offScreenSprite;
            }
        }
    }

    // Hiển thị indicator ngoài màn hình
    private void ShowOffScreenIndicator(Vector3 screenPoint)
    {
        Vector3 center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

        if (screenPoint.z < 0)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        Vector3 direction = (screenPoint - center).normalized;
        float margin = 50f;

        float x, y;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX * Screen.height > absY * Screen.width)
        {
            x = direction.x > 0 ? Screen.width - margin : margin;
            y = center.y + direction.y * (x - center.x) / direction.x;
        }
        else
        {
            y = direction.y > 0 ? Screen.height - margin : margin;
            x = center.x + direction.x * (y - center.y) / direction.y;
        }

        waypointIndicator.position = new Vector3(x, y, 0);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        waypointIndicator.rotation = Quaternion.AngleAxis(angle + 90f, Vector3.forward);
    }

    // Kiểm tra đã đến nơi chưa
    private void CheckArrivalDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, currentTargetPosition);

        if (distance <= arrivalDistance)
        {
            Debug.Log("Arrived at waypoint!");

            onReachedCallback?.Invoke();
            RemoveWaypoint();
        }
    }

    // Utility methods
    public bool HasActiveWaypoint()
    {
        return isWaypointActive;
    }

    public float GetDistanceToWaypoint()
    {
        if (!isWaypointActive || player == null) return -1f;
        return Vector3.Distance(player.position, currentTargetPosition);
    }

    public void SetIndicatorSprites(Sprite onScreen, Sprite offScreen)
    {
        onScreenSprite = onScreen;
        offScreenSprite = offScreen;
    }

    // Manual refresh UI - có thể gọi từ ngoài
    public void RefreshUIReferences()
    {
        StartCoroutine(DelayedSetup());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}