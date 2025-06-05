using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("UI References - Sẽ được tự động tìm lại")]
    public TextMeshProUGUI coinText; // Text hiển thị số coin trên UI

    private int currentCoins;
    private const string COIN_KEY = "PlayerCoins";

    private void Awake()
    {
        // Singleton pattern với DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCoins();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đăng ký event khi scene load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        FindCoinText();
        UpdateCoinUI();
    }

    // Được gọi mỗi khi load scene mới
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSetup());
    }

    // Delay để đảm bảo UI đã được tạo
    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        FindCoinText();
        UpdateCoinUI();
    }

    // Tự động tìm lại UI text
    private void FindCoinText()
    {
        if (coinText == null)
        {
            // Tìm theo tên GameObject
            GameObject coinTextObj = GameObject.Find("CoinText");
            if (coinTextObj != null)
            {
                coinText = coinTextObj.GetComponent<TextMeshProUGUI>();
            }

            // Nếu không tìm thấy, thử tìm theo tag
            if (coinText == null)
            {
                GameObject coinObj = GameObject.FindWithTag("CoinUI");
                if (coinObj != null)
                {
                    coinText = coinObj.GetComponent<TextMeshProUGUI>();
                }
            }

            // Nếu vẫn không tìm thấy, tìm bất kỳ TextMeshProUGUI nào có text là số
            if (coinText == null)
            {
                TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
                foreach (var text in allTexts)
                {
                    // Tìm text có tên chứa "coin" (không phân biệt hoa thường)
                    if (text.name.ToLower().Contains("coin"))
                    {
                        coinText = text;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tải số coin từ PlayerPrefs
    /// </summary>
    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt(COIN_KEY, 0);
        Debug.Log("Đã tải: " + currentCoins + " coins");
    }

    /// <summary>
    /// Lưu số coin vào PlayerPrefs
    /// </summary>
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COIN_KEY, currentCoins);
        PlayerPrefs.Save();
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

    /// <summary>
    /// Lấy số coin hiện tại
    /// </summary>
    public int GetCoins()
    {
        return currentCoins;
    }

    /// <summary>
    /// Đặt lại coin về 0 (reset game)
    /// </summary>
    public void ResetCoins()
    {
        currentCoins = 0;
        SaveCoins();
        UpdateCoinUI();
        Debug.Log("Đã reset coins về 0");
    }

    /// <summary>
    /// Cập nhật UI hiển thị coin với null check
    /// </summary>
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

    /// <summary>
    /// Cập nhật UI từ bên ngoài (nếu cần)
    /// </summary>
    public void RefreshUI()
    {
        FindCoinText();
        UpdateCoinUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}