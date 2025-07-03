using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string levelName = "Thanh_Pho2";
    // Tên màn chơi
    public Button button;            // Nút bấm màn chơi

    public GameObject lockIcon;      // Icon khóa hiển thị nếu màn bị khóa
    

    void Start()
    {
        if (!PlayerPrefs.HasKey(levelName))
        {
            // Nếu đây là màn đầu tiên (Thanh_Pho2), mở sẵn
            PlayerPrefs.SetInt(levelName, 1);
            PlayerPrefs.Save();
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