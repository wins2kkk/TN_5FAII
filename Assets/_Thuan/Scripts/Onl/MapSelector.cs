using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelector : MonoBehaviour
{
    public void LoadMap(string mapName)
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("SceneToLoad", mapName);
        SceneManager.LoadScene("Loading", LoadSceneMode.Single); // Single để xóa hết scene trước
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("SceneToLoad", "Menu");
        SceneManager.LoadScene("Loading", LoadSceneMode.Single);
    }

}
