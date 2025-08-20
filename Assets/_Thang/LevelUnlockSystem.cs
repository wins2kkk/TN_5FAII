using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;

public class LevelUnlockSystem : MonoBehaviour
{
    public Button[] levelButtons;
    private int maxUnlockedLevel = 1;

    void Start()
    {
        LoadMaxUnlockedLevelFromPlayFab();
    }

    private void LoadMaxUnlockedLevelFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("MaxUnlockedLevel"))
                    maxUnlockedLevel = Convert.ToInt32(result.Data["MaxUnlockedLevel"].Value);
                else
                    maxUnlockedLevel = 1;
                UpdateLevelButtons();
            },
            error =>
            {
                Debug.LogError("❌ Load Error: " + error.GenerateErrorReport());
                maxUnlockedLevel = 1;
                UpdateLevelButtons();
            });
    }

    void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i + 1;
            levelButtons[i].gameObject.SetActive(levelIndex <= maxUnlockedLevel);
        }
    }

    public void UnlockNextLevel(int completedLevel)
    {
        // SỬA: Chỉ unlock nếu level mới cao hơn maxUnlockedLevel hiện tại
        int newUnlockedLevel = completedLevel + 1;
        if (newUnlockedLevel > maxUnlockedLevel && completedLevel < levelButtons.Length)
        {
            maxUnlockedLevel = newUnlockedLevel;
            var request = new UpdateUserDataRequest
            {
                Data = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "MaxUnlockedLevel", maxUnlockedLevel.ToString() }
                }
            };
            PlayFabClientAPI.UpdateUserData(request,
                result => Debug.Log($"✅ Unlocked Level {maxUnlockedLevel} on PlayFab"),
                error => Debug.LogError("❌ Save Error: " + error.GenerateErrorReport()));
            UpdateLevelButtons();
        }
    }

    public void OnLevelButtonClicked(int level)
    {
        string sceneName = "Level_" + level;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}