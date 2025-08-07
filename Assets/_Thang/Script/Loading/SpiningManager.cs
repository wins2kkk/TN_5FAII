using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

public class SpiningManager : MonoBehaviour
{
    public GameObject wheelPanel;
    public Button spinButton;
    public Button adButton;
    public Button freeSpinButton;
    public TMP_Text winText;
    public TMP_Text countdownText;

    public TMP_Text adCooldownText; // Text hiển thị cooldown quảng cáo

    [Header("Danh sách phần thưởng (coins)")]
    public int[] PrizeCoins = { 300, 100, 500, 100, 100, 200, 100, 200 };
    [HideInInspector] public string[] PrizeName;

    [Header("Số phần thưởng")]
    public int section = 8;

    [Header("Thời gian cooldown (giây)")]
    public int cooldownSeconds = 10;

    [Header("Cài đặt quảng cáo")]
    public int adCooldownMinutes = 15; // 15 phút cooldown giữa các lần xem quảng cáo


    private int randVal;
    private float timeInterval;
    private bool isSpinning;
    private int finalAngle;
    private float totalAngle;

    private DateTime nextSpinTime;
    private DateTime nextAdTime;
    private int freeSpinCount = 0;

    private const string SPIN_KEY = "NextSpinTime";
    private const string AD_KEY = "NextAdTime";
    private const string FREE_SPIN_KEY = "FreeSpinCount";


    private void Start()
    {
        isSpinning = false;
        totalAngle = 360f / section;

        PrizeName = new string[PrizeCoins.Length];
        for (int i = 0; i < PrizeCoins.Length; i++)
        {
            PrizeName[i] = PrizeCoins[i].ToString();
        }

        LoadCooldownFromPlayFab();
        UpdateUI();
        winText.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateUI();
    }

    public void OpenWheelPanel() => wheelPanel.SetActive(true);
    public void CloseWheelPanel() => wheelPanel.SetActive(false);

    public void OnSpinButtonClicked()
    {
        if (isSpinning) return;
        if (CanSpin()) StartCoroutine(Spin());
        else Debug.Log("Chưa đến thời gian quay lại!");
    }

