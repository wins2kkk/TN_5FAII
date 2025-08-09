using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragImage : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public bool canDrag = true;
    public bool constrainToParent = true;

    [Header("Custom Move Limits (optional)")]
    public bool useCustomLimits = false;
    public float leftLimit = -200f;
    public float rightLimit = 200f;
    public float topLimit = 150f;
    public float bottomLimit = -150f;

    [Header("Optional: Drag Bounds")]
    public RectTransform boundsRect; // Nếu muốn giới hạn vùng kéo

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Camera uiCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalPosition = rectTransform.anchoredPosition;

        uiCamera = canvas.worldCamera;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        Vector2 moveVector;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            uiCamera,
            out Vector2 currentPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position - eventData.delta,
            uiCamera,
            out Vector2 previousPos);

        moveVector = currentPos - previousPos;

        // Chỉ thay đổi X, bỏ Y
        Vector2 newPos = rectTransform.anchoredPosition + new Vector2(moveVector.x, 0);

        // Giới hạn: không cho qua phải hơn vị trí ban đầu
        float minX = -1060f; // Giới hạn sang trái (tùy bạn chỉnh)
        float maxX = originalPosition.x; // Vị trí ban đầu là giới hạn phải

        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);

        // Giữ nguyên Y
        newPos.y = originalPosition.y;

        rectTransform.anchoredPosition = newPos;
    }





    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
    }

 

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
    }

    public void SetDragEnabled(bool enabled)
    {
        canDrag = enabled;
    }
}
