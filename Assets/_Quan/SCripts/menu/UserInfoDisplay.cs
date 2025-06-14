using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;


public class UserInfoDisplay : MonoBehaviour
{
    public TextMeshProUGUI displayNameText;
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI emailText;

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
        string playerId = result.AccountInfo.PlayFabId;
        string email = result.AccountInfo.PrivateInfo?.Email ?? "Không có";

        // Hiển thị ra UI
        displayNameText.text = "Tên hiển thị: " + displayName;
        playerIdText.text = "Player ID: " + playerId;
        emailText.text = "Email: " + email;

        Debug.Log("Thông tin người chơi đã được tải.");
    }

    void OnGetAccountFailure(PlayFabError error)
    {
        Debug.LogError("Không lấy được thông tin tài khoản: " + error.GenerateErrorReport());
    }
}

