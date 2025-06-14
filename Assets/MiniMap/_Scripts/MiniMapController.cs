using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Reflection;

[ExecuteInEditMode]
public class MiniMapController : MonoBehaviour
{
    [HideInInspector]
    public Transform shapeColliderGO;
    public RenderTexture renderTex;
    public Material mapMaterial;
    [HideInInspector]
    public List<MiniMapEntity> miniMapEntities;
    public GameObject iconPref;
    [HideInInspector]
    public Camera mapCamera;

    // THAY ĐỔI: Thêm hỗ trợ nhiều targets
    [Header("Target Settings")]
    [Tooltip("Current active target (xe hiện tại)")]
    public Transform currentTarget;

    [Tooltip("Danh sách tất cả các xe có thể theo dõi")]
    public List<Transform> allTargets = new List<Transform>();

    [Tooltip("Tự động tìm tất cả GameObjects có tag 'Player' hoặc 'Car'")]
    public bool autoFindTargets = true;

    [Tooltip("Tags để tự động tìm targets")]
    public string[] targetTags = { "Player", "Car" };

    // UI related variables
    [Header("UI Settings")]
    [Tooltip("Set which layers to show in the minimap")]
    public LayerMask minimapLayers;
    [Tooltip("Set this true, if you want minimap border as background of minimap")]
    public bool showBackground;
    [Tooltip("The mask to change the shape of minimap")]
    public Sprite miniMapMask;
    [Tooltip("border graphics of the minimap")]
    public Sprite miniMapBorder;
    [Tooltip("Set opacity of minimap")]
    [Range(0, 1)]
    public float miniMapOpacity = 1;
    [Tooltip("border graphics of the minimap")]
    public Vector3 miniMapScale = new Vector3(1, 1, 1);

    // Render camera related variables
    [Header("Camera Settings")]
    [Tooltip("Camera offset from the target")]
    public Vector3 cameraOffset = new Vector3(0f, 7.5f, 0f);
    [Tooltip("Camera's orthographic size")]
    public float camSize = 15;
    [Tooltip("Camera's far clip")]
    public float camFarClip = 1000;
    [Tooltip("Adjust the rotation according to your scene")]
    public Vector3 rotationOfCam = new Vector3(90, 0, 0);
    [Tooltip("If true the camera rotates according to the target")]
    public bool rotateWithTarget = true;

    [Header("Auto Update Settings")]
    [Tooltip("Tự động kiểm tra và cập nhật target")]
    public bool autoValidateTarget = true;

    [Tooltip("Thời gian kiểm tra target (giây)")]
    public float targetValidationInterval = 2f;

    [Tooltip("Tự động tìm target khi bắt đầu game")]
    public bool autoFindOnStart = true;

    [HideInInspector]
    public Dictionary<GameObject, GameObject> ownerIconMap = new Dictionary<GameObject, GameObject>();

    // Private variables
    private GameObject miniMapPanel;
    private Image mapPanelMask;
    private Image mapPanelBorder;
    private Image mapPanel;
    private Color mapColor;
    private Color mapBorderColor;
    private RectTransform mapPanelRect;
    private RectTransform mapPanelMaskRect;
    private Vector3 prevRotOfCam;
    Vector2 res;
    Image miniMapPanelImage;

    // THÊM: Flag để kiểm tra đã khởi tạo
    private bool isInitialized = false;
    private float lastTargetValidationTime;

    // THÊM: Tự động tìm target theo tag
    /// <summary>
    /// Tự động tìm target theo tag cụ thể
    /// </summary>
    /// <param name="tag">Tag cần tìm</param>
    public void SetTargetByTag(string tag)
    {
        GameObject targetObject = GameObject.FindGameObjectWithTag(tag);

        if (targetObject != null)
        {
            SetTarget(targetObject.transform);
            Debug.Log($"MiniMapController: Đã tìm thấy và set target '{targetObject.name}' với tag '{tag}'");
        }
        else
        {
            Debug.LogWarning($"MiniMapController: Không tìm thấy GameObject với tag '{tag}'");
        }
    }

    /// <summary>
    /// Tìm tất cả targets theo tag và chọn cái đầu tiên
    /// </summary>
    /// <param name="tag">Tag cần tìm</param>
    public void FindAllTargetsByTag(string tag)
    {
        GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(tag);

        if (targetObjects.Length > 0)
        {
            // Clear danh sách cũ
            allTargets.Clear();

            // Thêm tất cả targets tìm được
            foreach (GameObject obj in targetObjects)
            {
                allTargets.Add(obj.transform);
            }

            // Set target đầu tiên
            SetTarget(targetObjects[0].transform);
            Debug.Log($"MiniMapController: Tìm thấy {targetObjects.Length} targets với tag '{tag}', đã chọn '{targetObjects[0].name}'");
        }
        else
        {
            Debug.LogWarning($"MiniMapController: Không tìm thấy GameObject nào với tag '{tag}'");
        }
    }

