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
        StartCoroutine(DelayedSetup());
    }

    // Delay để đảm bảo UI đã được tạo
    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        FindUIReferences();
        SetupReferences();
    }

    // Tự động tìm lại UI references
    private void FindUIReferences()
    {
        // Tìm player và camera
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindObjectOfType<Camera>();
        }

        // Tìm UI elements
        if (waypointUI == null)
            waypointUI = GameObject.Find("WaypointUI");

        if (distanceText == null)
        {
            GameObject distObj = GameObject.Find("DistanceText");
            if (distObj != null) distanceText = distObj.GetComponent<TextMeshProUGUI>();
        }

        if (waypointIndicator == null)
        {
            GameObject indicatorObj = GameObject.Find("WaypointIndicator");
            if (indicatorObj != null) waypointIndicator = indicatorObj.GetComponent<RectTransform>();
        }
    }

    // Setup references sau khi tìm được
    private void SetupReferences()
    {
        // Lấy Image component từ waypoint indicator
        if (waypointIndicator != null && indicatorImage == null)
        {
            indicatorImage = waypointIndicator.GetComponent<Image>();
        }

        // Hiển thị lại UI nếu có waypoint đang active
        if (isWaypointActive && waypointUI != null)
        {
            waypointUI.SetActive(true);
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

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}