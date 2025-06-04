using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    [Header("Waypoint Settings")]
    public GameObject waypointPrefab;
    public Transform player;
    public Camera playerCamera;

    [Header("UI Elements")]
    public GameObject waypointUI;
    public TextMeshProUGUI distanceText;
    public RectTransform waypointIndicator;

    [Header("Indicator Images")]
    public Sprite onScreenSprite;  // Hình ảnh khi trong màn hình
    public Sprite offScreenSprite; // Hình ảnh khi ngoài rìa màn hình
    private Image indicatorImage;  // Component Image của indicator

    [Header("Settings")]
    public float arrivalDistance = 3f;
    public bool showDistance = true;

    // Current waypoint
    private GameObject currentWaypoint;
    private Vector3 currentTargetPosition;
    private bool isWaypointActive = false;
    private Action onReachedCallback;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Lấy Image component từ waypoint indicator
            if (waypointIndicator != null)
            {
                indicatorImage = waypointIndicator.GetComponent<Image>();
            }
        }
        else
        {
            Destroy(gameObject);
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

    // Tạo waypoint - Method chính bạn sẽ dùng
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

       // Debug.Log($"Waypoint created at {targetPosition}");
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

       // Debug.Log("Waypoint removed");
    }

    // Cập nhật UI
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
            // Trong tầm nhìn - đổi sang hình ảnh on-screen và không xoay
            ChangeIndicatorImage(true);
            waypointIndicator.position = screenPoint;
            waypointIndicator.rotation = Quaternion.identity; // Không xoay khi trong màn hình
        }
        else
        {
            // Ngoài tầm nhìn - đổi sang hình ảnh off-screen và xoay theo hướng
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
            // Trong màn hình
            if (onScreenSprite != null)
            {
                indicatorImage.sprite = onScreenSprite;
            }
        }
        else
        {
            // Ngoài rìa màn hình
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

        // Nếu waypoint ở phía sau camera, đảo ngược vị trí
        if (screenPoint.z < 0)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        Vector3 direction = (screenPoint - center).normalized;
        float margin = 50f;

        // Tính tọa độ trên rìa màn hình
        float x, y;

        // Tìm điểm giao với rìa màn hình
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX * Screen.height > absY * Screen.width)
        {
            // Chạm rìa trái hoặc phải
            x = direction.x > 0 ? Screen.width - margin : margin;
            y = center.y + direction.y * (x - center.x) / direction.x;
        }
        else
        {
            // Chạm rìa trên hoặc dưới
            y = direction.y > 0 ? Screen.height - margin : margin;
            x = center.x + direction.x * (y - center.y) / direction.y;
        }

        waypointIndicator.position = new Vector3(x, y, 0);

        // Xoay theo hướng waypoint
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

            // Gọi callback nếu có
            onReachedCallback?.Invoke();

            // Xóa waypoint
            RemoveWaypoint();
        }
    }

    // Kiểm tra có waypoint đang hoạt động không
    public bool HasActiveWaypoint()
    {
        return isWaypointActive;
    }

    // Lấy khoảng cách đến waypoint hiện tại
    public float GetDistanceToWaypoint()
    {
        if (!isWaypointActive || player == null) return -1f;
        return Vector3.Distance(player.position, currentTargetPosition);
    }

    // Method để thay đổi sprite từ bên ngoài
    public void SetIndicatorSprites(Sprite onScreen, Sprite offScreen)
    {
        onScreenSprite = onScreen;
        offScreenSprite = offScreen;
    }
}