using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{
    public GameObject[] allPanels;
    public static menu Instance; // Singleton pattern
    
    private void Awake()
    {
        // Ki?m tra xem ð? có instance nào t?n t?i chýa
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Gi? GameObject này qua các scene
        }
        else
        {
            Destroy(gameObject); // H?y duplicate
        }
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Ðý?c g?i khi scene m?i ðý?c t?i
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // T?m l?i các panel trong scene m?i (n?u c?n)
        RefreshPanelReferences();
    }
    
    // C?p nh?t l?i references ð?n các panel
    private void RefreshPanelReferences()
    {
        // N?u b?n c?n t?m l?i các panel trong scene m?i
        // Uncomment d?ng dý?i n?u c?n thi?t
        // allPanels = GameObject.FindGameObjectsWithTag("Panel");
    }

    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != null) // Ki?m tra null ð? tránh l?i
                panel.SetActive(false);
        }
        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    public void HideAllPanels()
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    public void TogglePanel(GameObject panelToToggle)
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != panelToToggle && panel != null)
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
            Debug.LogWarning("Tên scene không h?p l?!");
        }
    }
}