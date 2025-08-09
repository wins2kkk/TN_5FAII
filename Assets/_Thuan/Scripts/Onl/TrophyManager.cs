using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 TrophyManager: tìm lại UI trong scene {scene.name}");
        StartCoroutine(DelayedFindUI());
    }

    private System.Collections.IEnumerator DelayedFindUI()
    {
        yield return new WaitForEndOfFrame();
        FindUIReferences();
        UpdateCupUI();
    }

    private void Start()
    {
        FindUIReferences();
        LoadCupFromPlayFab();
    }

    private void FindUIReferences()
    {
        if (cupText == null)
        {
            cupText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "CupText");
        }
    }

    public void AddCup(int amount)
    {
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
            Debug.LogWarning("⚠️ cupText UI chưa được tìm thấy!");
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

                if (!foundCupData)
                {
                    currentCupCount = 0;
                    Debug.Log("🆕 Chưa có dữ liệu cúp, khởi tạo với 0 cúp");
                    SendCupToLeaderboard(currentCupCount);
                }

                isDataLoaded = true;
                UpdateCupUI();
                Debug.Log($"📥 Tải dữ liệu cúp thành công. Cúp hiện tại: {currentCupCount}");
            },
            error =>
            {
                Debug.LogError("❌ Lỗi tải dữ liệu cúp từ PlayFab: " + error.GenerateErrorReport());
                currentCupCount = 0;
                isDataLoaded = true;
                UpdateCupUI();
            });
    }

    [System.Obsolete("Chỉ dùng cho testing")]
    public void ResetCups()
    {
        currentCupCount = 0;
        UpdateCupUI();
        SendCupToLeaderboard(currentCupCount);
        Debug.Log("🔄 Đã reset cúp về 0");
    }

    public void SetCups(int amount)
    {
        currentCupCount = amount;
        UpdateCupUI();
        SendCupToLeaderboard(currentCupCount);
        Debug.Log($"⚙️ Đã set cúp thành: {amount}");
    }
}