    /// <summary>
    /// Tìm target gần nhất theo tag
    /// </summary>
    /// <param name="tag">Tag cần tìm</param>
    /// <param name="referencePosition">Vị trí tham chiếu (nếu null sẽ dùng camera position)</param>
    public void FindNearestTargetByTag(string tag, Vector3? referencePosition = null)
    {
        GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(tag);

        if (targetObjects.Length == 0)
        {
            Debug.LogWarning($"MiniMapController: Không tìm thấy GameObject nào với tag '{tag}'");
            return;
        }

        Vector3 refPos = referencePosition ?? mapCamera.transform.position;
        GameObject nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject obj in targetObjects)
        {
            float distance = Vector3.Distance(refPos, obj.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = obj;
            }
        }

        if (nearestTarget != null)
        {
            SetTarget(nearestTarget.transform);
            Debug.Log($"MiniMapController: Đã tìm thấy target gần nhất '{nearestTarget.name}' với tag '{tag}' (khoảng cách: {nearestDistance:F2})");
        }
    }

    // THÊM: Phương thức để thay đổi target
    /// <summary>
    /// Thay đổi target hiện tại bằng index
    /// </summary>
    /// <param name="targetIndex">Index trong danh sách allTargets</param>
    public void SetTarget(int targetIndex)
    {
        if (targetIndex >= 0 && targetIndex < allTargets.Count)
        {
            currentTarget = allTargets[targetIndex];
            Debug.Log($"Minimap target changed to: {currentTarget.name}");
        }
        else
        {
            Debug.LogWarning($"Target index {targetIndex} out of range!");
        }
    }

    /// <summary>
    /// Thay đổi target trực tiếp
    /// </summary>
    /// <param name="newTarget">Transform mới</param>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            currentTarget = newTarget;

            // Thêm vào danh sách nếu chưa có
            if (!allTargets.Contains(newTarget))
            {
                allTargets.Add(newTarget);
            }

            Debug.Log($"Minimap target changed to: {currentTarget.name}");
        }
        else
        {
            Debug.LogWarning("Cannot set null target!");
        }
    }

    /// <summary>
    /// Tìm target theo tên
    /// </summary>
    /// <param name="targetName">Tên của GameObject</param>
    public void SetTarget(string targetName)
    {
        Transform foundTarget = allTargets.Find(t => t.name == targetName);
        if (foundTarget != null)
        {
            SetTarget(foundTarget);
        }
        else
        {
            Debug.LogWarning($"Target with name '{targetName}' not found!");
        }
    }

    /// <summary>
    /// Tự động tìm tất cả targets theo tags
    /// </summary>
    public void FindAllTargets()
    {
        allTargets.Clear();

        foreach (string tag in targetTags)
        {
            GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in foundObjects)
            {
                if (obj != null && obj.transform != null && !allTargets.Contains(obj.transform))
                {
                    allTargets.Add(obj.transform);
                }
            }
        }

        Debug.Log($"MiniMapController: Tìm thấy {allTargets.Count} potential targets với tags: {string.Join(", ", targetTags)}");

        // Nếu chưa có currentTarget, set target đầu tiên
        if (currentTarget == null && allTargets.Count > 0)
        {
            currentTarget = allTargets[0];
            Debug.Log($"MiniMapController: Auto-set target to '{currentTarget.name}'");
        }
    }

    /// <summary>
    /// Tự động kiểm tra và cập nhật target nếu target hiện tại bị mất
    /// </summary>
    public void ValidateCurrentTarget()
    {
        // Kiểm tra target hiện tại còn hợp lệ không
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            Debug.Log("MiniMapController: Target hiện tại không hợp lệ, đang tìm target mới...");

            // Thử tìm target khác trong allTargets
            for (int i = 0; i < allTargets.Count; i++)
            {
                if (allTargets[i] != null && allTargets[i].gameObject.activeInHierarchy)
                {
                    currentTarget = allTargets[i];
                    Debug.Log($"MiniMapController: Đã chuyển sang target '{currentTarget.name}'");
                    return;
                }
            }

            // Nếu không tìm được target nào hợp lệ, tìm lại toàn bộ
            FindAllTargets();
        }
    }

    /// <summary>
    /// Chuyển sang target tiếp theo trong danh sách
    /// </summary>
    public void NextTarget()
    {
        if (allTargets.Count == 0) return;

        int currentIndex = allTargets.IndexOf(currentTarget);
        int nextIndex = (currentIndex + 1) % allTargets.Count;
        SetTarget(nextIndex);
    }

    /// <summary>
    /// Chuyển sang target trước đó trong danh sách
    /// </summary>
    public void PreviousTarget()
    {
        if (allTargets.Count == 0) return;

        int currentIndex = allTargets.IndexOf(currentTarget);
        int prevIndex = currentIndex <= 0 ? allTargets.Count - 1 : currentIndex - 1;
        SetTarget(prevIndex);
    }

    // SỬA: Thêm validation cho initialization
    private bool ValidateComponents()
    {
        // Kiểm tra UI components
        Mask maskComponent = transform.GetComponentInChildren<Mask>();
        if (maskComponent == null)
        {
            Debug.LogError("MiniMapController: Không tìm thấy Mask component trong children!");
            return false;
        }

        GameObject maskPanelGO = maskComponent.gameObject;
        if (maskPanelGO == null)
        {
            Debug.LogError("MiniMapController: Mask GameObject null!");
            return false;
        }

        mapPanelMask = maskPanelGO.GetComponent<Image>();
        if (mapPanelMask == null)
        {
            Debug.LogError("MiniMapController: Không tìm thấy Image component trên Mask GameObject!");
            return false;
        }

        if (maskPanelGO.transform.parent == null)
        {
            Debug.LogError("MiniMapController: Mask GameObject không có parent!");
            return false;
        }

        mapPanelBorder = maskPanelGO.transform.parent.GetComponent<Image>();
        if (mapPanelBorder == null)
        {
            Debug.LogError("MiniMapController: Không tìm thấy Border Image component!");
            return false;
        }

        if (maskPanelGO.transform.childCount == 0)
        {
            Debug.LogError("MiniMapController: Mask GameObject không có children!");
            return false;
        }

        miniMapPanel = maskPanelGO.transform.GetChild(0).gameObject;
        if (miniMapPanel == null)
        {
            Debug.LogError("MiniMapController: Không tìm thấy MiniMapPanel!");
            return false;
        }

        mapPanel = miniMapPanel.GetComponent<Image>();
        if (mapPanel == null)
        {
            Debug.LogError("MiniMapController: Không tìm thấy MapPanel Image component!");
            return false;
        }

        // Kiểm tra camera
        if (mapCamera == null)
        {
            mapCamera = transform.GetComponentInChildren<Camera>();
            if (mapCamera == null)
            {
                Debug.LogError("MiniMapController: Không tìm thấy Camera component!");
                return false;
            }
        }

        // Kiểm tra RenderTexture
        if (renderTex == null)
        {
            Debug.LogError("MiniMapController: RenderTexture chưa được assign!");
            return false;
        }

        // Kiểm tra Material
        if (mapMaterial == null)
        {
            Debug.LogError("MiniMapController: Map Material chưa được assign!");
            return false;
        }

        return true;
    }

    // Initialize everything here
    public void OnEnable()
    {
        // Tự động tìm targets nếu được bật
        if (autoFindTargets || autoFindOnStart)
        {
            FindAllTargets();
        }

        // Đảm bảo currentTarget được set
        if (currentTarget == null && allTargets.Count > 0)
        {
            currentTarget = allTargets[0];
            Debug.Log($"MiniMapController: Auto-assigned target to '{currentTarget.name}'");
        }

        // SỬA: Thêm validation trước khi khởi tạo
        if (!ValidateComponents())
        {
            isInitialized = false;
            return;
        }

        ownerIconMap.Clear();

        mapColor = mapPanel.color;
        mapBorderColor = mapPanelBorder.color;

        mapCamera.cullingMask = minimapLayers;

        mapPanelMaskRect = mapPanelMask.GetComponent<RectTransform>();
        mapPanelRect = miniMapPanel.GetComponent<RectTransform>();

        if (mapPanelMaskRect != null && mapPanelRect != null)
        {
            mapPanelRect.anchoredPosition = mapPanelMaskRect.anchoredPosition;
        }

        res = new Vector2(Screen.width, Screen.height);

        miniMapPanelImage = miniMapPanel.GetComponent<Image>();
        if (miniMapPanelImage != null)
        {
            miniMapPanelImage.enabled = !showBackground;
        }

        SetupRenderTexture();
        isInitialized = true;
        lastTargetValidationTime = Time.time;
    }

    void OnDisable()
    {
        if (renderTex != null && renderTex.IsCreated())
        {
            renderTex.Release();
        }
    }

    void OnDestroy()
    {
        if (renderTex != null && renderTex.IsCreated())
        {
            renderTex.Release();
        }
    }

    public void LateUpdate()
    {
        // SỬA: Kiểm tra đã khởi tạo chưa
        if (!isInitialized)
        {
            return;
        }

        // THÊM: Auto validate target
        if (autoValidateTarget && Time.time - lastTargetValidationTime > targetValidationInterval)
        {
            ValidateCurrentTarget();
            lastTargetValidationTime = Time.time;
        }

        // SỬA: Thêm null checks
        if (mapPanelMask == null || mapPanelBorder == null || mapPanel == null ||
            mapPanelMaskRect == null || mapPanelRect == null || miniMapPanelImage == null)
        {
            Debug.LogWarning("MiniMapController: Một số UI components bị null, thử khởi tạo lại...");
            OnEnable();
            return;
        }

        // Set minimap images and colors
        if (miniMapMask != null) mapPanelMask.sprite = miniMapMask;
        if (miniMapBorder != null) mapPanelBorder.sprite = miniMapBorder;

        mapPanelBorder.rectTransform.localScale = miniMapScale;
        mapBorderColor.a = miniMapOpacity;
        mapColor.a = miniMapOpacity;
        mapPanelBorder.color = mapBorderColor;
        mapPanel.color = mapColor;

        // Set minimappanel size and position
        mapPanelMaskRect.sizeDelta = new Vector2(Mathf.RoundToInt(mapPanelMaskRect.sizeDelta.x), Mathf.RoundToInt(mapPanelMaskRect.sizeDelta.y));
        mapPanelRect.position = mapPanelMaskRect.position;
        mapPanelRect.sizeDelta = mapPanelMaskRect.sizeDelta;
        miniMapPanelImage.enabled = !showBackground;

        if (Screen.width != res.x || Screen.height != res.y)
        {
            SetupRenderTexture();
            res.x = Screen.width;
            res.y = Screen.height;
        }

        SetCam();
    }

    void SetupRenderTexture()
    {
        // SỬA: Thêm null checks
        if (renderTex == null || mapMaterial == null || mapCamera == null || mapPanelRect == null)
        {
            Debug.LogWarning("MiniMapController: Thiếu components để setup RenderTexture");
            return;
        }

        if (renderTex.IsCreated()) renderTex.Release();

        int width = Mathf.Max(1, (int)mapPanelRect.sizeDelta.x);
        int height = Mathf.Max(1, (int)mapPanelRect.sizeDelta.y);

        renderTex = new RenderTexture(width, height, 24);
        renderTex.Create();

        mapMaterial.mainTexture = renderTex;
        mapCamera.targetTexture = renderTex;

        if (mapPanelMaskRect != null)
        {
            mapPanelMaskRect.gameObject.SetActive(false);
            mapPanelMaskRect.gameObject.SetActive(true);
        }
    }

    void SetCam()
    {
        // SỬA: Thêm null check cho camera
        if (mapCamera == null)
        {
            Debug.LogWarning("MiniMapController: mapCamera is null!");
            return;
        }

        mapCamera.orthographicSize = camSize;
        mapCamera.farClipPlane = camFarClip;

        // SỬA: Sử dụng currentTarget thay vì target
        if (currentTarget == null)
        {
#if UNITY_EDITOR
            Debug.Log("Please assign the current target");
#endif
        }
        else
        {
            mapCamera.transform.eulerAngles = rotationOfCam;

            if (rotateWithTarget)
            {
                mapCamera.transform.eulerAngles = currentTarget.eulerAngles + rotationOfCam;
            }
            mapCamera.transform.position = currentTarget.position + cameraOffset;
        }
    }

    public MapObject RegisterMapObject(GameObject owner, MiniMapEntity mme)
    {
        // SỬA: Thêm null checks
        if (iconPref == null)
        {
            Debug.LogError("MiniMapController: iconPref is null!");
            return null;
        }

        if (miniMapPanel == null)
        {
            Debug.LogError("MiniMapController: miniMapPanel is null!");
            return null;
        }

        GameObject curMGO = Instantiate(iconPref);
        MapObject curMO = curMGO.AddComponent<MapObject>();
        curMO.SetMiniMapEntityValues(this, mme, owner, mapCamera, miniMapPanel);
        ownerIconMap.Add(owner, curMGO);
        return owner.GetComponent<MapObject>();
    }

    public void UnregisterMapObject(MapObject mmo, GameObject owner)
    {
        if (ownerIconMap.ContainsKey(owner))
        {
            Destroy(ownerIconMap[owner]);
            ownerIconMap.Remove(owner);
        }

        if (mmo != null)
        {
            Destroy(mmo);
        }
    }
}