using UnityEngine;
using UnityEngine.EventSystems;

public class ShowImageOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Image to Show on Hover")]
    public GameObject hoverImage;

    void Start()
    {
        if (hoverImage != null)
            hoverImage.SetActive(false); // Ẩn ban đầu
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverImage != null)
            hoverImage.SetActive(true); // Hiện khi rê chuột vào
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverImage != null)
            hoverImage.SetActive(false); // Ẩn khi chuột rời ra
    }
}
