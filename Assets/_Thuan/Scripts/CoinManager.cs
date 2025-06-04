using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI coinText; // Text hiển thị số coin trên UI

    private int currentCoins;
    private const string COIN_KEY = "PlayerCoins";

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCoins();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateCoinUI();
    }

    /// <summary>
    /// Tải số coin từ PlayerPrefs
    /// </summary>
    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt(COIN_KEY, 0); // Mặc định 0 coin nếu chưa có
        Debug.Log("Đã tải: " + currentCoins + " coins");
    }

    /// <summary>
    /// Lưu số coin vào PlayerPrefs
    /// </summary>
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COIN_KEY, currentCoins);
        PlayerPrefs.Save(); // Đảm bảo dữ liệu được lưu ngay lập tức
        Debug.Log("Đã lưu: " + currentCoins + " coins");
    }

    /// <summary>
    /// Thêm coin và lưu
    /// </summary>
    /// <param name="amount">Số coin cần thêm</param>
    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            currentCoins += amount;
            SaveCoins();
            UpdateCoinUI();
            Debug.Log("+ " + amount + " coins! Tổng: " + currentCoins);
        }
    }

    /// <summary>
    /// Trừ coin (cho mua đồ, v.v.)
    /// </summary>
    /// <param name="amount">Số coin cần trừ</param>
    /// <returns>True nếu đủ coin để trừ</returns>
    public bool SpendCoins(int amount)
    {
        if (amount > 0 && currentCoins >= amount)
        {
            currentCoins -= amount;
            SaveCoins();
            UpdateCoinUI();
            Debug.Log("- " + amount + " coins! Còn lại: " + currentCoins);
            return true;
        }
        else
        {
            Debug.Log("Không đủ coin! Hiện có: " + currentCoins + ", cần: " + amount);
            return false;
        }
    }

  
    // số coin hiện tại

    public int GetCoins()
    {
        return currentCoins;
    }

    /// Đặt lại coin về 0 (reset game)
    public void ResetCoins()
    {
        currentCoins = 0;
        SaveCoins();
        UpdateCoinUI();
        Debug.Log("Đã reset coins về 0");
    }

    
    //  hiển thị coin
    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = currentCoins.ToString();
        }
    }

    /// <summary>
    /// Kiểm tra có đủ coin không
    /// </summary>
    /// <param name="amount">Số coin cần kiểm tra</param>
    /// <returns>True nếu đủ coin</returns>
    public bool HasEnoughCoins(int amount)
    {
        return currentCoins >= amount;
    }
}