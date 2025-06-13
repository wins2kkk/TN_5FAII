using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class menu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject gameUIPanel;

    public void OnStartButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
    }
    public void HideGameUIPanel()
    {
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(false); // Ẩn panel
        }
    }
}
