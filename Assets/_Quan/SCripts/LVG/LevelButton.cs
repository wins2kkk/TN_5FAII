using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string levelName;         // Tên màn chơi
    public Button button;            // Nút bấm màn chơi
    public GameObject lockIcon;      // Icon khóa hiển thị nếu màn bị khóa

    void Start()
    {
        // Mở khóa màn đầu tiên nếu chưa có dữ liệu
        if (!PlayerPrefs.HasKey(levelName))
        {
            PlayerPrefs.SetInt(levelName, levelName == "Login" ? 1 : 0);
        }

        int isUnlocked = PlayerPrefs.GetInt(levelName);

        button.interactable = isUnlocked == 1;         // Bật tắt nút
        lockIcon.SetActive(isUnlocked == 0);           // Hiển thị icon khóa nếu chưa mở

        button.onClick.AddListener(() => LoadLevel());
    }

    void LoadLevel()
    {
        if (PlayerPrefs.GetInt(levelName) == 1)
        {
            SceneManager.LoadScene(levelName);
        }
    }
    public void UnlockNextLevel(string nextLevelName)
    {
        Debug.Log($"🔵 Unlocking next level: {nextLevelName}");
        PlayerPrefs.SetInt(nextLevelName, 1);
        PlayerPrefs.Save();
        Debug.Log($"🟢 {nextLevelName} is now unlocked! Value: {PlayerPrefs.GetInt(nextLevelName)}");
    }


}