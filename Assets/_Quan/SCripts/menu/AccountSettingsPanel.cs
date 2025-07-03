using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;

public class AccountSettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform avatarGridPanel;
    public TMP_InputField displayNameInput;
    public Button saveButton;
    public Image avatarPreview;
    public TextMeshProUGUI displayNameText;
    //public TextMeshProUGUI Name;
    //public Image avatar;
    public GameObject accountSettingsPanel;


    [Header("Settings")]
    public Vector2 buttonSize = new Vector2(120, 120);
    public float buttonSpacing = 15f;
    public int columnsCount = 4;

    private string selectedAvatar = "";
    private bool isSaving = false;

    private List<string> avatarNames = new List<string> { "avatar1", "avatar2", "avatar3", "avatar4", "avatar5", "avatar6", "avatar7", "avatar8", "avatar9" };

    void OnEnable()
    {
        isSaving = false;
        saveButton.interactable = true;
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(OnSaveClicked);

        CreateAvatarButtons();
        LoadCurrentUserData();
    }

    public void OnSaveClicked()
    {
        if (isSaving) return;

        string newDisplayName = displayNameInput.text.Trim();
        if (string.IsNullOrEmpty(newDisplayName))
        {
            ShowErrorMessage("Tên hiển thị không được để trống!");
            return;
        }

        StartCoroutine(SaveUserDataCoroutine(newDisplayName));
    }

    IEnumerator SaveUserDataCoroutine(string newDisplayName)
    {
        isSaving = true;
        saveButton.interactable = false;

        bool nameCompleted = false;
        bool avatarCompleted = false;
        bool success = true;

        yield return StartCoroutine(SaveDisplayName(newDisplayName,
            (isSuccess) => { nameCompleted = true; success &= isSuccess; }));

        yield return StartCoroutine(SaveAvatar(
            (isSuccess) => { avatarCompleted = true; success &= isSuccess; }));

        yield return StartCoroutine(FinalizeSave(success));
    }

    IEnumerator SaveDisplayName(string newDisplayName, System.Action<bool> callback)
    {
        yield return new WaitForSeconds(0.5f);

        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newDisplayName };

        bool completed = false;
        bool success = false;

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result =>
            {
                Debug.Log("Tên cập nhật thành công!");
                if (displayNameText  != null)
                    displayNameText.text = newDisplayName;
                   // Name.text = newDisplayName;

                success = true;
                completed = true;
            },
            error =>
            {
                if (error.Error == PlayFabErrorCode.NameNotAvailable || error.HttpCode == 409)
                {
                    ShowErrorMessage("Tên hiển thị đã được sử dụng, vui lòng chọn tên khác.");
                }
                else
                {
                    ShowErrorMessage("Lỗi cập nhật tên: " + error.ErrorMessage);
                }

                Debug.LogError("Lỗi cập nhật tên: " + error.GenerateErrorReport());
                success = false;
                completed = true;
            });

        yield return new WaitUntil(() => completed);
        callback(success);
    }

    IEnumerator SaveAvatar(System.Action<bool> callback)
    {
        if (string.IsNullOrEmpty(selectedAvatar))
        {
            callback(true);
            yield break;
        }

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "avatar", selectedAvatar } }
        };

        bool completed = false;
        bool success = false;

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log("Avatar cập nhật thành công!");
                success = true;
                completed = true;
            },
            error =>
            {
                ShowErrorMessage("Lỗi cập nhật avatar: " + error.ErrorMessage);
                Debug.LogError("Lỗi avatar: " + error.GenerateErrorReport());
                success = false;
                completed = true;
            });

        yield return new WaitUntil(() => completed);
        callback(success);
    }

   IEnumerator FinalizeSave(bool success)
{
    yield return new WaitForSeconds(0.2f);
    isSaving = false;
    saveButton.interactable = true;

    if (success)
    {
        Debug.Log("Lưu thành công!");

        // Tự động cập nhật UI ngoài nếu tồn tại
        UserInfoDisplay infoDisplay = FindObjectOfType<UserInfoDisplay>();
        if (infoDisplay != null)
        {
            infoDisplay.RefreshUserInfo();
        }

        
    }
    else
    {
        Debug.LogWarning("Lưu thất bại!");
    }
}


    void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    void ShowErrorMessage(string message)
    {
        Debug.LogWarning(message);
        // Có thể thêm hệ thống UI hiển thị thông báo ở đây
    }

    void LoadCurrentUserData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {
                string name = result.AccountInfo?.TitleInfo?.DisplayName;
                if (!string.IsNullOrEmpty(name))
                {
                    displayNameInput.text = name;
                    if (displayNameText != null)
                        displayNameText.text = name;
                }
            },
            error => Debug.LogError("Lỗi tải tên: " + error.GenerateErrorReport())
        );

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("avatar"))
                {
                    OnAvatarSelected(result.Data["avatar"].Value);
                }
            },
            error => Debug.LogError("Lỗi tải avatar: " + error.GenerateErrorReport())
        );
    }

    void CreateAvatarButtons()
    {
        foreach (Transform child in avatarGridPanel)
        {
            Destroy(child.gameObject);
        }

        GridLayoutGroup grid = avatarGridPanel.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = avatarGridPanel.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = buttonSize;
        grid.spacing = new Vector2(buttonSpacing, buttonSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columnsCount;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.padding = new RectOffset(10, 10, 10, 10);

        foreach (string avatarName in avatarNames)
        {
            GameObject btnObj = new GameObject("AvatarButton_" + avatarName);
            btnObj.transform.SetParent(avatarGridPanel, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = buttonSize;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.95f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnAvatarSelected(avatarName));

            GameObject avatarImgObj = new GameObject("AvatarImage");
            avatarImgObj.transform.SetParent(btnObj.transform, false);

            Image avatarImg = avatarImgObj.AddComponent<Image>();
            avatarImg.sprite = Resources.Load<Sprite>("Avatars/" + avatarName);
            avatarImg.preserveAspect = true;

            RectTransform imgRect = avatarImgObj.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = new Vector2(8, 8);
            imgRect.offsetMax = new Vector2(-8, -8);
        }
        // Auto-size container width for 4 avatars + spacing + padding
        float totalWidth = (buttonSize.x * columnsCount) + (buttonSpacing * (columnsCount - 1)) + 20; // 20 là padding trái/phải
        avatarGridPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);

        // Căn giữa
        avatarGridPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

    }

    public void OnAvatarSelected(string avatarName)
    {
        selectedAvatar = avatarName;

        if (avatarPreview != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Avatars/" + avatarName);
            if (sprite != null)
            {
                avatarPreview.sprite = sprite;
                //avatar.sprite = sprite;
            }
        }

        foreach (Transform child in avatarGridPanel)
        {
            Image img = child.GetComponent<Image>();
            img.color = child.name == "AvatarButton_" + avatarName ? new Color(1f, 0.9f, 0.3f) : new Color(0.95f, 0.95f, 0.95f);
        }

        Debug.Log($"Avatar selected: {avatarName}");
    }
    public void ShowSettingsPanel()
    {
        if (accountSettingsPanel != null)
            accountSettingsPanel.SetActive(true);
    }
    public void HideSettingsPanel()
    {
        if (accountSettingsPanel != null)
            accountSettingsPanel.SetActive(false);
    }
}
