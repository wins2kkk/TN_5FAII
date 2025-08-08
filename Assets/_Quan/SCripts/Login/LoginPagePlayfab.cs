using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine.SceneManagement;

public class LoginPagePlayfab : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI TopText;
    [SerializeField] TextMeshProUGUI MessageText;

    [Header("Login")]
    [SerializeField] TMP_InputField EmailLoginInput;
    [SerializeField] TMP_InputField PasswordLoginInput;
    [SerializeField] GameObject LoginPage;

    [Header("Register")]
    [SerializeField] TMP_InputField UserNameRegisterInput;
    [SerializeField] TMP_InputField EmailRegisterInput;
    [SerializeField] TMP_InputField PassworRegisterInput;
    [SerializeField] GameObject ResgisterPage;

    [Header("Recovery")]
    [SerializeField] TMP_InputField EmailRecoveryInput;
    [SerializeField] GameObject RecoveryPage;

    [SerializeField]
    private GameObject WelcomeObject;

    [SerializeField]
    private Text WelcomeText;

    [SerializeField]
    private GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    #region Buttom Fuctions
    public void RegisterUser()
    {
        // Ki?m tra ?? dài m?t kh?u
        if (PassworRegisterInput.text.Length < 6)
        {
            MessageText.text = "Mật khẩu quá ngắn";
            return;
        }

        var request = new RegisterPlayFabUserRequest
        {
            DisplayName = UserNameRegisterInput.text,
            Email = EmailRegisterInput.text,
            Password = PassworRegisterInput.text,

            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    public void Login()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = EmailLoginInput.text,
            Password = PasswordLoginInput.text,


            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSucces, OnError);
    }

    private void OnLoginSucces(LoginResult result)
    {
        string name = result.InfoResultPayload?.PlayerProfile?.DisplayName ?? "Player";
        WelcomeObject.SetActive(true);
        WelcomeText.text = "Chào mừng " + name;

        if (gameManager != null)
        {
            gameManager.playerName = name;
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.LoadCoinsFromPlayFab();
        }
        else
        {
            Debug.LogWarning("CoinManager chưa được tạo! Không thể tải coin.");
        }

        StartCoroutine(LoadNextScene());
    }

    public void RecoveryUser()
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = EmailLoginInput.text,
            TitleId = "14A2DF"
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnRecoverySucces, OnErrorRecovery);
    }

    private void OnErrorRecovery(PlayFabError result)
    {
        MessageText.text = "Không tìm thấy email";
    }

    private void OnRecoverySucces(SendAccountRecoveryEmailResult obj)
    {
        OpenLoginPage();
        MessageText.text = "Đã gửi thư khôi phục";
    }


    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        MessageText.text = "Tài khoản mới đã được tạo";
        OpenLoginPage();
    }

    private void OnError(PlayFabError error)
    {
        MessageText.text = "Lỗi: hãy nhập lại";
    }


    public void OpenLoginPage()
    {
        LoginPage.SetActive(true);
        ResgisterPage.SetActive(false);
        RecoveryPage.SetActive(false);
        TopText.text = "Đăng nhập";
    }
    public void OpenRegiserPage()
    {
        LoginPage.SetActive(false);
        ResgisterPage.SetActive(true);
        RecoveryPage.SetActive(false);
        TopText.text = "Đăng ký";
    }
    public void OpenRecoveryPage()
    {
        LoginPage.SetActive(false);
        ResgisterPage.SetActive(false);
        RecoveryPage.SetActive(true);
        TopText.text = "Khôi phục mật khẩu";
    }
    #endregion

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(2);
        MessageText.text = "\r\nĐăng nhập thành công";
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        PlayerPrefs.SetString("SceneToLoad", "Menu");
        SceneManager.LoadScene("Loading");
    }

}
