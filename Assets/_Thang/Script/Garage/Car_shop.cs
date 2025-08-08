using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

public class Car_shop : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public GameObject buyButton;
    public GameObject selectButton;
    public GameObject successPanel;
    public GameObject failPanel;

    private int[] carPrices = { 0, 1999, 3999, 5999, 7999, 10000, 15000};
    private bool[] carOwned;
    private const string CAR_OWNED_KEY_PREFIX = "CarOwned_";

    void Awake()
    {
        carOwned = new bool[carPrices.Length];
        StartCoroutine(LoadCarOwnershipFromPlayFab());
    }

    public void UpdateUI()
    {
        int currentIndex = FindObjectOfType<CarSelect>().GetCurrentCarIndex();
        int currentCoins = CoinManager.Instance.GetCoins();

        if (carOwned[currentIndex])
        {
            buyButton.SetActive(false);
            selectButton.SetActive(true);
            coinText.text = currentCoins.ToString();
        }
        else
        {
            buyButton.SetActive(true);
            selectButton.SetActive(false);
            coinText.text = carPrices[currentIndex].ToString();
        }
        Debug.Log("Xe " + currentIndex + " - Owned: " + carOwned[currentIndex] + " - Giá: " + coinText.text);
    }

    public void OnCarChanged()
    {
        UpdateUI();
    }

    public void BuyCar()
    {
        int currentIndex = FindObjectOfType<CarSelect>().GetCurrentCarIndex();
        int price = carPrices[currentIndex];

        // ✅ FIX: Chỉ cho phép mua nếu đủ tiền
        if (CoinManager.Instance.HasEnoughCoins(price))
        {
            CoinManager.Instance.SpendCoins(price);
            carOwned[currentIndex] = true;
            SaveCarOwnershipToPlayFab();
            StartCoroutine(ShowSuccessPanel());
        }
        else
        {
            Debug.Log("Không đủ tiền mua xe!");
            StartCoroutine(ShowFailPanel());
        }
    }

    private IEnumerator ShowSuccessPanel()
    {
        successPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        successPanel.SetActive(false);
        UpdateUI();
    }

    private IEnumerator ShowFailPanel()
    {
        failPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        failPanel.SetActive(false);
        UpdateUI();
    }

    // 🟢 Lưu ownership lên PlayFab
    private void SaveCarOwnershipToPlayFab()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        for (int i = 0; i < carOwned.Length; i++)
        {
            data[CAR_OWNED_KEY_PREFIX + i] = carOwned[i] ? "1" : "0";
        }

        var request = new UpdateUserDataRequest
        {
            Data = data
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Đã lưu dữ liệu xe lên PlayFab."),
            error => Debug.LogError("Lỗi lưu dữ liệu xe lên PlayFab: " + error.GenerateErrorReport()));
    }

    // 🟢 Tải ownership từ PlayFab
    private IEnumerator LoadCarOwnershipFromPlayFab()
    {
        bool done = false;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                for (int i = 0; i < carPrices.Length; i++)
                {
                    string key = CAR_OWNED_KEY_PREFIX + i;
                    if (result.Data != null && result.Data.ContainsKey(key))
                    {
                        carOwned[i] = result.Data[key].Value == "1";
                    }
                    else
                    {
                        carOwned[i] = (i == 0); // Mặc định xe đầu tiên được sở hữu
                    }
                }

                Debug.Log("Đã tải ownership xe từ PlayFab.");
                done = true;
                UpdateUI();
            },
            error =>
            {
                Debug.LogError("Lỗi tải dữ liệu ownership xe: " + error.GenerateErrorReport());
                done = true;
            });

        yield return new WaitUntil(() => done);
    }
}