    public void OnAdButtonClicked()
    {
        if(CanWatchAd()) //(DateTime.Now >= nextAdTime)
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.OnRewardedAdWatched = OnRewardedAdFinished;
                AdsManager.Instance.ShowRewardedAd();
            }
            else
            {
                Debug.LogWarning("AdsManager chưa được khởi tạo.");
            }
        }
        else
        { 

            Debug.Log("Chưa đủ thời gian để xem quảng cáo tiếp.");
        }
    }
    private void OnRewardedAdFinished()
    {
        freeSpinCount++;
        nextAdTime = DateTime.Now.AddMinutes(adCooldownMinutes); //DateTime.Now.AddHours(2);
        SaveCooldownToPlayFab();
        UpdateUI();

        Debug.Log("Người chơi đã nhận lượt quay miễn phí sau khi xem quảng cáo.");
    }

    public void OnFreeSpinButtonClicked()
    {
        freeSpinCount++;
        SaveCooldownToPlayFab();
        Debug.Log("Đã nhận 1 lượt quay miễn phí!");
        UpdateUI();
    }

    private bool CanSpin() => DateTime.Now >= nextSpinTime || freeSpinCount > 0;
    private bool CanWatchAd() => DateTime.Now >= nextAdTime;

    private IEnumerator Spin()
    {
        isSpinning = true;
        winText.gameObject.SetActive(false);
        UpdateUI();

        randVal = UnityEngine.Random.Range(200, 300);
        timeInterval = 0.0001f * Time.deltaTime * 2;

        for (int i = 0; i < randVal; i++)
        {
            transform.Rotate(0, 0, (totalAngle / 2));

            if (i > Mathf.RoundToInt(randVal * 0.2f)) timeInterval = 0.5f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.5f)) timeInterval = 1f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.7f)) timeInterval = 1.5f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.8f)) timeInterval = 2f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.9f)) timeInterval = 2.5f * Time.deltaTime;

            yield return new WaitForSeconds(timeInterval);
        }

        if (Mathf.RoundToInt(transform.eulerAngles.z) % totalAngle != 0)
            transform.Rotate(0, 0, totalAngle / 2);

        finalAngle = Mathf.RoundToInt(transform.eulerAngles.z);
        int prizeIndex = (int)(finalAngle / totalAngle) % section;

        int prizeCoin = PrizeCoins[prizeIndex];
        winText.text = $"Bạn nhận được: {prizeCoin} coins";
        winText.gameObject.SetActive(true);
        CoinManager.Instance.AddCoins(prizeCoin);

        if (freeSpinCount > 0)
        {
            freeSpinCount--;
        }
        else
        {
            nextSpinTime = DateTime.Now.AddSeconds(cooldownSeconds);
        }

        SaveCooldownToPlayFab();

        isSpinning = false;
        yield return new WaitForSeconds(3f);
        winText.gameObject.SetActive(false);
        UpdateUI();
    }

    // ----------------- PlayFab Sync -----------------

    private void LoadCooldownFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            var data = result.Data;

            if (data.ContainsKey(SPIN_KEY))
                nextSpinTime = DateTime.FromBinary(Convert.ToInt64(data[SPIN_KEY].Value));
            else
                nextSpinTime = DateTime.MinValue;

            if (data.ContainsKey(AD_KEY))
                nextAdTime = DateTime.FromBinary(Convert.ToInt64(data[AD_KEY].Value));
            else
                nextAdTime = DateTime.MinValue;

            if (data.ContainsKey(FREE_SPIN_KEY))
                freeSpinCount = int.Parse(data[FREE_SPIN_KEY].Value);
            else
                freeSpinCount = 0;
            Debug.Log("Đã load cooldown từ PlayFab.");
            UpdateUI();
        },
        error =>
        {
            Debug.LogError("Lỗi load cooldown từ PlayFab: " + error.GenerateErrorReport());
        });
    }

    private void SaveCooldownToPlayFab()
    {
        var data = new Dictionary<string, string>
        {
            { SPIN_KEY, nextSpinTime.ToBinary().ToString() },
            { AD_KEY, nextAdTime.ToBinary().ToString() },
            { FREE_SPIN_KEY, freeSpinCount.ToString() },
        };

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = data
        },
        result =>
        {
            Debug.Log("Đã lưu cooldown lên PlayFab.");
        },
        error =>
        {
            Debug.LogError("Lỗi lưu cooldown: " + error.GenerateErrorReport());
        });
    }

    // ----------------- UI -----------------

    private void UpdateUI()
    {
        // Cập nhật spin button
        spinButton.gameObject.SetActive(true);
        spinButton.interactable = !isSpinning && CanSpin();

        // Cập nhật ad button
        adButton.interactable = CanWatchAd();
        
        // Cập nhật free spin button
        if (freeSpinButton != null) freeSpinButton.interactable = true;
        
        //Cập nhật countdown text cho spin
        if (countdownText != null)
        {
            if (freeSpinCount > 0 || DateTime.Now >= nextSpinTime)
            {
                countdownText.gameObject.SetActive(false);
            }
            else
            {
                countdownText.gameObject.SetActive(true);
                TimeSpan timeLeft = nextSpinTime - DateTime.Now;
                string minutes = Mathf.FloorToInt((float)timeLeft.TotalSeconds / 60).ToString("00");
                string seconds = Mathf.FloorToInt((float)timeLeft.TotalSeconds % 60).ToString("00");
                countdownText.text = $"Còn lại: {minutes}:{seconds}";
            }
        }


        // Cập nhật ad cooldown text
        if (adCooldownText != null)
        {
            if (DateTime.Now >= nextAdTime)
            {
                adCooldownText.gameObject.SetActive(false);
            }
            else
            {
                adCooldownText.gameObject.SetActive(true);
                TimeSpan timeLeft = nextAdTime - DateTime.Now;
                string minutes = Mathf.FloorToInt((float)timeLeft.TotalMinutes).ToString();
                string seconds = Mathf.FloorToInt((float)timeLeft.TotalSeconds % 60).ToString("00");
                adCooldownText.text = $"{minutes}:{seconds}";
            }
        }

        // Cập nhật màu spin button
        spinButton.GetComponent<Image>().color = spinButton.interactable ? Color.white : Color.gray;

        // Cập nhật màu ad button
        adButton.GetComponent<Image>().color = adButton.interactable ? Color.white : Color.gray;
        // spinButton.GetComponent<Image>().color = spinButton.interactable ? Color.white : Color.gray;
    }
}
