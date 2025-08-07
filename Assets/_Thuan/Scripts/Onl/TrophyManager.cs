using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class TrophyManager : MonoBehaviour
{
    public static TrophyManager Instance;

    private const string CUP_LEADERBOARD = "BXH";
    public int currentCupCount = 0;
    private bool isDataLoaded = false;

    [Header("UI")]
    public TextMeshProUGUI cupText; // UI hiển thị cúp

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadCupFromPlayFab();
    }

    public void AddCup(int amount)
    {
        // Chờ data được load trước khi cộng
        if (!isDataLoaded)
        {
            Debug.LogWarning("⏳ Đang chờ tải dữ liệu từ PlayFab...");
            StartCoroutine(WaitForDataThenAddCup(amount));
            return;
        }

        currentCupCount += amount;
        UpdateCupUI();
        SendCupToLeaderboard(currentCupCount);
        Debug.Log($"🏆 Đã cộng {amount} cúp. Tổng cúp hiện tại: {currentCupCount}");
    }

    private System.Collections.IEnumerator WaitForDataThenAddCup(int amount)
    {
        yield return new WaitUntil(() => isDataLoaded);
        AddCup(amount);
    }

    private void UpdateCupUI()
    {
        if (cupText != null)
        {
            cupText.text = $"Cúp: {currentCupCount}";
        }
        else
        {
            Debug.LogWarning("⚠️ cupText UI chưa được gán trong Inspector!");
        }
    }

    public void SendCupToLeaderboard(int cupCount)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new System.Collections.Generic.List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = CUP_LEADERBOARD,
                    Value = cupCount
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result =>
            {
                Debug.Log($"✅ Cập nhật cúp lên Leaderboard thành công. Cúp hiện tại: {cupCount}");
            },
            error =>
            {
                Debug.LogError("❌ Lỗi cập nhật Leaderboard: " + error.GenerateErrorReport());
                // Thử gửi lại sau 2 giây
                Invoke(nameof(RetrySendCup), 2f);
            });
    }

    private void RetrySendCup()
    {
        Debug.Log("🔄 Thử gửi lại cúp lên Leaderboard...");
        SendCupToLeaderboard(currentCupCount);
    }

    public void LoadCupFromPlayFab()
    {
        Debug.Log("📥 Đang tải dữ liệu cúp từ PlayFab...");

        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
            result =>
            {
                bool foundCupData = false;
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == CUP_LEADERBOARD)
                    {
                        currentCupCount = stat.Value;
                        foundCupData = true;
                        break;
                    }
                }

                // Nếu chưa có dữ liệu cúp, khởi tạo bằng 0
                if (!foundCupData)
                {
                    currentCupCount = 0;
                    Debug.Log("🆕 Chưa có dữ liệu cúp, khởi tạo với 0 cúp");
                    // Gửi dữ liệu khởi tạo lên PlayFab
                    SendCupToLeaderboard(currentCupCount);
                }

                isDataLoaded = true;
                UpdateCupUI();
                Debug.Log($"📥 Tải dữ liệu cúp thành công. Cúp hiện tại: {currentCupCount}");
            },
            error =>
            {
                Debug.LogError("❌ Lỗi tải dữ liệu cúp từ PlayFab: " + error.GenerateErrorReport());
                // Nếu lỗi, vẫn cho phép chơi với 0 cúp
                currentCupCount = 0;
                isDataLoaded = true;
                UpdateCupUI();
            });
    }

    // Method để reset cúp (chỉ dùng cho testing)
    [System.Obsolete("Chỉ dùng cho testing")]
    public void ResetCups()
    {
        currentCupCount = 0;
        UpdateCupUI();
        SendCupToLeaderboard(currentCupCount);
        Debug.Log("🔄 Đã reset cúp về 0");
    }

    // Method để thêm cúp trực tiếp (dùng cho testing hoặc admin)
    public void SetCups(int amount)
    {
        currentCupCount = amount;
        UpdateCupUI();
        SendCupToLeaderboard(currentCupCount);
        Debug.Log($"⚙️ Đã set cúp thành: {amount}");
    }
}