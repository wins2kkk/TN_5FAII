using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using System;

public class UserInfor : MonoBehaviour
{
    public TextMeshProUGUI displayNameText;
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI emailText;
  

    public Image avatarImage;
    public static string displayNameCached = "Người chơi";

    // Event để thông báo khi tên đã sẵn sàng
    public static event Action<string> OnDisplayNameReady;

    void Start()
    {
        GetAccountInfoFromPlayFab();
    }

    void GetAccountInfoFromPlayFab()
    {
        var request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess, OnGetAccountFailure);
    }

    void OnGetAccountSuccess(GetAccountInfoResult result)
    {
        string displayName = result.AccountInfo.TitleInfo.DisplayName ?? "Chưa đặt";
        displayNameCached = displayName;
        OnDisplayNameReady?.Invoke(displayName);

        string playerId = result.AccountInfo.PlayFabId;
        string email = result.AccountInfo.PrivateInfo?.Email ?? "Không có";

        displayNameText.text = displayName;
        playerIdText.text = "ID: " + playerId;
        emailText.text = "Email: " + email;

        Sprite avatarSprite = Resources.Load<Sprite>("Avatars/avatar1");
        if (avatarSprite != null)
        {
            avatarImage.sprite = avatarSprite;
        }

        GetAvatarFromUserData();
        

        Debug.Log("Thông tin người chơi đã được tải.");
    }


    void GetAvatarFromUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("avatar"))
            {
                string avatarName = result.Data["avatar"].Value;
                Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + avatarName);
                if (avatarSprite != null)
                    avatarImage.sprite = avatarSprite;
            }
        },
        error => Debug.LogWarning("Không thể lấy avatar từ PlayFab: " + error.GenerateErrorReport()));
    }

    void OnGetAccountFailure(PlayFabError error)
    {
        Debug.LogError("Không lấy được thông tin tài khoản: " + error.GenerateErrorReport());
    }

    public void RefreshUserInfo()
    {
        GetAccountInfoFromPlayFab(); // gọi lại như khi Start()
    }
    

}