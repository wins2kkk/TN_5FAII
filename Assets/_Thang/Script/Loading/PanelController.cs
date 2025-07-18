using UnityEngine;

public class PanelController : MonoBehaviour
{
    public GameObject panel; // Kéo panel cần điều khiển vào đây trong Inspector

    // Hàm để hiện panel
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    // Hàm để ẩn panel
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
