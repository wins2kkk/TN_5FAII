using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class LeaderboardPanel : MonoBehaviour
{
    public GameObject entryPrefab;         // Prefab UI từng entry (nếu bạn muốn Instantiate)
    public LeaderboardEntryUI[] leaderboardEntries; // Gán sẵn 10 entry trong Inspector

    public Sprite defaultAvatar;           // Avatar mặc định nếu user chưa chọn

    void Start()
    {
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "BXH",
            StartPosition = 0,
            MaxResultsCount = leaderboardEntries.Length
        },
        result =>
        {
            // Ẩn tất cả entry trước
            for (int i = 0; i < leaderboardEntries.Length; i++)
            {
                leaderboardEntries[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < result.Leaderboard.Count && i < leaderboardEntries.Length; i++)
            {
                var item = result.Leaderboard[i];
                LeaderboardEntryUI entryUI = leaderboardEntries[i];
                entryUI.gameObject.SetActive(true);

                string displayName = item.DisplayName ?? "Người chơi";
                int cup = item.StatValue;

                LoadAvatar(item.PlayFabId, (avatarSprite) =>
                {
                    entryUI.SetEntry(avatarSprite ?? defaultAvatar, displayName, cup);
                });
            }
        },
        error =>
        {
            Debug.LogError("Không lấy được BXH: " + error.GenerateErrorReport());
        });
    }



    // Lấy avatar từ PlayFab userData (key = "avatar")
    void LoadAvatar(string playFabId, System.Action<Sprite> callback)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = playFabId
        },
        result =>
        {
            string avatarName = "avatar1"; // Mặc định

            if (result.Data != null && result.Data.ContainsKey("avatar"))
            {
                avatarName = result.Data["avatar"].Value;
            }

            Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + avatarName);
            callback?.Invoke(avatarSprite);
        },
        error =>
        {
            Debug.LogWarning("Không thể load avatar: " + error.GenerateErrorReport());
            callback?.Invoke(null);
        });
    }
}
