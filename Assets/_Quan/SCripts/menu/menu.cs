using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMenuManager : MonoBehaviour
{
    public GameObject[] allPanels;

    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(false);
        }

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    public void HideAllPanels()
    {
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(false);
        }
    }

    public void TogglePanel(GameObject panelToToggle)
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != panelToToggle)
                panel.SetActive(false);
        }

        if (panelToToggle != null)
            panelToToggle.SetActive(!panelToToggle.activeSelf);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Tên scene không hợp lệ!");
        }
    }
}