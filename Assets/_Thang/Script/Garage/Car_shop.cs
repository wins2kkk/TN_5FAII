using UnityEngine;
using TMPro;
using System.Collections;

public class Car_shop : MonoBehaviour
{
    public TextMeshProUGUI coinText; // Hiển thị số xu
    public GameObject buyButton; // Nút mua
    public GameObject selectButton; // Nút chọn
    public GameObject successPanel; // Panel mua thành công
    public GameObject failPanel; // Panel không mua thành công

    // Cập nhật: Thêm giá tiền cho 7 xe
    private int[] carPrices = { 0, 2, 5, 10, 25, 40, 80 };
    private bool[] carOwned;

    private const string CAR_OWNED_KEY_PREFIX = "CarOwned_";

    void Awake()
    {
        carOwned = new bool[carPrices.Length];
        LoadCarOwnership();
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
        if (CoinManager.Instance.SpendCoins(price))
        {
            carOwned[currentIndex] = true;
            SaveCarOwnership();
            StartCoroutine(ShowSuccessPanel());
        }
        else
        {
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
    }

    private void LoadCarOwnership()
    {
        for (int i = 0; i < carOwned.Length; i++)
        {
            carOwned[i] = PlayerPrefs.GetInt(CAR_OWNED_KEY_PREFIX + i, i == 0 ? 1 : 0) == 1;
        }
    }

    private void SaveCarOwnership()
    {
        int currentIndex = FindObjectOfType<CarSelect>().GetCurrentCarIndex();
        PlayerPrefs.SetInt(CAR_OWNED_KEY_PREFIX + currentIndex, carOwned[currentIndex] ? 1 : 0);
        PlayerPrefs.Save();
    }
}
